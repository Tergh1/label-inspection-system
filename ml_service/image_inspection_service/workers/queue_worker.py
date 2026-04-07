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


logger = logging.getLogger(__name__)

shutdown_requested = False


def handle_shutdown(signum, frame):
    global shutdown_requested
    logger.info(f"Shutdown signal received: {signum}")
    shutdown_requested = True


signal.signal(signal.SIGTERM, handle_shutdown)
signal.signal(signal.SIGINT, handle_shutdown)


def run_worker():

    logger.info("Worker started")
    print("Worker started")

    while not shutdown_requested:

        db = SessionLocal()
        job = None

        try:
            job = get_next_job(db)

            if not job: 
                logger.info("Job not found.")
                print("Job not found.")     
                time.sleep(1)
                continue

            logger.info(f"Processing job {job.id}")
            print(f"Processing job {job.id}")

            # -----------------------------
            # PROCESS JOB
            # -----------------------------
            result = process_request(job)

            # -----------------------------
            # SEND WEBHOOK
            # -----------------------------
            try:
                send_webhook(job.callback_url, result)

            except Exception:
                logger.exception("Failed to send inspection webhook", extra={"image_id": job.image_id})
                print("Failed to send inspection webhook", extra={"image_id": job.image_id})

            # -----------------------------
            # MARK COMPLETE
            # -----------------------------
            complete_job(db, job)

            logger.info(f"Job {job.id} completed")
            print(f"Job {job.id} completed")

        except Exception as e:

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

    # -----------------------------
    # EXIT
    # -----------------------------
    logger.info("Worker shutting down...")


def run_worker_forever(poll_interval_seconds: int = 5, error_backoff_seconds: int = 5):

    logger.info("Worker started")
    print("Worker started")

    while not shutdown_requested:

        db = SessionLocal()
        job = None

        try:
            job = get_next_job(db)

            if not job:
                logger.info("Job not found.")
                print("Job not found.")
                time.sleep(poll_interval_seconds)
                continue

            logger.info(f"Processing job {job.id}")
            print(f"Processing job {job.id}")

            result = process_request(job)

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
            time.sleep(error_backoff_seconds)

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
