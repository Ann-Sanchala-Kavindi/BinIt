import logging
import time
from datetime import date
from typing import Optional

import httpx
from langchain_core.tools import tool

from app.core.config import get_settings
from app.models.collection_needs import CollectionNeedsToolResponse

logger = logging.getLogger(__name__)

INTERNAL_AUTH_HEADER = "X-Internal-Service-Key"
COLLECTION_NEEDS_PATH = "/api/v1/internal/ai-tools/collection-needs"
MAX_TRANSIENT_RETRIES = 1
INITIAL_RETRY_BACKOFF_SECONDS = 0.5
_TARGET_TYPES = {"Report", "Bin"}
_COLLECTION_REASONS = {"VerifiedReport", "FullOrBlockedBin", "RoutineCollection"}


def _validate_query(
    target_type: Optional[str],
    collection_reason: Optional[str],
    target_date: Optional[str],
    page: int,
    page_size: int,
) -> Optional[str]:
    if not isinstance(page, int) or isinstance(page, bool) or page < 1:
        raise ValueError("page must be an integer greater than or equal to 1.")
    if not isinstance(page_size, int) or isinstance(page_size, bool) or not 1 <= page_size <= 50:
        raise ValueError("page_size must be an integer between 1 and 50.")
    if target_type is not None and target_type not in _TARGET_TYPES:
        raise ValueError("target_type must be Report or Bin.")
    if collection_reason is not None and collection_reason not in _COLLECTION_REASONS:
        raise ValueError("collection_reason must be VerifiedReport, FullOrBlockedBin, or RoutineCollection.")
    if target_date is None:
        return None
    if not isinstance(target_date, str):
        raise ValueError("target_date must be an ISO calendar date (YYYY-MM-DD).")
    try:
        return date.fromisoformat(target_date).isoformat()
    except ValueError as ex:
        raise ValueError("target_date must be an ISO calendar date (YYYY-MM-DD).") from ex


def fetch_collection_needs(
    target_type: Optional[str] = None,
    collection_reason: Optional[str] = None,
    target_date: Optional[str] = None,
    page: int = 1,
    page_size: int = 20,
    client: Optional[httpx.Client] = None,
) -> CollectionNeedsToolResponse:
    """Retrieve a bounded, sanitized C2 collection-needs page from ASP.NET Core.

    The endpoint is read-only. It applies the backend's current authoritative
    eligibility, urgency, routine-date, and active-task-suppression rules.
    Empty results are returned as an empty paginated response; failures raise.
    """
    normalized_target_date = _validate_query(
        target_type, collection_reason, target_date, page, page_size
    )
    settings = get_settings()
    if not settings.INTERNAL_SERVICE_KEY or not settings.INTERNAL_SERVICE_KEY.strip():
        logger.error("Internal service key is not configured in settings/environment.")
        raise RuntimeError("Internal service authentication key is not configured.")

    params = {"page": page, "pageSize": page_size}
    if target_type is not None:
        params["targetType"] = target_type
    if collection_reason is not None:
        params["collectionReason"] = collection_reason
    if normalized_target_date is not None:
        params["targetDate"] = normalized_target_date

    url = f"{settings.ASPNET_API_BASE_URL.rstrip('/')}{COLLECTION_NEEDS_PATH}"
    headers = {INTERNAL_AUTH_HEADER: settings.INTERNAL_SERVICE_KEY, "Accept": "application/json"}
    should_close_client = client is None
    http_client = client or httpx.Client(timeout=settings.TOOL_HTTP_TIMEOUT)

    try:
        for attempt in range(1, MAX_TRANSIENT_RETRIES + 2):
            try:
                response = http_client.get(url, params=params, headers=headers)
                status = response.status_code
                if status in (401, 403) or 400 <= status < 500:
                    logger.warning("Internal collection-needs request was rejected (HTTP %d).", status)
                    raise RuntimeError(f"Backend rejected collection-needs request (HTTP {status}).")
                if status >= 500:
                    if attempt <= MAX_TRANSIENT_RETRIES:
                        logger.warning("Transient collection-needs backend failure (HTTP %d); retrying once.", status)
                        time.sleep(INITIAL_RETRY_BACKOFF_SECONDS)
                        continue
                    raise RuntimeError(f"Collection-needs backend error (HTTP {status}) after retry.")

                return CollectionNeedsToolResponse.model_validate(response.json())
            except (httpx.ConnectError, httpx.ConnectTimeout, httpx.ReadTimeout, httpx.NetworkError) as ex:
                if attempt <= MAX_TRANSIENT_RETRIES:
                    logger.warning("Transient collection-needs network error (%s); retrying once.", type(ex).__name__)
                    time.sleep(INITIAL_RETRY_BACKOFF_SECONDS)
                    continue
                raise RuntimeError(
                    f"Failed to retrieve collection needs after retry: {type(ex).__name__}"
                ) from None
    finally:
        if should_close_client:
            http_client.close()


@tool("get_collection_needs")
def get_collection_needs(
    target_type: Optional[str] = None,
    collection_reason: Optional[str] = None,
    target_date: Optional[str] = None,
    page: int = 1,
    page_size: int = 20,
) -> CollectionNeedsToolResponse:
    """Retrieve a safe, paginated snapshot of current authoritative collection needs.

    Arguments: target_type may be Report or Bin; collection_reason may be
    VerifiedReport, FullOrBlockedBin, or RoutineCollection; target_date is an
    optional municipality-local ISO calendar date used only for routine-needs
    evaluation. Page metadata must be used to distinguish a partial page from a
    complete queue. This tool cannot create, reschedule, assign, or approve tasks.
    """
    return fetch_collection_needs(
        target_type=target_type,
        collection_reason=collection_reason,
        target_date=target_date,
        page=page,
        page_size=page_size,
    )
