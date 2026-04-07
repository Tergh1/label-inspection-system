from fastapi import FastAPI
from contextlib import asynccontextmanager
from image_inspection_service.api.routes import router
from image_inspection_service.core.startup import load_resources
from image_inspection_service.workers.queue_worker import run_worker


@asynccontextmanager
async def lifespan(app: FastAPI):
    load_resources()
    run_worker()
    yield


app = FastAPI(lifespan=lifespan)
app.include_router(router)