from sqlalchemy.orm import Session
from datetime import datetime, timezone
from image_inspection_service.models.queue import InspectionJob


def create_job(db: Session, data):
    job = InspectionJob(**data)
    db.add(job)
    db.commit()
    db.refresh(job)
    return job


def get_next_job(db: Session):

    job = (
        db.query(InspectionJob)
        .filter(InspectionJob.status == "pending")
        .order_by(InspectionJob.created_at)
        .first()
    )

    if job:
        job.status = "processing"
        job.started_at = datetime.now(timezone.utc)
        db.commit()

    return job


def complete_job(db: Session, job):
    job.status = "completed"
    db.commit()


def fail_job(db: Session, job):
    job.retries += 1

    if job.retries >= job.max_retries:
        job.status = "failed"
    else:
        job.status = "pending"

    db.commit()