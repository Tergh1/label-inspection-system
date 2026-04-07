import torch
import torch.nn as nn
from torchvision import transforms
from torchvision.models import (
    resnet18, ResNet18_Weights,
    efficientnet_b0, EfficientNet_B0_Weights
)


transform = transforms.Compose([
    transforms.Resize(256),
    transforms.CenterCrop(224),
    transforms.ToTensor(),
    transforms.Normalize(
        mean=[0.485, 0.456, 0.406],
        std=[0.229, 0.224, 0.225]
    )
])

class FeatureExtractor:

    def __init__(self, model):
        self.model = model
        self.model.eval()
        self.transform = transform

    @torch.no_grad()
    def extract(self, image):
        tensor = self.transform(image).unsqueeze(0)
        features = self.model(tensor)
        return features.squeeze()


def get_resnet() -> FeatureExtractor:
    weights = ResNet18_Weights.DEFAULT
    model = resnet18(weights=weights)
    model.fc = nn.Identity()
    return FeatureExtractor(model)


def get_efficientnet() -> FeatureExtractor:
    weights = EfficientNet_B0_Weights.DEFAULT
    model = efficientnet_b0(weights=weights)
    model.classifier = nn.Identity()
    return FeatureExtractor(model)
