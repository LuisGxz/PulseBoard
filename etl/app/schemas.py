from pydantic import BaseModel, Field


class ColumnProfile(BaseModel):
    """Inferred schema + profiling stats for one CSV column. Mirrors the .NET DatasetColumn entity."""

    name: str = Field(description="Machine key (snake_case), used as the JSONB row key.")
    label: str = Field(description="Original CSV header, shown in the UI.")
    type: str = Field(description="One of: String | Number | Date | Boolean.")
    position: int
    null_count: int
    distinct_count: int
    min_numeric: float | None = None
    max_numeric: float | None = None
    sample_values: list[str] = Field(default_factory=list)


class ProfileResult(BaseModel):
    """Dry-run result: the inferred schema and a small preview, with nothing persisted."""

    row_count: int
    columns: list[ColumnProfile]
    preview: list[dict] = Field(description="First few normalized rows.")


class IngestResult(BaseModel):
    """Outcome of a committed ingest. status is the resulting Dataset status string."""

    dataset_id: str
    status: str
    row_count: int
    columns: list[ColumnProfile]
