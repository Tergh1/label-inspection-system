import torch
import torchvision.models as models


def load_model():

    model = models.resnet18(weights=models.ResNet18_Weights.DEFAULT)

    model.fc = torch.nn.Identity()

    model.eval()

    return model