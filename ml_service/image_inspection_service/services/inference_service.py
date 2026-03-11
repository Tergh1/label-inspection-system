from PIL import Image

from image_inspection_service.services.image_service import download_image
from image_inspection_service.services.webhook_service import send_webhook

from image_inspection_service.ml.feature_extractor import extract_features
from image_inspection_service.ml.similarity import cosine_similarity
from image_inspection_service.ml.defect_detection import detect_defects

from image_inspection_service.core.startup import model, template_features
from image_inspection_service.config import TEMPLATE_IMAGE_PATH


template_image = Image.open(TEMPLATE_IMAGE_PATH).convert("RGB")


def process_request(request):

    image = download_image(request.image_url)

    features = extract_features(model, image)

    similarity = cosine_similarity(template_features, features)

    defects = detect_defects(template_image, image)

    result = {
        "image_id": request.image_id,
        "similarity_percent": similarity,
        "defects": defects
    }

    send_webhook(request.callback_url, result)