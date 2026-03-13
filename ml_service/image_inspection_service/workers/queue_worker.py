import time
from sqlalchemy import text
from image_inspection_service.core.database import engine
from image_inspection_service.services.inference_service import process_request


def recover_stuck_jobs():

    with engine.begin() as conn:

        conn.execute(text("""
        UPDATE inspection_queue
        SET status='pending'
        WHERE status='processing'
        AND started_at < NOW() - INTERVAL '5 minutes'
        """))


def worker_loop():

    while True:

        recover_stuck_jobs()
        
        with engine.begin() as conn:

            job = conn.execute(text("""
                SELECT *
                FROM inspection_queue
                WHERE status='pending'
                ORDER BY created_at
                FOR UPDATE SKIP LOCKED
                LIMIT 1
            """)).fetchone()

            if not job:
                time.sleep(3)
                continue

            conn.execute(text("""
                UPDATE inspection_queue
                SET status='processing',
                    started_at = NOW()
                WHERE id=:id
                """), {"id": job.id})

        try:

            process_request(job)

            with engine.begin() as conn:
                conn.execute(text("""
                    UPDATE inspection_queue
                    SET status='completed'
                    WHERE id=:id
                """), {"id": job.id})

        except Exception:

            with engine.begin() as conn:
                conn.execute(text("""
                    UPDATE inspection_queue
                    SET retries = retries + 1,
                        status = CASE
                            WHEN retries + 1 >= max_retries THEN 'failed'
                            ELSE 'pending'
                        END
                    WHERE id=:id
                """), {"id": job.id})