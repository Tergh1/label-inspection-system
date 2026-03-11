import os
from dotenv import load_dotenv

load_dotenv()

API_KEY = os.getenv("ML_SERVICE_API_KEY")

TEMPLATE_IMAGE_PATH = os.getenv(
    "TEMPLATE_IMAGE_PATH",
    "models/template.png"
)

LOG_LEVEL = os.getenv("LOG_LEVEL", "INFO")