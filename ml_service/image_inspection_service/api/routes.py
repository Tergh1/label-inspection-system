from fastapi import APIRouter, Depends
from sqlalchemy.orm import Session
from image_inspection_service.schemas.dto import InspectRequest
from image_inspection_service.security import verify_api_key
from image_inspection_service.services.queue_service import create_job
from image_inspection_service.core.database import SessionLocal

router = APIRouter()

def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()

@router.post("/inspect-async", operation_id="inspect_async")
async def inspect_async(request: InspectRequest, db: Session = Depends(get_db), auth=Depends(verify_api_key)):

    job = create_job(db, request.model_dump())

    return {
        "status": "queued",
        "image_id": request.image_id
    }