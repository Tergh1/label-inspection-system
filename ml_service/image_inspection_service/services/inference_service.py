import logging
import time
from image_inspection_service.services.image_service import download_image

from image_inspection_service.ml.similarity import  cosine_similarity
from image_inspection_service.ml.defect_detection import detect_defects

from image_inspection_service.core import startup

from time import perf_counter

from image_inspection_service.schemas.performance import (
    RequestMetrics,
    ModelMetrics
)


logger = logging.getLogger(__name__)

template_cache = {}
CACHE_TTL_SECONDS = 600   # 10 minutes
CACHE_MAX_SIZE = 25


def process_request(request):
    metrics = RequestMetrics(image_id=request.image_id)

    try:
        logger.info(f"Processing inspection request for image_id: {request.image_id}")

        # -----------------------------
        # TEMPLATE HANDLING (WITH CACHE)
        # -----------------------------

        logger.info(f"Downloading template and extracting features if not in cache from URL: {request.template_url}")

        if request.template_url not in template_cache:

            logger.info(f"Downloading template image from URL: {request.template_url}")
            start = perf_counter()
            template_img = download_image(request.template_url)

            metrics.template_download_ms = (
                perf_counter() - start
            ) * 1000
            
            if template_img is None:
                raise RuntimeError("Template image is None")

            logger.info("Extracting template features for all models")

            template_features_per_model = {}

            for name, extractor in startup.extractors.items():

                try:
                    logger.info(f"Extracting template features using {name}")
                    features = extractor.extract(template_img)
                    template_features_per_model[name] = features.detach().cpu()

                except Exception as e:
                    logger.exception(f"Failed extracting template features for model {name}")
                    template_features_per_model[name] = None

            _cleanup_expired_cache()
            _ensure_cache_limit()
            template_cache[request.template_url] = {
                "image": template_img,
                "features_by_model": template_features_per_model,
                "timestamp": time.time()
                }
            logger.info(f"Cache size: {len(template_cache)}")

        template_data = template_cache[request.template_url]
        template_data["timestamp"] = time.time()

        template_img = template_data["image"]
        template_features_per_model = template_data["features_by_model"]

        logger.info("Template and features loaded from cache successfully.")

        # -----------------------------
        # DOWNLOAD INSPECTED IMAGE
        # -----------------------------

        logger.info(f"Downloading image from URL: {request.image_url}")
        start = perf_counter()
        image = download_image(request.image_url)
        metrics.image_download_ms = (
            perf_counter() - start
        ) * 1000

        if image is None:
            raise RuntimeError("Inspected image is None")

        logger.info(f"Image downloaded successfully for image_id: {request.image_id}")

        # resize to match template
        logger.info("Resizing image based on template size")
        image = image.resize(template_img.size)

        # -----------------------------
        # PROCESS PER MODEL (SEQUENTIAL)
        # -----------------------------

        results = []

        for name, extractor in startup.extractors.items():

            logger.info(f"Processing model: {name}")
            model_metrics = ModelMetrics(
                model=name
            )
            
            try:
                template_features = template_features_per_model.get(name)

                if template_features is None:
                    raise RuntimeError("Template features missing")

                # -----------------------------
                # FEATURE EXTRACTION
                # -----------------------------
                start = perf_counter()
                inspected_features = extractor.extract(image)
                model_metrics.feature_extraction_ms = (
                    perf_counter() - start
                ) * 1000

                # -----------------------------
                # SIMILARITY
                # -----------------------------
                start = perf_counter()
                similarity = cosine_similarity(template_features, inspected_features)
                model_metrics.similarity_ms = (
                    perf_counter() - start
                ) * 1000
                logger.info(f"{name}: similarity={similarity:.4f}")

                # -----------------------------
                # DEFECT DETECTION
                # -----------------------------
                start = perf_counter()
                defects = detect_defects(template_img, image)
                model_metrics.defect_detection_ms = (
                    perf_counter() - start
                ) * 1000
                logger.info(f"{name}: defects found={len(defects)}")

                results.append({
                    "model": name,
                    "similarity": similarity,
                    "similarity_percent": similarity,
                    "defects": defects,
                    "status": "completed"
                })

                metrics.model_metrics.append(model_metrics)

            except Exception as e:

                logger.exception(f"Model {name} failed")

                results.append({
                    "model": name,
                    "similarity": None,
                    "defects": [],
                    "status": "failed",
                    "error": str(e)
                })

        # -----------------------------
        # FINAL RESULT
        # -----------------------------

        result = {
            "image_id": request.image_id,
            "results": results
        }

    except Exception as exc:

        logger.exception("Failed to process inspection request", extra={"image_id": request.image_id})

        result = {
            "image_id": request.image_id,
            "results": [
                {
                    "model": name,
                    "similarity": None,
                    "defects": [],
                    "status": "failed",
                    "error": str(exc)
                }
                for name in startup.extractors.keys()
            ]
        }
    
    return result, metrics;

def _cleanup_expired_cache():
    now = time.time()

    expired_keys = [
        key for key, value in template_cache.items()
        if now - value["timestamp"] > CACHE_TTL_SECONDS
    ]

    for key in expired_keys:
        del template_cache[key]

def _ensure_cache_limit():
    if len(template_cache) <= CACHE_MAX_SIZE:
        return

    oldest_key = min(
        template_cache.keys(),
        key=lambda k: template_cache[k]["timestamp"]
    )

    del template_cache[oldest_key]
