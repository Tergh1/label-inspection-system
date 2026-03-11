import logging

from PIL import Image

from image_inspection_service.services.image_service import download_image
from image_inspection_service.services.webhook_service import send_webhook

from image_inspection_service.ml.feature_extractor import extract_features
from image_inspection_service.ml.similarity import cosine_similarity
from image_inspection_service.ml.defect_detection import detect_defects

from image_inspection_service.core import startup
from image_inspection_service.config import TEMPLATE_IMAGE_PATH


template_image = Image.open(TEMPLATE_IMAGE_PATH).convert("RGB")
logger = logging.getLogger(__name__)


def process_request(request):
    try:
        print(f"Processing inspection request for image_id: {request.image_id}")
        print(f"Downloading image from URL: {request.image_url}")
        image = download_image(request.image_url)
        print(f"Image downloaded successfully for image_id: {request.image_id}")
        features = extract_features(startup.model, image)
        print(f"Features extracted successfully for image_id: {request.image_id}")
        similarity = cosine_similarity(startup.template_features, features)
        print(f"Similarity calculated successfully for image_id: {request.image_id}, similarity: {similarity:.4f}")
        defects = detect_defects(template_image, image)
        print(f"Defects detected successfully for image_id: {request.image_id}, defects found: {len(defects)}")
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

    try:
        send_webhook(request.callback_url, result)
    except Exception:
        logger.exception("Failed to send inspection webhook", extra={"image_id": request.image_id})
