from typing import List, Literal, Optional
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field

from app.models.fleet_resources import FleetCompatibilityStatus


class FleetRouteRequest(BaseModel):
    """Input request for the advisory Fleet & Route Agent."""

    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    objective: str = Field(min_length=5, max_length=500)
    page: int = Field(default=1, ge=1)
    page_size: int = Field(default=20, ge=1, le=50, alias="pageSize")


class RecommendedFleetTask(BaseModel):
    """Structured advisory recommendation for a collection task in a suggested stop sequence."""

    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    task_id: UUID = Field(alias="taskId")
    task_code: str = Field(alias="taskCode")
    sequence: int = Field(ge=1)
    address_text: Optional[str] = Field(default=None, alias="addressText")
    reason: str


class DriverRecommendation(BaseModel):
    """Structured recommendation of an available, unoccupied Driver from authoritative resources."""

    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    driver_id: UUID = Field(alias="driverId")
    display_name: str = Field(alias="displayName")
    reason: str


class VehicleRecommendation(BaseModel):
    """Structured recommendation of an operationally available, unoccupied Vehicle from authoritative resources."""

    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    vehicle_id: UUID = Field(alias="vehicleId")
    registration_number: str = Field(alias="registrationNumber")
    vehicle_type: str = Field(alias="vehicleType")
    reason: str


class RecommendationCompatibility(BaseModel):
    """Authoritative deterministic compatibility outcome evaluated against the ASP.NET Core backend."""

    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    status: FleetCompatibilityStatus
    requires_acknowledgement: bool = Field(alias="requiresAcknowledgement")
    issues: List[str] = Field(default_factory=list)


class _StructuredFleetRoutePayload(BaseModel):
    """Internal schema for model output parsing."""

    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    recommended_tasks: List[RecommendedFleetTask] = Field(default_factory=list, alias="recommendedTasks")
    recommended_driver: Optional[DriverRecommendation] = Field(default=None, alias="recommendedDriver")
    recommended_vehicle: Optional[VehicleRecommendation] = Field(default=None, alias="recommendedVehicle")
    warnings: List[str] = Field(default_factory=list, max_length=15)
    rationale: str


class FleetRouteResult(BaseModel):
    """Advisory-only fleet dispatch proposal returned by the Fleet & Route Agent."""

    model_config = ConfigDict(populate_by_name=True, extra="forbid")

    objective: str
    recommended_tasks: List[RecommendedFleetTask] = Field(default_factory=list, alias="recommendedTasks")
    recommended_driver: Optional[DriverRecommendation] = Field(default=None, alias="recommendedDriver")
    recommended_vehicle: Optional[VehicleRecommendation] = Field(default=None, alias="recommendedVehicle")
    compatibility: Optional[RecommendationCompatibility] = None
    warnings: List[str] = Field(default_factory=list)
    rationale: str

    source_task_page: int = Field(alias="sourceTaskPage")
    source_task_page_size: int = Field(alias="sourceTaskPageSize")
    source_task_total_count: int = Field(alias="sourceTaskTotalCount")
    source_task_total_pages: int = Field(alias="sourceTaskTotalPages")

    agent_name: str = Field(alias="agentName")
    model_name: Optional[str] = Field(default=None, alias="modelName")
    advisory_only: Literal[True] = Field(default=True, alias="advisoryOnly")
    status: Literal["completed", "empty"]
