import torch.nn.functional as F


def compute_similarity(vec1, vec2):
    vec1 = F.normalize(vec1, dim=0)
    vec2 = F.normalize(vec2, dim=0)
    return F.cosine_similarity(vec1, vec2, dim=0).item()