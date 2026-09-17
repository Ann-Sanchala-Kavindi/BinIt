from datetime import datetime
from typing import List, Optional
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field


class VerifiedWasteReportItem(BaseModel):
    """Safe, minimal projection of an authoritative Verified WasteReport for AI tools.
    
    Data minimization rules:
    - Excludes Citizen PII (CitizenId, name, email, phone)
    - Excludes internal officer IDs
    - Excludes private storage keys and photo URLs
    - Excludes internal status history notes
    """

    model_config = ConfigDict(populate_by_name=True)

    id: UUID
    description: str
    waste_type: str = Field(alias="wasteType")
    latitude: float
    longitude: float
    address_text: Optional[str] = Field(default=None, alias="addressText")
    status: str
    created_at: datetime = Field(alias="createdAt")
    verified_at: Optional[datetime] = Field(default=None, alias="verifiedAt")
    attachment_count: int = Field(alias="attachmentCount")


class VerifiedWasteReportsResponse(BaseModel):
    """Paginated response containing safe verified waste report items."""

    model_config = ConfigDict(populate_by_name=True)

    items: List[VerifiedWasteReportItem]
    total_count: int = Field(alias="totalCount")
    page: int
    page_size: int = Field(alias="pageSize")
    total_pages: int = Field(alias="totalPages")
