using SmartWaste.Domain.Workflow.Enums;

namespace SmartWaste.Application.Workflow.DTOs.Responses;

/// <summary>
/// Lightweight workflow summary for list queries and initial mutation responses.
/// </summary>
public class AgentWorkflowSummaryDto
{
    public Guid Id { get; set; }
    public string Objective { get; set; } = string.Empty;
    public AgentWorkflowStatus Status { get; set; }
    public WorkflowStepType CurrentStep { get; set; }
    public Guid InitiatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? FinalOutcome { get; set; }
    public int Version { get; set; }
}
