from fastapi import FastAPI
from contextlib import asynccontextmanager
from image_inspection_service.api.routes import router
from image_inspection_service.core.startup import load_resources


@asynccontextmanager
async def lifespan(app: FastAPI):
    # -----------------------------
    # STARTUP
    # -----------------------------
    load_resources()
    yield
    # -----------------------------
    # SHUTDOWN (optional)
    # -----------------------------
    # cleanup logic here if needed


app = FastAPI(lifespan=lifespan)
app.include_router(router)