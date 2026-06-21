from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """ETL service configuration. Values are read from the environment (or a local .env)."""

    model_config = SettingsConfigDict(env_file=".env", env_prefix="ETL_", extra="ignore")

    # Postgres shared with the .NET API. snake-cased here, quoted PascalCase columns at write time.
    database_url: str = "postgresql://pulseboard:PulseBoard_Dev!2026@localhost:55432/pulseboard"

    # Shared secret the .NET orchestrator must present in the X-ETL-Key header.
    api_key: str = "pulseboard-etl-dev-key"

    # Safety limits for uploaded CSVs.
    max_rows: int = 200_000
    max_columns: int = 100


settings = Settings()
