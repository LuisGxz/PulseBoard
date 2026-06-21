import pytest

from app.ingest import IngestError, process_csv, slugify

LIMITS = dict(max_rows=10_000, max_columns=100)

CSV = (
    "Order Date,Revenue (USD),Region,Is Paid,Seats\n"
    "2026-01-05,1200.50,North America,true,5\n"
    "2026-01-06,980,Europe,false,3\n"
    "2026-01-07,,APAC,yes,8\n"
)


def _cols(csv: str):
    columns, rows = process_csv(csv.encode(), **LIMITS)
    return {c.name: c for c in columns}, rows


def test_slugify_normalizes_headers():
    assert slugify("Revenue (USD)") == "revenue_usd"
    assert slugify("  Order Date ") == "order_date"
    assert slugify("???") == "column"


def test_infers_types():
    cols, _ = _cols(CSV)
    assert cols["order_date"].type == "Date"
    assert cols["revenue_usd"].type == "Number"
    assert cols["region"].type == "String"
    assert cols["is_paid"].type == "Boolean"
    assert cols["seats"].type == "Number"


def test_coerces_values_in_rows():
    _, rows = _cols(CSV)
    assert rows[0]["revenue_usd"] == 1200.5
    assert rows[1]["seats"] == 3  # integral floats become ints
    assert rows[0]["is_paid"] is True
    assert rows[2]["is_paid"] is True  # "yes"
    assert rows[0]["order_date"] == "2026-01-05"


def test_profiles_nulls_and_numeric_range():
    cols, _ = _cols(CSV)
    assert cols["revenue_usd"].null_count == 1  # the empty cell
    assert cols["revenue_usd"].min_numeric == 980.0
    assert cols["revenue_usd"].max_numeric == 1200.5
    assert cols["region"].distinct_count == 3


def test_duplicate_headers_get_unique_names():
    cols, _ = _cols("A,A,B\n1,2,3\n")
    assert set(cols.keys()) == {"a", "a_1", "b"}


def test_empty_csv_raises():
    with pytest.raises(IngestError):
        process_csv(b"", **LIMITS)


def test_header_only_raises():
    with pytest.raises(IngestError):
        process_csv(b"a,b,c\n", **LIMITS)


def test_row_limit_enforced():
    csv = "n\n" + "\n".join(str(i) for i in range(5))
    with pytest.raises(IngestError):
        process_csv(csv.encode(), max_rows=3, max_columns=100)
