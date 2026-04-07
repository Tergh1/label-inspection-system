import numpy as np
import torch


def compute_defect_map(template_image, inspected_image):
    template_array = np.asarray(template_image.convert("RGB"), dtype=np.int16)
    inspected_array = np.asarray(inspected_image.convert("RGB"), dtype=np.int16)

    if template_array.shape != inspected_array.shape:
        raise ValueError("Template and inspected images must have the same size")

    per_pixel_diff = np.abs(template_array - inspected_array).mean(axis=2)

    return torch.from_numpy(per_pixel_diff)


def extract_bounding_boxes(diff_map, threshold=30, min_area=20):

    if diff_map is None or diff_map.numel() == 0:
        return []

    mask = (diff_map > threshold).cpu().numpy().astype(np.uint8)
    height, width = mask.shape

    visited = np.zeros_like(mask, dtype=bool)
    boxes = []

    for start_y in range(height):
        for start_x in range(width):
            if mask[start_y, start_x] == 0 or visited[start_y, start_x]:
                continue

            component = _collect_component(mask, visited, start_x, start_y, width, height)

            if len(component) < min_area:
                continue

            xs = [x for x, _ in component]
            ys = [y for _, y in component]

            boxes.append({
                "x": min(xs),
                "y": min(ys),
                "width": max(xs) - min(xs) + 1,
                "height": max(ys) - min(ys) + 1
            })

    return boxes


def _collect_component(mask, visited, start_x, start_y, width, height):
    stack = [(start_x, start_y)]
    component = []

    while stack:
        x, y = stack.pop()

        if x < 0 or x >= width or y < 0 or y >= height:
            continue

        if visited[y, x] or mask[y, x] == 0:
            continue

        visited[y, x] = True
        component.append((x, y))

        stack.extend([
            (x - 1, y),
            (x + 1, y),
            (x, y - 1),
            (x, y + 1),
        ])

    return component
