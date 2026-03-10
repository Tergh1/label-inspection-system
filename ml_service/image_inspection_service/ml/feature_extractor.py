from torchvision import transforms
import torch


transform = transforms.Compose([
    transforms.Resize(256),
    transforms.CenterCrop(224),
    transforms.ToTensor(),
])


def extract_features(model, image):

    tensor = transform(image).unsqueeze(0)

    with torch.no_grad():
        features = model(tensor)

    return features.flatten().numpy()