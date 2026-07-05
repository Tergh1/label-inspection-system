#!/bin/sh
# Runs once, on first initialization of an empty Postgres data directory.
# Creates the two application databases and the ml_service role/queue table.
# Passwords come from the container environment (see docker-compose.prod.yml).
set -eu

# 1. Blazor identity/app database (EF Core migrations create the tables at client startup)
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-SQL
	SELECT 'CREATE DATABASE label_inspection_identity'
	WHERE NOT EXISTS (
	    SELECT FROM pg_database WHERE datname = 'label_inspection_identity'
	)\gexec
SQL

# 2. ML service role + database
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-SQL
	DO \$\$ BEGIN
	    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'ml_service_user') THEN
	        CREATE USER ml_service_user WITH PASSWORD '${ML_DB_PASSWORD}';
	    END IF;
	END \$\$;

	SELECT 'CREATE DATABASE ml_inspection OWNER ml_service_user'
	WHERE NOT EXISTS (
	    SELECT FROM pg_database WHERE datname = 'ml_inspection'
	)\gexec
SQL

# 3. ML service queue table + indexes
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname ml_inspection <<-SQL
	CREATE TABLE IF NOT EXISTS inspection_queue (
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

	CREATE INDEX IF NOT EXISTS idx_queue_status_created
	    ON inspection_queue(status, created_at);

	CREATE INDEX IF NOT EXISTS idx_queue_pending
	    ON inspection_queue(created_at) WHERE status = 'pending';

	ALTER TABLE inspection_queue OWNER TO ml_service_user;
SQL
