from pathlib import Path
from sqlalchemy import create_engine, Column, Integer, String, Float, DateTime, ForeignKey
from sqlalchemy.orm import DeclarativeBase, Session, relationship
from datetime import datetime

DB_PATH = Path(__file__).parent / "poe.db"
engine = create_engine(f"sqlite:///{DB_PATH}", echo=False)

class Base(DeclarativeBase):
    pass

class Items(Base):
    __tablename__ = "uniq_items"
    id = Column(Integer, primary_key = True)
    name = Column(String(200), unique=True, nullable=False)
    item_type = Column(String(200), nullable=False, index=True)
    item_sub_type = Column(String(200), nullable=False, index=True)
    prices = relationship("Price", back_populates="item")

class Price(Base):
    __tablename__ = "prices"
    id = Column(Integer, primary_key=True)
    item_id = Column(Integer, ForeignKey("uniq_items.id"), nullable=False, index=True)
    price = Column(Float, nullable=False)
    currency = Column(String(50), nullable=False)
    chaos_equal = Column(Float, nullable=True)
    prev_chaos_equal = Column(Float, nullable=True)
    recorded_at = Column(String(50), default=lambda: datetime.utcnow().strftime("%Y-%m-%d %H:%M:%S"))
    item = relationship("Items", back_populates="prices")

def init_db():
    Base.metadata.create_all(engine)

def get_session():
    return Session(engine)

def clear_prices():
    session = get_session()
    session.query(Price).delete()
    session.commit()
    session.close()

#clear_prices()
init_db()