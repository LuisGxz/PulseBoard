"""Postgres writes for the ETL service. Shares the database with the .NET API.

Columns are quoted PascalCase because EF Core mapped the entities that way. Enum-as-string values
(Status, Type) must match the .NET enum member names exactly.
"""

from __future__ import annotations

import uuid
from datetime import datetime, timezone

import psycopg
from psycopg.types.json import Jsonb

from .schemas import ColumnProfile


def _now() -> datetime:
    return datetime.now(timezone.utc)


def dataset_exists(conn: psycopg.Connection, dataset_id: str) -> bool:
    with conn.cursor() as cur:
        cur.execute('SELECT 1 FROM datasets WHERE "Id" = %s', (dataset_id,))
        return cur.fetchone() is not None


def write_dataset(
    conn: psycopg.Connection,
    dataset_id: str,
    columns: list[ColumnProfile],
    rows: list[dict],
) -> None:
    """Replace a dataset's columns and rows, then mark it Ready. All-or-nothing (single transaction)."""
    now = _now()
    with conn.transaction(), conn.cursor() as cur:
        cur.execute('DELETE FROM dataset_rows WHERE "DatasetId" = %s', (dataset_id,))
        cur.execute('DELETE FROM dataset_columns WHERE "DatasetId" = %s', (dataset_id,))

        cur.executemany(
            'INSERT INTO dataset_columns '
            '("Id","DatasetId","Name","Label","Type","Position","NullCount","DistinctCount",'
            '"MinNumeric","MaxNumeric","SampleValues","CreatedAt") '
            "VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)",
            [
                (
                    uuid.uuid4(), dataset_id, c.name, c.label, c.type, c.position,
                    c.null_count, c.distinct_count, c.min_numeric, c.max_numeric,
                    Jsonb(c.sample_values), now,
                )
                for c in columns
            ],
        )

        cur.executemany(
            'INSERT INTO dataset_rows ("Id","DatasetId","Data","CreatedAt") VALUES (%s,%s,%s,%s)',
            [(uuid.uuid4(), dataset_id, Jsonb(r), now) for r in rows],
        )

        cur.execute(
            'UPDATE datasets SET "Status" = %s, "StatusMessage" = NULL, "RowCount" = %s, "UpdatedAt" = %s '
            'WHERE "Id" = %s',
            ("Ready", len(rows), now, dataset_id),
        )


def mark_failed(conn: psycopg.Connection, dataset_id: str, message: str) -> None:
    with conn.transaction(), conn.cursor() as cur:
        cur.execute(
            'UPDATE datasets SET "Status" = %s, "StatusMessage" = %s, "UpdatedAt" = %s WHERE "Id" = %s',
            ("Failed", message[:1000], _now(), dataset_id),
        )
