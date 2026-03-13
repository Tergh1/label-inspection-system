CREATE DATABASE label_inspection_identity_dev OWNER postgres;

CREATE USER ml_service_user WITH SUPERUSER PASSWORD 'ml_service_pass';
CREATE DATABASE ml_inspection OWNER ml_service_user;

\c ml_inspection

CREATE TABLE inspection_queue (
    id UUID PRIMARY KEY,
    image_id TEXT NOT NULL,
    template_url TEXT NOT NULL,
    image_url TEXT NOT NULL,
    callback_url TEXT NOT NULL,
    status TEXT DEFAULT 'pending',
    retries INT DEFAULT 0,
    max_retries INT DEFAULT 3,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    started_at TIMESTAMP
);

CREATE INDEX idx_queue_status_created
ON inspection_queue(status, created_at);

CREATE INDEX idx_queue_pending
ON inspection_queue(created_at)
WHERE status = 'pending';