from fastapi import FastAPI
from contextlib import asynccontextmanager
from threading import Thread

from image_inspection_service.api.routes import router
from image_inspection_service.core.startup import load_resources
from image_inspection_service.core.database import (
    init_database,
    verify_database_connection,
)
from image_inspection_service.workers.queue_worker import run_worker_forever


@asynccontextmanager
async def lifespan(app: FastAPI):
    verify_database_connection()
    init_database()
    load_resources()
    worker_thread = Thread(target=run_worker_forever, name="inspection-queue-worker", daemon=True)
    worker_thread.start()
    yield


app = FastAPI(lifespan=lifespan)
app.include_router(router)
