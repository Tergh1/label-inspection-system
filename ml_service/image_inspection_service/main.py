from fastapi import FastAPI
from ml_service.api.routes import router
from ml_service.core.startup import load_resources

app = FastAPI(
    title="ML Image Inspection Service",
    version="1.0"
)

@app.on_event("startup")
def startup_event():
    load_resources()

app.include_router(router)