from PIL import Image

from ml_service.ml.model_loader import load_model
from ml_service.ml.feature_extractor import extract_features
from ml_service.config import TEMPLATE_IMAGE_PATH


model = None
template_features = None


def load_resources():
    global model
    global template_features

    model = load_model()

    template = Image.open(TEMPLATE_IMAGE_PATH).convert("RGB")

    template_features = extract_features(model, template)