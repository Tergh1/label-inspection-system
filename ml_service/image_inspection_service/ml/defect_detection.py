import numpy as np


def detect_defects(template_img, test_img):

    template = np.array(template_img)
    test = np.array(test_img)

    diff = np.abs(template.astype(int) - test.astype(int))

    mask = diff.mean(axis=2) > 30

    ys, xs = np.where(mask)

    if len(xs) == 0:
        return []

    x1, x2 = xs.min(), xs.max()
    y1, y2 = ys.min(), ys.max()

    return [{
        "x": int(x1),
        "y": int(y1),
        "width": int(x2 - x1),
        "height": int(y2 - y1)
    }]