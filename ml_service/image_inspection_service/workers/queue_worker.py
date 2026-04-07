import time
import signal
import logging

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

    while not shutdown_requested:

        db = SessionLocal()
        job = None

        try:
            job = get_next_job(db)

            if not job:
                for _ in range(20):
                    if shutdown_requested:
                        break
                    time.sleep(0.1)
                continue

            logger.info(f"Processing job {job.id}")

            # -----------------------------
            # PROCESS JOB
            # -----------------------------
            result = process_request(job)

            # -----------------------------
            # SEND WEBHOOK
            # -----------------------------
            send_webhook(job.callback_url, result)

            # -----------------------------
            # MARK COMPLETE
            # -----------------------------
            complete_job(db, job)

            logger.info(f"Job {job.id} completed")

        except Exception as e:

            logger.exception(f"Job failed: {job.id if job else 'unknown'}")

            if job:
                fail_job(db, job)

        finally:
            db.close()

    # -----------------------------
    # EXIT
    # -----------------------------
    logger.info("Worker shutting down...")