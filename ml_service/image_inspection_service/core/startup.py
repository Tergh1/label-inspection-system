from PIL import Image

from image_inspection_service.ml.model_loader import load_model
from image_inspection_service.ml.feature_extractor import extract_features
from image_inspection_service.config import TEMPLATE_IMAGE_PATH


model = None
template_features = None


def load_resources():
    global model
    global template_features

    model = load_model()

    template = Image.open(TEMPLATE_IMAGE_PATH).convert("RGB")

    template_features = extract_features(model, template)