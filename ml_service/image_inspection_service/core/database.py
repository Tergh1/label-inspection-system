import os

from dotenv import load_dotenv
from sqlalchemy import create_engine, text
from sqlalchemy.orm import declarative_base, sessionmaker


load_dotenv()
DATABASE_URL = os.getenv("DATABASE_URL")

engine = create_engine(DATABASE_URL)
Base = declarative_base()

SessionLocal = sessionmaker(bind=engine)


def init_database():
    Base.metadata.create_all(bind=engine)


def verify_database_connection():
    with engine.connect() as connection:
        connection.execute(text("SELECT 1"))
