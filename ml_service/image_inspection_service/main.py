from fastapi import FastAPI
from threading import Thread

from image_inspection_service.api.routes import router
from image_inspection_service.workers.queue_worker import worker_loop
from image_inspection_service.core.startup import load_resources

app = FastAPI()

app.include_router(router)


@app.on_event("startup")
def startup():

    load_resources()

    Thread(target=worker_loop, daemon=True).start()