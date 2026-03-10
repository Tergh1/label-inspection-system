# Web-Based Image Recognition Portal for Automated Quality Control

## Project Overview

This project is a **web-based image inspection system** for manufacturing labels. Users provide the **URL of a product label image** via Blazor UI. The system automatically verifies it against a **template image** using a **pretrained ResNet50 model**.

**Key Features:**

- Blazor sends image URL to **Python ML service**  
- Python ML service downloads the image, performs **feature extraction**, calculates similarity %, detects defects  
- ML service **calls Blazor webhook** when processing finishes  
- Blazor applies **tolerance logic** and updates the UI  
- PostgreSQL database is only accessed by Blazor for storing images and results  
- **Authentication** secures ML service access  

---

## Architecture Diagram

            ┌─────────────────────────────────┐
            │      Blazor Web UI              │
            │  - Input image URL              │
            │  - Send to ML service           │
            │  - Receive webhook              │
            │  - Apply tolerance              │
            │  - Display similarity & defects │
            │  - Access PostgreSQL DB         │
            └─────────────┬───────────────────┘
                          │ HTTP POST (async)
                          ▼
            ┌─────────────────────────────┐
            │    Python ML Service        │
            │       (FastAPI)             │
            │  - Authenticates request    │
            │  - Downloads image from URL │
            │  - Pretrained ResNet50      │
            │  - Computes similarity %    │
            │  - Detects defects          │
            │  - Calls Blazor webhook     │
            └─────────────┬───────────────┘
                          │ Webhook POST
                          ▼
            ┌─────────────────────────────┐
            │      Blazor Web UI          │
            │   - Receives ML results     │
            │   - Applies tolerance       │
            │   - Updates UI with defects │
            │   - Stores results in DB    │
            └─────────────────────────────┘


---

## Technology Stack

| Layer             | Technology                       |
|------------------ |----------------------------------|
| Frontend / API    | Blazor (C#)                      |
| ML Service        | Python, FastAPI, PyTorch         |
| ML Model          | ResNet50 (pretrained, ImageNet)  |
| Database          | PostgreSQL                       |
| Authentication    | API key / token                  |

---

## Workflow

1. User inputs **image URL** in Blazor UI.  
2. Blazor sends POST to `/inspect-async` of Python ML service:

```csharp
var request = new MultipartFormDataContent();
request.Add(new StringContent(imageUrl), "image_url");
request.Add(new StringContent(WebhookUrl), "callback_url");
request.Add(new StringContent(ImageId), "image_id");
request.Headers.Add("X-API-KEY", ApiKey);

await Http.PostAsync("http://ml-service:8000/inspect-async", request);
```
3. ML service authenticates the request using API key.

4. ML service downloads the image and performs ML evaluation asynchronously:

  - ResNet50 feature extraction

  - Similarity % calculation

  - Defect detection (bounding boxes)

5. ML service calls Blazor webhook with results:

```JSON
{
  "image_id": "12345",
  "similarity_percent": 92.4,
  "defects": [{
    "x":95,
    "y":70,
    "width":80,
    "height":50
    }]
}
```
6. Blazor applies tolerance and updates UI with:

 - Similarity %

 - Defect highlights

 - Acceptance / rejection

7. Results are stored in PostgreSQL.

---

## Python ML Service API Contract
/inspect-async (POST)

**Request** (multipart/form-data):

|Field	|Type	|Description|
|----|---| ---|
|image_url|	string|	URL of image to evaluate|
|callback_url|	string	|Blazor webhook URL for results|
|image_id|	string|	Unique image identifier|

**Headers:**

|Header	|Value|	Description|
|----|---| ---|
|X-API-KEY|	<API_KEY>|	API key for authentication|

**Response (immediate):**
```JSON
{
  "status": "processing_started",
  "image_id": "12345"
}
```

**Webhook JSON (sent to Blazor when done):**
```JSON
{
  "image_id": "12345",
  "similarity_percent": 92.4,
  "defects": [
    {"x":95,"y":70,"width":80,"height":50}
  ]
}
```

---
## ML Component Details

 - **Model:** ResNet50 pretrained on ImageNet

 - **Feature extraction only** — no training required

 - **Similarity calculation:** Cosine similarity between template and uploaded image

 - **Defect detection:** Returns coordinates of differing regions

 - **Normalization:** RGB, resize/center crop 224×224, normalize with ImageNet mean/std

 - **Template caching:** Template features stored in memory for speed

---
 ## Blazor Implementation

1. Send image URL to ML service asynchronously (with API key).

2. Implement webhook endpoint to receive results.

3. Display defects and similarity %.

4. Use PostgreSQL for storing images, results, and metadata.
