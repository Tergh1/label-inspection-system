import logging


from image_inspection_service.services.image_service import download_image
from image_inspection_service.services.webhook_service import send_webhook

from image_inspection_service.ml.feature_extractor import extract_features
from image_inspection_service.ml.similarity import cosine_similarity
from image_inspection_service.ml.defect_detection import detect_defects

from image_inspection_service.core import startup


logger = logging.getLogger(__name__)
template_cache = {}


def process_request(request):
    try:
        print(f"Processing inspection request for image_id: {request.image_id}")

        # -----------------------------
        # TEMPLATE HANDLING
        # -----------------------------

        print(f"Downloading template and extracting features if not in cache from URL: {request.template_url}")
        if request.template_url not in template_cache:

            logger.info("Downloading template image")

            template_img = download_image(request.template_url)

            logger.info("Extracting template features")
            print("Extracting template features")
            if not startup.model:
                print("Model is none.")
            if not template_img:
                print("Template img is none")
                
            template_features = extract_features(startup.model, template_img)

            template_cache[request.template_url] = {
                "image": template_img,
                "features": template_features
            }

        template_data = template_cache[request.template_url]

        template_img = template_data["image"]
        template_features = template_data["features"]
        print(f"Template and features extracted from cache successfully.")

        # -----------------------------
        # DOWNLOAD INSPECTED IMAGE
        # -----------------------------

        print(f"Downloading image from URL: {request.image_url}")
        image = download_image(request.image_url)
        print(f"Image downloaded successfully for image_id: {request.image_id}")
        print(f"Resizing image based on the template size")
        image = image.resize(template_img.size)
        print(f"Resized image based on the template size")

        # -----------------------------
        # FEATURE EXTRACTION
        # -----------------------------

        inspected_features = extract_features(startup.model, image)
        print(f"Features extracted successfully for image_id: {request.image_id} and template")

        # -----------------------------
        # SIMILARITY CALCULATION
        # -----------------------------

        similarity = cosine_similarity(template_features, inspected_features)
        print(f"Similarity calculated successfully for image_id: {request.image_id}, similarity: {similarity:.4f}")

        # -----------------------------
        # DEFECT DETECTION
        # -----------------------------

        defects = detect_defects(template_img, image)
        print(f"Defects detected successfully for image_id: {request.image_id}, defects found: {len(defects)}")

        # -----------------------------
        # RESULT
        # -----------------------------

        result = {
            "image_id": request.image_id,
            "similarity_percent": similarity,
            "defects": defects
        }
    except Exception as exc:
        logger.exception("Failed to process inspection request", extra={"image_id": request.image_id})
        result = {
            "image_id": request.image_id,
            "failure_reason": str(exc)
        }

        # -----------------------------
        # SEND WEBHOOK
        # -----------------------------

    try:
        send_webhook(request.callback_url, result)
    except Exception:
        logger.exception("Failed to send inspection webhook", extra={"image_id": request.image_id})
