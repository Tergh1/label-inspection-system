import numpy as np


def cosine_similarity(a, b):

    dot = np.dot(a, b)

    norm_a = np.linalg.norm(a)
    norm_b = np.linalg.norm(b)

    similarity = dot / (norm_a * norm_b)

    return float(similarity * 100)