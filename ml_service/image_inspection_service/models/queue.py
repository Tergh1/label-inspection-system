from sqlalchemy import Column, String, Integer, DateTime
from sqlalchemy.dialects.postgresql import UUID
from sqlalchemy.sql import func
from sqlalchemy.ext.declarative import declarative_base
import uuid

Base = declarative_base()


class InspectionQueue(Base):

    __tablename__ = "inspection_queue"

    id = Column(UUID(as_uuid=True), primary_key=True, default=uuid.uuid4)

    image_id = Column(String)
    template_url = Column(String)
    image_url = Column(String)
    callback_url = Column(String)

    status = Column(String, default="pending")

    retries = Column(Integer, default=0)
    max_retries = Column(Integer, default=3)

    created_at = Column(DateTime, server_default=func.now())