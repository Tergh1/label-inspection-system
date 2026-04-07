import torch
import torch.nn.functional as F


def compute_defect_map(vec1, vec2):

    # ensure same shape
    vec1 = F.normalize(vec1, dim=0)
    vec2 = F.normalize(vec2, dim=0)

    diff = torch.abs(vec1 - vec2)

    # reshape to pseudo spatial map (best effort)
    size = int(diff.shape[0] ** 0.5)

    if size * size != diff.shape[0]:
        return []  # cannot map spatially

    diff_map = diff.view(size, size)

    return diff_map


def extract_bounding_boxes(diff_map, threshold=0.3):

    if diff_map is None or len(diff_map) == 0:
        return []

    mask = diff_map > threshold

    coords = torch.nonzero(mask)

    boxes = []

    for y, x in coords:
        boxes.append({
            "x": int(x.item()),
            "y": int(y.item()),
            "width": 1,
            "height": 1
        })

    return boxes