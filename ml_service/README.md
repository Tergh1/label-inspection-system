# Image Inspection Service

## Overview

`image_inspection_service` is a Python-based Machine Learning microservice designed to perform automated visual inspection of product labels in a manufacturing environment.

The service receives a URL to an image, downloads it, compares it against a predefined **template image**, calculates a **similarity percentage**, detects **visual defects**, and returns the results asynchronously through a **webhook callback**.

This service is designed to integrate with a **Blazor Web Application**, which provides the user interface and manages system data storage.

The system uses a **pretrained deep learning model (ResNet50)** to extract image features and calculate similarity.

---

# Technology Stack

| Component | Technology |
|--------|--------|
| Programming Language | Python 3.11 |
| Dependency Management | Poetry |
| Web Framework | FastAPI |
| ML Framework | PyTorch |
| Pretrained Model | ResNet50 |
| Image Processing | Pillow |
| HTTP Requests | Requests |
| Data Validation | Pydantic |

---


Key design principles:

- **Asynchronous processing**
- **Pretrained neural network**
- **Webhook-based communication**
- **No direct database access**
- **Stateless service**

---

# Project Structure
```
image_inspection_service
│
├── image_inspection_service
│
│ ├── main.py
│ ├── config.py
│ ├── security.py
│
│ ├── api
│ │ └── routes.py
│
│ ├── core
│ │ └── startup.py
│
│ ├── services
│ │ ├── inference_service.py
│ │ ├── image_service.py
│ │ └── webhook_service.py
│
│ ├── ml
│ │ ├── model_loader.py
│ │ ├── feature_extractor.py
│ │ ├── similarity.py
│ │ └── defect_detection.py
│
│ └── schemas
│ └── dto.py
│
├── models
│ └── template.jpg
│
├── pyproject.toml
└── README.md
```


---

# Installation

## Pre-requisites:

1. Windows setup
    - Python v3.11.3
        - Installers (64-bit) - [Download Link](https://www.python.org/downloads/release/python-3113/)
    - Poetry
        - Install Poetry using pip: `python -m pip install poetry`

2. MAC/LINUX setup
    - Install Brew - [Link](https://brew.sh/)
    - Install Pyenv via brew - `brew install pyenv` (will enable management of multiple python versions)
    - Restart terminal
    - Setup default python version to v3.11.3 - `pyenv global 3.11.3`

---

## Install Dependencies and activate virtual environment

Open terminal inside the project folder:
1. Set Poetry config setting which force to create .venv in the project:
    - `poetry config virtualenvs.in-project true`
2. Install all dependencies from `./pyproject.toml` file via:
    - `poetry install`

3. Activate Virtual Environment
- `poetry env activate`


---

# Running the Service

Start the FastAPI server:
 - `poetry run start`

Service will run at:
 - http://localhost:8000


Swagger documentation:
 - http://localhost:8000/docs


---

# API Endpoint

## Start Image Inspection
POST /inspect-async


### Request Body
```JSON
{
  "image_id": "12345",
  "image_url": "https://example.com/image.jpg",
  "callback_url": "https://blazor-app/api/ml-result"
}
```

### Response
```JSON
{
  "status": "processing_started",
  "image_id": "12345"
}
```

### Webhook Callback Response

After processing the image, the service calls the provided webhook URL.

Example Callback Payload:
```JSON
{
  "image_id": "12345",
  "similarity_percent": 96.3,
  "defects": [
    {
      "x": 210,
      "y": 120,
      "width": 45,
      "height": 30
    }
  ]
}
```