"""CSV parsing, type inference and profiling with pandas.

Pure / DB-free: takes raw CSV bytes and returns normalized rows + column profiles. The DB write
lives in db.py so this module stays trivially unit-testable.
"""

from __future__ import annotations

import io
import math
import re

import pandas as pd

from .schemas import ColumnProfile

_BOOL_TRUE = {"true", "yes", "y", "t"}
_BOOL_FALSE = {"false", "no", "n", "f"}
_SLUG_RE = re.compile(r"[^a-z0-9]+")


class IngestError(ValueError):
    """Raised when a CSV cannot be parsed into a usable dataset."""


def slugify(header: str) -> str:
    slug = _SLUG_RE.sub("_", header.strip().lower()).strip("_")
    return slug or "column"


def _unique_names(headers: list[str]) -> list[str]:
    """Slugify headers, disambiguating collisions with a numeric suffix."""
    seen: dict[str, int] = {}
    names: list[str] = []
    for h in headers:
        base = slugify(h)
        if base in seen:
            seen[base] += 1
            names.append(f"{base}_{seen[base]}")
        else:
            seen[base] = 0
            names.append(base)
    return names


def _infer_type(series: pd.Series) -> str:
    non_null = series.dropna().astype(str).str.strip()
    non_null = non_null[non_null != ""]
    if non_null.empty:
        return "String"

    lowered = non_null.str.lower()
    if set(lowered.unique()) <= (_BOOL_TRUE | _BOOL_FALSE):
        return "Boolean"

    numeric = pd.to_numeric(non_null, errors="coerce")
    if numeric.notna().all():
        return "Number"

    # Dates only after numbers, so plain integers aren't misread as timestamps.
    dates = pd.to_datetime(non_null, errors="coerce", format="mixed", dayfirst=False)
    if dates.notna().mean() >= 0.9:
        return "Date"

    return "String"


def _coerce(value, col_type: str):
    if value is None or (isinstance(value, float) and math.isnan(value)):
        return None
    text = str(value).strip()
    if text == "":
        return None

    if col_type == "Boolean":
        return text.lower() in _BOOL_TRUE
    if col_type == "Number":
        num = pd.to_numeric(text, errors="coerce")
        if pd.isna(num):
            return None
        num = float(num)
        return int(num) if num.is_integer() else num
    if col_type == "Date":
        parsed = pd.to_datetime(text, errors="coerce", format="mixed")
        return None if pd.isna(parsed) else parsed.strftime("%Y-%m-%d")
    return text


def _profile(name: str, label: str, position: int, raw: pd.Series, col_type: str) -> ColumnProfile:
    coerced = [_coerce(v, col_type) for v in raw]
    present = [v for v in coerced if v is not None]
    null_count = len(coerced) - len(present)
    distinct = {str(v) for v in present}

    min_numeric = max_numeric = None
    if col_type == "Number" and present:
        nums = [float(v) for v in present]
        min_numeric, max_numeric = min(nums), max(nums)

    sample = sorted(distinct)[:8]
    return ColumnProfile(
        name=name,
        label=label,
        type=col_type,
        position=position,
        null_count=null_count,
        distinct_count=len(distinct),
        min_numeric=min_numeric,
        max_numeric=max_numeric,
        sample_values=sample,
    )


def process_csv(
    data: bytes, *, max_rows: int, max_columns: int
) -> tuple[list[ColumnProfile], list[dict]]:
    """Parse CSV bytes → (column profiles, normalized JSON-ready rows). Raises IngestError on bad input."""
    try:
        df = pd.read_csv(io.BytesIO(data), dtype=str, keep_default_na=True, skip_blank_lines=True)
    except Exception as exc:  # pandas raises many parser-specific types
        raise IngestError(f"Could not parse CSV: {exc}") from exc

    if df.shape[1] == 0:
        raise IngestError("CSV has no columns.")
    if df.shape[1] > max_columns:
        raise IngestError(f"CSV has {df.shape[1]} columns; the limit is {max_columns}.")
    if len(df) == 0:
        raise IngestError("CSV has a header but no data rows.")
    if len(df) > max_rows:
        raise IngestError(f"CSV has {len(df)} rows; the limit is {max_rows}.")

    headers = [str(h) for h in df.columns]
    names = _unique_names(headers)

    columns: list[ColumnProfile] = []
    coerced_cols: dict[str, list] = {}
    for position, (name, label) in enumerate(zip(names, headers)):
        raw = df.iloc[:, position]
        col_type = _infer_type(raw)
        columns.append(_profile(name, label, position, raw, col_type))
        coerced_cols[name] = [_coerce(v, col_type) for v in raw]

    rows = [
        {name: coerced_cols[name][i] for name in names}
        for i in range(len(df))
    ]
    return columns, rows
