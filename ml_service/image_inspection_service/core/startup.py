from image_inspection_service.ml.feature_extractors import (
    get_resnet,
    get_efficientnet
)

extractors = {}


def load_resources():
    print("loading resnet model")
    extractors["resnet"] = get_resnet()
    print("loaded resnet model")
    print("loading efficientnet model")
    extractors["efficientnet"] = get_efficientnet()
    print("loaded efficientnet model")