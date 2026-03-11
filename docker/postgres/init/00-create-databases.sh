#!/bin/sh
set -eu

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" <<-SQL
  SELECT 'CREATE DATABASE label_inspection_identity'
  WHERE NOT EXISTS (
      SELECT FROM pg_database WHERE datname = 'label_inspection_identity'
  )\gexec
SQL
