import logging
from typing import Optional

from langchain_core.language_models import BaseChatModel
from langchain_core.language_models.fake_chat_models import FakeChatModel

from app.core.config import Settings, get_settings

logger = logging.getLogger(__name__)


def get_chat_model(
    settings: Optional[Settings] = None,
    model_override: Optional[BaseChatModel] = None,
) -> BaseChatModel:
    """Factory provider for the configured LangChain chat model.

    Design rules:
    - If model_override is supplied (e.g. for testing/fixtures), it is returned directly.
    - Default provider is 'mock' which produces an offline FakeChatModel.
    - Supported real provider is 'google' via langchain-google-genai (ChatGoogleGenerativeAI).
    - Never leaks API keys in logs or exceptions.
    - Fails safely with clear RuntimeError if an unsupported or unconfigured provider is requested.
    """
    if model_override is not None:
        return model_override

    active_settings = settings or get_settings()
    provider = active_settings.LLM_PROVIDER.lower().strip()

    if provider == "mock":
        logger.debug("Using mock LangChain chat model provider.")
        return FakeChatModel()

    if provider == "google":
        try:
            from langchain_google_genai import ChatGoogleGenerativeAI
        except ImportError:
            raise RuntimeError(
                "LLM provider 'google' requires 'langchain-google-genai'. "
                "Ensure dependencies are installed or use LLM_PROVIDER='mock'."
            ) from None

        if not active_settings.LLM_API_KEY or not active_settings.LLM_API_KEY.strip():
            raise RuntimeError(
                "LLM provider 'google' requested but LLM_API_KEY is not configured. "
                "Set the LLM_API_KEY environment variable."
            )

        model_name = active_settings.LLM_MODEL.strip()
        if not model_name or model_name == "mock-model":
            model_name = "gemini-1.5-flash"

        logger.info("Configuring Google Gemini chat model [model=%s]", model_name)
        return ChatGoogleGenerativeAI(
            model=model_name,
            google_api_key=active_settings.LLM_API_KEY,
            temperature=active_settings.LLM_TEMPERATURE,
            timeout=active_settings.LLM_TIMEOUT,
        )

    raise RuntimeError(
        f"Unsupported LLM provider: '{provider}'. Supported providers: 'mock', 'google'."
    )
