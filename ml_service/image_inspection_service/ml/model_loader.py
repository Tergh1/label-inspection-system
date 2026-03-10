import torch
import torchvision.models as models


def load_model():

    model = models.resnet50(weights=models.ResNet50_Weights.DEFAULT)

    model = torch.nn.Sequential(*list(model.children())[:-1])

    model.eval()

    return model