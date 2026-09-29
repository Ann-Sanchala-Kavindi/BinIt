using SmartWaste.Application.Common.Models;
using SmartWaste.Application.Workflow.DTOs.Requests;
using SmartWaste.Application.Workflow.DTOs.Responses;
using SmartWaste.Domain.Workflow.Entities;
using SmartWaste.Domain.Workflow.Enums;

namespace SmartWaste.Application.Workflow.Interfaces;

/// <summary>
/// Application service interface for authoritatively creating, updating, querying, and auditing AgentWorkflow aggregates.
/// </summary>
public interface IAgentWorkflowService
{
    /// <summary>
    /// Creates a new AgentWorkflow in Created state with an initial audit transition record.
    /// </summary>
    Task<AgentWorkflow> CreateWorkflowAsync(
        string objective,
        Guid initiatedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a workflow by ID with optional eager loading of child collections.
    /// </summary>
    Task<AgentWorkflow?> GetWorkflowByIdAsync(
        Guid id,
        bool includeDetails = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paged list of workflow summaries filtered by row-level role access and optional status.
    /// </summary>
    Task<PagedResult<AgentWorkflowSummaryDto>> GetWorkflowsAsync(
        AgentWorkflowListQuery query,
        Guid actorUserId,
        string actorRole,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the complete structured view of a specific workflow, validating row-level access.
    /// </summary>
    Task<AgentWorkflowDetailDto> GetWorkflowDetailsAsync(
        Guid id,
        Guid actorUserId,
        string actorRole,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the chronological audit transition history for a workflow, validating row-level access.
    /// </summary>
    Task<IReadOnlyList<AgentWorkflowTransitionDto>> GetWorkflowHistoryAsync(
        Guid id,
        Guid actorUserId,
        string actorRole,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically starts a Created workflow, transitioning it to Planning and setting CurrentStep to SharedPlanning.
    /// Does not trigger external AI microservice execution.
    /// </summary>
    Task<AgentWorkflowSummaryDto> StartWorkflowAsync(
        Guid id,
        Guid actorUserId,
        string actorRole,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically transitions the workflow from its current status to a target status,
    /// validating against the state machine, setting terminal timestamps if applicable,
    /// recording an immutable audit transition row, and enforcing optimistic concurrency.
    /// </summary>
    Task<AgentWorkflow> TransitionAsync(
        Guid workflowId,
        AgentWorkflowStatus toStatus,
        string? reason = null,
        Guid? changedByUserId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the logical execution position of the workflow.
    /// </summary>
    Task<AgentWorkflow> SetCurrentStepAsync(
        Guid workflowId,
        WorkflowStepType stepType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a discrete specialist or system execution step to the workflow.
    /// </summary>
    Task<AgentWorkflowStep> AddStepAsync(
        Guid workflowId,
        WorkflowStepType stepType,
        string? agentName = null,
        string? inputJson = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a step as Running and sets its StartedAt timestamp.
    /// </summary>
    Task<AgentWorkflowStep> StartStepAsync(
        Guid stepId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a step as Completed with structured output and optional validation summary.
    /// </summary>
    Task<AgentWorkflowStep> CompleteStepAsync(
        Guid stepId,
        string? outputJson = null,
        string? validationJson = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a step as Failed with an error message and optional validation diagnostic.
    /// </summary>
    Task<AgentWorkflowStep> FailStepAsync(
        Guid stepId,
        string errorMessage,
        string? validationJson = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a human-in-the-loop review decision (infrastructure for future approval workflows).
    /// </summary>
    Task<AgentWorkflowApproval> RecordApprovalAsync(
        Guid workflowId,
        WorkflowApprovalStage stage,
        WorkflowApprovalDecision decision,
        Guid decidedByUserId,
        string? reason = null,
        string? payloadJson = null,
        Guid? stepId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists an authoritative system execution result (infrastructure for future execution bridges).
    /// </summary>
    Task<AgentWorkflowExecutionResult> RecordExecutionResultAsync(
        Guid workflowId,
        WorkflowExecutionType executionType,
        WorkflowExecutionStatus status,
        string? resultJson = null,
        string? errorMessage = null,
        Guid? stepId = null,
        CancellationToken cancellationToken = default);
}
