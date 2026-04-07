import time
import signal
import logging

from sqlalchemy.exc import SQLAlchemyError

from image_inspection_service.core.database import SessionLocal
from image_inspection_service.services.queue_service import (
    get_next_job,
    complete_job,
    fail_job
)
from image_inspection_service.services.inference_service import process_request
from image_inspection_service.services.webhook_service import send_webhook
from image_inspection_service.utils.string_utils import extract_result_string
import threading


shutdown_event = threading.Event()

logger = logging.getLogger(__name__)

def run_worker(poll_interval_seconds: int = 5, error_backoff_seconds: int = 5):

    logger.info("Worker started")
    print("Worker started")

    while not shutdown_event.is_set():

        db = SessionLocal()
        job = None

        try:
            job = get_next_job(db)

            if not job:
                logger.info("Job not found.")
                print("Job not found.")
                shutdown_event.wait(timeout=poll_interval_seconds)
                continue

            logger.info(f"Processing job {job.id}")
            print(f"Processing job {job.id}")

            result = process_request(job)
            

            logger.info(f"Result established: {extract_result_string(result)}")
            print(f"Result established: {extract_result_string(result)}")

            try:
                send_webhook(job.callback_url, result)

            except Exception:
                logger.exception("Failed to send inspection webhook", extra={"image_id": job.image_id})
                print("Failed to send inspection webhook", extra={"image_id": job.image_id})

            complete_job(db, job)

            logger.info(f"Job {job.id} completed")
            print(f"Job {job.id} completed")

        except SQLAlchemyError:
            db.rollback()
            logger.exception("Worker database error. Backing off before retry.")
            print("Worker database error. Backing off before retry.")
            shutdown_event.wait(timeout=error_backoff_seconds)

        except Exception:

            logger.exception(f"Job failed: {job.id if job else 'unknown'}")
            print(f"Job failed: {job.id if job else 'unknown'}")

            if job:
                fail_job(db, job)

        finally:
            logger.info("DB connection is about to close.")
            print("DB connection is about to close.")
            db.close()
            logger.info("DB connection is closed.")
            print("DB connection is closed.")

    logger.info("Worker shutting down...")
