from functools import lru_cache
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    """Foundation configuration for the SmartWaste AI Service."""

    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        extra="ignore",
    )

    APP_NAME: str = "SmartWaste AI Service"
    APP_ENV: str = "development"
    HOST: str = "127.0.0.1"
    PORT: int = 8000
    ASPNET_API_BASE_URL: str = "http://localhost:5276"
    INTERNAL_SERVICE_KEY: str = ""
    TOOL_HTTP_TIMEOUT: float = 10.0

    # LLM Provider Configuration
    LLM_PROVIDER: str = "mock"
    LLM_MODEL: str = "mock-model"
    LLM_API_KEY: str = ""
    LLM_TEMPERATURE: float = 0.0
    LLM_TIMEOUT: float = 30.0


@lru_cache
def get_settings() -> Settings:
    """Cached singleton provider for application settings."""
    return Settings()
