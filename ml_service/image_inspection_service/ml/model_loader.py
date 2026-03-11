import torch
import torchvision.models as models


def load_model():

    model = models.resnet18(weights=models.ResNet18_Weights.DEFAULT)

    model = torch.nn.Sequential(*list(model.children())[:-1])

    model.eval()

    return model