import uvicorn
from image_inspection_service.core.startup import load_resources
from image_inspection_service.workers.queue_worker import run_worker

def main():
    uvicorn.run(
        "image_inspection_service.main:app",
        host="0.0.0.0",
        port=8000,
        reload=True,
    )


    if __name__ == "__main__":
        load_resources()
        run_worker()