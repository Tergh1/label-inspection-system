from fastapi import FastAPI
from contextlib import asynccontextmanager
from threading import Thread

from image_inspection_service.api.routes import router
from image_inspection_service.core.startup import load_resources
from image_inspection_service.core.database import (
    init_database,
    verify_database_connection,
)
from image_inspection_service.workers.queue_worker import (
    run_worker,
    shutdown_event
)


@asynccontextmanager
async def lifespan(app: FastAPI):

    verify_database_connection()
    init_database()
    load_resources()

    workers = []

    for i in range(3):
        worker = Thread(target=run_worker, daemon=True)
        worker.start()
        workers.append(worker)

    yield

    shutdown_event.set()

    for w in workers:
        w.join(timeout=5)


app = FastAPI(lifespan=lifespan)
app.include_router(router)