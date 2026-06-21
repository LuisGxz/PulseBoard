"""FastAPI ETL microservice for PulseBoard.

The .NET API orchestrates: it creates the Dataset row (Status=Processing), then calls /ingest with the
uploaded CSV. This service parses + profiles it with pandas and writes columns/rows into the shared
Postgres, flipping the dataset to Ready (or Failed). /preview does the same parsing without persisting.
"""

from __future__ import annotations

import psycopg
from fastapi import Depends, FastAPI, File, Form, Header, HTTPException, UploadFile

from .config import settings
from .db import dataset_exists, mark_failed, write_dataset
from .ingest import IngestError, process_csv
from .schemas import IngestResult, ProfileResult

app = FastAPI(title="PulseBoard ETL", version="1.0.0")


def require_api_key(x_etl_key: str | None = Header(default=None)) -> None:
    if x_etl_key != settings.api_key:
        raise HTTPException(status_code=401, detail="Invalid or missing X-ETL-Key.")


async def _read_csv(file: UploadFile) -> bytes:
    data = await file.read()
    if not data:
        raise HTTPException(status_code=422, detail="Uploaded file is empty.")
    return data


@app.get("/health")
def health() -> dict:
    return {"status": "ok", "service": "pulseboard-etl"}


@app.post("/preview", response_model=ProfileResult, dependencies=[Depends(require_api_key)])
async def preview(file: UploadFile = File(...)) -> ProfileResult:
    """Parse + profile a CSV and return the inferred schema and a row preview. Nothing is persisted."""
    data = await _read_csv(file)
    try:
        columns, rows = process_csv(data, max_rows=settings.max_rows, max_columns=settings.max_columns)
    except IngestError as exc:
        raise HTTPException(status_code=422, detail=str(exc)) from exc
    return ProfileResult(row_count=len(rows), columns=columns, preview=rows[:10])


@app.post("/ingest", response_model=IngestResult, dependencies=[Depends(require_api_key)])
async def ingest(
    dataset_id: str = Form(...),
    file: UploadFile = File(...),
) -> IngestResult:
    """Parse a CSV and commit it to an existing (Processing) dataset, flipping it to Ready."""
    data = await _read_csv(file)

    with psycopg.connect(settings.database_url) as conn:
        if not dataset_exists(conn, dataset_id):
            raise HTTPException(status_code=404, detail=f"Dataset '{dataset_id}' not found.")

        try:
            columns, rows = process_csv(
                data, max_rows=settings.max_rows, max_columns=settings.max_columns
            )
        except IngestError as exc:
            mark_failed(conn, dataset_id, str(exc))
            raise HTTPException(status_code=422, detail=str(exc)) from exc

        write_dataset(conn, dataset_id, columns, rows)

    return IngestResult(
        dataset_id=dataset_id, status="Ready", row_count=len(rows), columns=columns
    )
