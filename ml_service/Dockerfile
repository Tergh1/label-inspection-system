FROM python:3.11-slim AS base

ENV PYTHONUNBUFFERED=1 \
    PYTHONDONTWRITEBYTECODE=1 \
    PIP_NO_CACHE_DIR=1 \
    # Cache torchvision model weights here and pre-download them at build time
    TORCH_HOME=/opt/torch

WORKDIR /app

# Runtime system libs (psycopg2-binary bundles libpq; libpq5 kept as a safety net)
RUN apt-get update && apt-get install -y --no-install-recommends \
        libpq5 \
        curl \
    && rm -rf /var/lib/apt/lists/*

# CPU-only PyTorch — avoids pulling the multi-GB CUDA build from PyPI
RUN pip install --no-cache-dir \
        torch==2.2.2 torchvision==0.17.2 \
        --index-url https://download.pytorch.org/whl/cpu

# Remaining runtime dependencies (mirrors ml_service/pyproject.toml, minus torch)
RUN pip install --no-cache-dir \
        "fastapi>=0.110,<0.111" \
        "uvicorn>=0.29,<0.30" \
        "pillow>=10.2" \
        "numpy>=1.26,<2" \
        "requests>=2.31" \
        "python-multipart>=0.0.9" \
        "python-dotenv>=1.2.2" \
        "sqlalchemy>=2.0.48" \
        "asyncpg>=0.31.0" \
        "psycopg2-binary>=2.9.11"

# Pre-download ResNet18 + EfficientNet-B0 weights so the first request needs no internet
RUN python -c "from torchvision.models import resnet18, ResNet18_Weights, efficientnet_b0, EfficientNet_B0_Weights; resnet18(weights=ResNet18_Weights.DEFAULT); efficientnet_b0(weights=EfficientNet_B0_Weights.DEFAULT)"

# Build context is the repo root (see docker-compose.prod.yml)
COPY ml_service/image_inspection_service ./image_inspection_service
COPY models ./models

EXPOSE 8000

# Run uvicorn directly (no --reload). The app's lifespan starts 3 queue-worker
# threads internally, so keep uvicorn to a single worker process.
CMD ["uvicorn", "image_inspection_service.main:app", "--host", "0.0.0.0", "--port", "8000", "--workers", "1"]
