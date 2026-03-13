from image_inspection_service.ml.model_loader import load_model


model = None


def load_resources():
    global model

    model = load_model()
    print("ResNet model loaded")
