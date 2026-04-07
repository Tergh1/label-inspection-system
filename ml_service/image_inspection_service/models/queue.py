from sqlalchemy import Column, String, Integer, DateTime, Text
from sqlalchemy.dialects.postgresql import UUID
from sqlalchemy.orm import declarative_base
import uuid
from datetime import datetime, timezone

Base = declarative_base()


class InspectionJob(Base):
    __tablename__ = "inspection_queue"

    id = Column(UUID(as_uuid=True), primary_key=True, default=uuid.uuid4)
    image_id = Column(String, nullable=False)
    template_url = Column(Text, nullable=False)
    image_url = Column(Text, nullable=False)
    callback_url = Column(Text, nullable=False)

    status = Column(String, default="pending")
    retries = Column(Integer, default=0)
    max_retries = Column(Integer, default=3)

    created_at = Column(DateTime, default=datetime.now(timezone.utc))
    started_at = Column(DateTime, nullable=True)