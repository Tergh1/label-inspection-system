from fastapi import APIRouter, Depends
from image_inspection_service.schemas.dto import InspectRequest
from image_inspection_service.security import verify_api_key

from image_inspection_service.core.database import SessionLocal
from image_inspection_service.models.queue import InspectionQueue

router = APIRouter()


@router.post("/inspect-async")
async def inspect_async(
    request: InspectRequest,
    auth=Depends(verify_api_key)
):

    db = SessionLocal()

    job = InspectionQueue(
        image_id=request.image_id,
        template_url=request.template_url,
        image_url=request.image_url,
        callback_url=request.callback_url
    )

    db.add(job)
    db.commit()

    return {
        "status": "queued",
        "image_id": request.image_id
    }