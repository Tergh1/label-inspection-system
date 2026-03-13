from pydantic import BaseModel
from typing import List


class InspectRequest(BaseModel):
    image_url: str
    template_url: str
    callback_url: str
    image_id: str


class Defect(BaseModel):
    x: int
    y: int
    width: int
    height: int


class InspectResult(BaseModel):
    image_id: str
    similarity_percent: float
    defects: List[Defect]