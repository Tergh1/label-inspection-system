from fastapi import APIRouter, BackgroundTasks, Depends

from ml_service.schemas.dto import InspectRequest
from ml_service.services.inference_service import process_request
from ml_service.security import verify_api_key

router = APIRouter()


@router.post("/inspect-async")
async def inspect_async(
    request: InspectRequest,
    background_tasks: BackgroundTasks,
    auth=Depends(verify_api_key)
):

    background_tasks.add_task(process_request, request)

    return {
        "status": "processing_started",
        "image_id": request.image_id
    }