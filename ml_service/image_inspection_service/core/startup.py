from image_inspection_service.ml.feature_extractors import (
    get_resnet,
    get_efficientnet
)

extractors = {}


def load_resources():

    extractors["resnet"] = get_resnet()
    extractors["efficientnet"] = get_efficientnet()