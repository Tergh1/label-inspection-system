import uvicorn


def main():
    uvicorn.run(
        "image_inspection_service.main:app",
        host="0.0.0.0",
        port=8000,
        reload=True,
    )