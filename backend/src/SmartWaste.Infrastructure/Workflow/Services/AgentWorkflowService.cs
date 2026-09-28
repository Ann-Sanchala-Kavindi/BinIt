using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartWaste.Application.Common.Exceptions;
using SmartWaste.Application.Common.Models;
using SmartWaste.Application.Workflow.DTOs.Helpers;
using SmartWaste.Application.Workflow.DTOs.Requests;
using SmartWaste.Application.Workflow.DTOs.Responses;
using SmartWaste.Application.Workflow.Interfaces;
using SmartWaste.Domain.Common;
using SmartWaste.Domain.Workflow.Entities;
using SmartWaste.Domain.Workflow.Enums;
using SmartWaste.Infrastructure.Persistence;

namespace SmartWaste.Infrastructure.Workflow.Services;

/// <summary>
/// Authoritative infrastructure service managing AgentWorkflow persistence and atomic lifecycle transitions.
/// </summary>
public class AgentWorkflowService : IAgentWorkflowService
{
    private readonly AppDbContext _db;
    private readonly IAgentWorkflowStateMachine _stateMachine;
    private readonly ILogger<AgentWorkflowService> _logger;

    public AgentWorkflowService(
        AppDbContext db,
        IAgentWorkflowStateMachine stateMachine,
        ILogger<AgentWorkflowService> logger)
    {
        _db = db;
        _stateMachine = stateMachine;
        _logger = logger;
    }

    public async Task<AgentWorkflow> CreateWorkflowAsync(
        string objective,
        Guid initiatedByUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(objective) || objective.Trim().Length < 5)
        {
            throw new ArgumentException("Workflow objective must be at least 5 characters long.", nameof(objective));
        }

        if (objective.Length > 1000)
        {
            throw new ArgumentException("Workflow objective cannot exceed 1000 characters.", nameof(objective));
        }

        var workflow = new AgentWorkflow
        {
            Id = Guid.NewGuid(),
            Objective = objective.Trim(),
            Status = AgentWorkflowStatus.Created,
            CurrentStep = WorkflowStepType.None,
            InitiatedByUserId = initiatedByUserId,
            CreatedAt = DateTime.UtcNow,
            Version = 1
        };

        _db.AgentWorkflows.Add(workflow);

        // Initial transition audit record: null -> Created
        var initialTransition = new AgentWorkflowTransition
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflow.Id,
            FromStatus = null,
            ToStatus = AgentWorkflowStatus.Created,
            Reason = "Workflow created.",
            ChangedByUserId = initiatedByUserId,
            ChangedAt = DateTime.UtcNow
        };

        _db.AgentWorkflowTransitions.Add(initialTransition);

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "AgentWorkflow {WorkflowId} created by User {UserId} with status {Status}.",
            workflow.Id, initiatedByUserId, workflow.Status);

        return workflow;
    }

    public async Task<AgentWorkflow?> GetWorkflowByIdAsync(
        Guid id,
        bool includeDetails = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<AgentWorkflow> query = _db.AgentWorkflows;

        if (includeDetails)
        {
            query = query
                .Include(w => w.InitiatedByUser)
                .Include(w => w.Steps)
                .Include(w => w.Transitions)
                .Include(w => w.Approvals)
                .Include(w => w.ExecutionResults);
        }

        return await query.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<PagedResult<AgentWorkflowSummaryDto>> GetWorkflowsAsync(
        AgentWorkflowListQuery query,
        Guid actorUserId,
        string actorRole,
        CancellationToken cancellationToken = default)
    {
        IQueryable<AgentWorkflow> queryable = _db.AgentWorkflows.AsNoTracking();

        // Row-level visibility: WasteOfficer sees only own workflows; MunicipalManager sees all
        if (actorRole == AppRoles.WasteOfficer)
        {
            queryable = queryable.Where(w => w.InitiatedByUserId == actorUserId);
        }
        else if (actorRole != AppRoles.MunicipalManager)
        {
            throw new ForbiddenException("You do not have permission to view agent workflows.");
        }

        // Status filter validation and application
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            if (Enum.TryParse<AgentWorkflowStatus>(query.Status.Trim(), true, out var status))
            {
                queryable = queryable.Where(w => w.Status == status);
            }
            else
            {
                throw new FluentValidation.ValidationException(new[]
                {
                    new FluentValidation.Results.ValidationFailure(
                        nameof(query.Status),
                        $"Invalid status filter '{query.Status}'. Must be a valid AgentWorkflowStatus.")
                });
            }
        }

        var totalCount = await queryable.CountAsync(cancellationToken);

        var items = await queryable
            .OrderByDescending(w => w.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(w => new AgentWorkflowSummaryDto
            {
                Id = w.Id,
                Objective = w.Objective,
                Status = w.Status,
                CurrentStep = w.CurrentStep,
                InitiatedByUserId = w.InitiatedByUserId,
                CreatedAt = w.CreatedAt,
                UpdatedAt = w.UpdatedAt,
                CompletedAt = w.CompletedAt,
                FinalOutcome = w.FinalOutcome,
                Version = w.Version
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<AgentWorkflowSummaryDto>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<AgentWorkflowDetailDto> GetWorkflowDetailsAsync(
        Guid id,
        Guid actorUserId,
        string actorRole,
        CancellationToken cancellationToken = default)
    {
        var workflow = await _db.AgentWorkflows
            .AsNoTracking()
            .Include(w => w.Steps)
            .Include(w => w.Transitions)
            .Include(w => w.Approvals)
            .Include(w => w.ExecutionResults)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (workflow == null)
        {
            throw new NotFoundException($"AgentWorkflow with ID '{id}' was not found.");
        }

        if (actorRole == AppRoles.WasteOfficer && workflow.InitiatedByUserId != actorUserId)
        {
            throw new NotFoundException($"AgentWorkflow with ID '{id}' was not found.");
        }
        else if (actorRole != AppRoles.WasteOfficer && actorRole != AppRoles.MunicipalManager)
        {
            throw new ForbiddenException("You do not have permission to view agent workflows.");
        }

        return new AgentWorkflowDetailDto
        {
            Id = workflow.Id,
            Objective = workflow.Objective,
            Status = workflow.Status,
            CurrentStep = workflow.CurrentStep,
            InitiatedByUserId = workflow.InitiatedByUserId,
            CreatedAt = workflow.CreatedAt,
            UpdatedAt = workflow.UpdatedAt,
            CompletedAt = workflow.CompletedAt,
            FinalOutcome = workflow.FinalOutcome,
            Version = workflow.Version,
            Steps = workflow.Steps
                .OrderBy(s => s.Sequence)
                .Select(s => new AgentWorkflowStepDto
                {
                    Id = s.Id,
                    Sequence = s.Sequence,
                    StepType = s.StepType,
                    AgentName = s.AgentName,
                    Status = s.Status,
                    Input = JsonElementHelper.ParseJsonElement(s.InputJson),
                    Output = JsonElementHelper.ParseJsonElement(s.OutputJson),
                    Validation = JsonElementHelper.ParseJsonElement(s.ValidationJson),
                    ErrorMessage = s.ErrorMessage,
                    StartedAt = s.StartedAt,
                    CompletedAt = s.CompletedAt
                })
                .ToList(),
            Transitions = workflow.Transitions
                .OrderBy(t => t.ChangedAt)
                .Select(t => new AgentWorkflowTransitionDto
                {
                    Id = t.Id,
                    FromStatus = t.FromStatus,
                    ToStatus = t.ToStatus,
                    Reason = t.Reason,
                    ChangedByUserId = t.ChangedByUserId,
                    ChangedAt = t.ChangedAt
                })
                .ToList(),
            Approvals = workflow.Approvals
                .OrderBy(a => a.DecidedAt)
                .Select(a => new AgentWorkflowApprovalDto
                {
                    Id = a.Id,
                    WorkflowStepId = a.WorkflowStepId,
                    ApprovalStage = a.ApprovalStage,
                    Decision = a.Decision,
                    DecisionReason = a.DecisionReason,
                    DecisionPayload = JsonElementHelper.ParseJsonElement(a.DecisionPayloadJson),
                    DecidedByUserId = a.DecidedByUserId,
                    DecidedAt = a.DecidedAt
                })
                .ToList(),
            ExecutionResults = workflow.ExecutionResults
                .OrderBy(e => e.ExecutedAt)
                .Select(e => new AgentWorkflowExecutionResultDto
                {
                    Id = e.Id,
                    WorkflowStepId = e.WorkflowStepId,
                    ExecutionType = e.ExecutionType,
                    Status = e.Status,
                    Result = JsonElementHelper.ParseJsonElement(e.ResultJson),
                    ErrorMessage = e.ErrorMessage,
                    ExecutedAt = e.ExecutedAt
                })
                .ToList()
        };
    }

    public async Task<IReadOnlyList<AgentWorkflowTransitionDto>> GetWorkflowHistoryAsync(
        Guid id,
        Guid actorUserId,
        string actorRole,
        CancellationToken cancellationToken = default)
    {
        var workflow = await _db.AgentWorkflows
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (workflow == null)
        {
            throw new NotFoundException($"AgentWorkflow with ID '{id}' was not found.");
        }

        if (actorRole == AppRoles.WasteOfficer && workflow.InitiatedByUserId != actorUserId)
        {
            throw new NotFoundException($"AgentWorkflow with ID '{id}' was not found.");
        }
        else if (actorRole != AppRoles.WasteOfficer && actorRole != AppRoles.MunicipalManager)
        {
            throw new ForbiddenException("You do not have permission to view agent workflows.");
        }

        var transitions = await _db.AgentWorkflowTransitions
            .AsNoTracking()
            .Where(t => t.WorkflowId == id)
            .OrderBy(t => t.ChangedAt)
            .Select(t => new AgentWorkflowTransitionDto
            {
                Id = t.Id,
                FromStatus = t.FromStatus,
                ToStatus = t.ToStatus,
                Reason = t.Reason,
                ChangedByUserId = t.ChangedByUserId,
                ChangedAt = t.ChangedAt
            })
            .ToListAsync(cancellationToken);

        return transitions;
    }

    public async Task<AgentWorkflowSummaryDto> StartWorkflowAsync(
        Guid id,
        Guid actorUserId,
        string actorRole,
        CancellationToken cancellationToken = default)
    {
        var workflow = await _db.AgentWorkflows
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken)
            ?? throw new NotFoundException($"AgentWorkflow with ID '{id}' was not found.");

        if (actorRole == AppRoles.WasteOfficer && workflow.InitiatedByUserId != actorUserId)
        {
            throw new NotFoundException($"AgentWorkflow with ID '{id}' was not found.");
        }
        else if (actorRole != AppRoles.WasteOfficer && actorRole != AppRoles.MunicipalManager)
        {
            throw new ForbiddenException("You do not have permission to start agent workflows.");
        }

        var fromStatus = workflow.Status;

        // State machine validation: only Created -> Planning is legal (throws InvalidWorkflowTransitionException -> 409 Conflict if already Planning, Completed, etc.)
        _stateMachine.EnsureCanTransition(fromStatus, AgentWorkflowStatus.Planning);

        // Atomic state and operational progress update
        workflow.Status = AgentWorkflowStatus.Planning;
        workflow.CurrentStep = WorkflowStepType.SharedPlanning;
        workflow.UpdatedAt = DateTime.UtcNow;
        workflow.Version++;

        var transition = new AgentWorkflowTransition
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflow.Id,
            FromStatus = fromStatus,
            ToStatus = AgentWorkflowStatus.Planning,
            Reason = "Workflow started by authorized actor.",
            ChangedByUserId = actorUserId,
            ChangedAt = DateTime.UtcNow
        };

        _db.AgentWorkflowTransitions.Add(transition);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict starting AgentWorkflow {WorkflowId}.", id);
            throw new BusinessRuleConflictException(
                "The workflow was modified concurrently by another operation. Please reload and try again.");
        }

        _logger.LogInformation(
            "AgentWorkflow {WorkflowId} started by User {UserId} ({Role}): Status=Planning, CurrentStep=SharedPlanning.",
            id, actorUserId, actorRole);

        return new AgentWorkflowSummaryDto
        {
            Id = workflow.Id,
            Objective = workflow.Objective,
            Status = workflow.Status,
            CurrentStep = workflow.CurrentStep,
            InitiatedByUserId = workflow.InitiatedByUserId,
            CreatedAt = workflow.CreatedAt,
            UpdatedAt = workflow.UpdatedAt,
            CompletedAt = workflow.CompletedAt,
            FinalOutcome = workflow.FinalOutcome,
            Version = workflow.Version
        };
    }

    public async Task<AgentWorkflow> TransitionAsync(
        Guid workflowId,
        AgentWorkflowStatus toStatus,
        string? reason = null,
        Guid? changedByUserId = null,
        CancellationToken cancellationToken = default)
    {
        var workflow = await _db.AgentWorkflows.FirstOrDefaultAsync(w => w.Id == workflowId, cancellationToken)
            ?? throw new NotFoundException($"AgentWorkflow with ID '{workflowId}' was not found.");

        var fromStatus = workflow.Status;

        // Validate legal state transition through deterministic state machine
        _stateMachine.EnsureCanTransition(fromStatus, toStatus);

        // Update workflow status & timestamps
        workflow.Status = toStatus;
        workflow.UpdatedAt = DateTime.UtcNow;

        if (_stateMachine.IsTerminal(toStatus))
        {
            workflow.CompletedAt = DateTime.UtcNow;
        }

        // Record immutable audit transition
        var transition = new AgentWorkflowTransition
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflow.Id,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            Reason = reason,
            ChangedByUserId = changedByUserId,
            ChangedAt = DateTime.UtcNow
        };

        _db.AgentWorkflowTransitions.Add(transition);

        // Increment optimistic concurrency token
        workflow.Version++;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "AgentWorkflow {WorkflowId} transitioned from {FromStatus} to {ToStatus}. Reason: {Reason}",
            workflow.Id, fromStatus, toStatus, reason ?? "N/A");

        return workflow;
    }

    public async Task<AgentWorkflow> SetCurrentStepAsync(
        Guid workflowId,
        WorkflowStepType stepType,
        CancellationToken cancellationToken = default)
    {
        var workflow = await _db.AgentWorkflows.FirstOrDefaultAsync(w => w.Id == workflowId, cancellationToken)
            ?? throw new NotFoundException($"AgentWorkflow with ID '{workflowId}' was not found.");

        workflow.CurrentStep = stepType;
        workflow.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "AgentWorkflow {WorkflowId} current step updated to {CurrentStep}.",
            workflow.Id, stepType);

        return workflow;
    }

    public async Task<AgentWorkflowStep> AddStepAsync(
        Guid workflowId,
        WorkflowStepType stepType,
        string? agentName = null,
        string? inputJson = null,
        CancellationToken cancellationToken = default)
    {
        var workflow = await _db.AgentWorkflows.FirstOrDefaultAsync(w => w.Id == workflowId, cancellationToken)
            ?? throw new NotFoundException($"AgentWorkflow with ID '{workflowId}' was not found.");

        var maxSequence = await _db.AgentWorkflowSteps
            .Where(s => s.WorkflowId == workflowId)
            .Select(s => (int?)s.Sequence)
            .MaxAsync(cancellationToken) ?? 0;

        var step = new AgentWorkflowStep
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflowId,
            Sequence = maxSequence + 1,
            StepType = stepType,
            AgentName = agentName,
            Status = WorkflowStepStatus.Pending,
            InputJson = inputJson,
            StartedAt = null,
            CompletedAt = null
        };

        _db.AgentWorkflowSteps.Add(step);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Added Step {Sequence} ({StepType}) to AgentWorkflow {WorkflowId}.",
            step.Sequence, step.StepType, workflowId);

        return step;
    }

    public async Task<AgentWorkflowStep> StartStepAsync(
        Guid stepId,
        CancellationToken cancellationToken = default)
    {
        var step = await _db.AgentWorkflowSteps.FirstOrDefaultAsync(s => s.Id == stepId, cancellationToken)
            ?? throw new NotFoundException($"AgentWorkflowStep with ID '{stepId}' was not found.");

        step.Status = WorkflowStepStatus.Running;
        step.StartedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Started AgentWorkflowStep {StepId} ({StepType}) in Workflow {WorkflowId}.",
            step.Id, step.StepType, step.WorkflowId);

        return step;
    }

    public async Task<AgentWorkflowStep> CompleteStepAsync(
        Guid stepId,
        string? outputJson = null,
        string? validationJson = null,
        CancellationToken cancellationToken = default)
    {
        var step = await _db.AgentWorkflowSteps.FirstOrDefaultAsync(s => s.Id == stepId, cancellationToken)
            ?? throw new NotFoundException($"AgentWorkflowStep with ID '{stepId}' was not found.");

        step.Status = WorkflowStepStatus.Completed;
        step.OutputJson = outputJson;
        step.ValidationJson = validationJson;
        step.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Completed AgentWorkflowStep {StepId} ({StepType}) in Workflow {WorkflowId}.",
            step.Id, step.StepType, step.WorkflowId);

        return step;
    }

    public async Task<AgentWorkflowStep> FailStepAsync(
        Guid stepId,
        string errorMessage,
        string? validationJson = null,
        CancellationToken cancellationToken = default)
    {
        var step = await _db.AgentWorkflowSteps.FirstOrDefaultAsync(s => s.Id == stepId, cancellationToken)
            ?? throw new NotFoundException($"AgentWorkflowStep with ID '{stepId}' was not found.");

        step.Status = WorkflowStepStatus.Failed;
        step.ErrorMessage = errorMessage;
        step.ValidationJson = validationJson;
        step.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Failed AgentWorkflowStep {StepId} ({StepType}) in Workflow {WorkflowId}: {Error}",
            step.Id, step.StepType, step.WorkflowId, errorMessage);

        return step;
    }

    public async Task<AgentWorkflowApproval> RecordApprovalAsync(
        Guid workflowId,
        WorkflowApprovalStage stage,
        WorkflowApprovalDecision decision,
        Guid decidedByUserId,
        string? reason = null,
        string? payloadJson = null,
        Guid? stepId = null,
        CancellationToken cancellationToken = default)
    {
        var workflow = await _db.AgentWorkflows.FirstOrDefaultAsync(w => w.Id == workflowId, cancellationToken)
            ?? throw new NotFoundException($"AgentWorkflow with ID '{workflowId}' was not found.");

        var approval = new AgentWorkflowApproval
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflowId,
            WorkflowStepId = stepId,
            ApprovalStage = stage,
            Decision = decision,
            DecisionReason = reason,
            DecisionPayloadJson = payloadJson,
            DecidedByUserId = decidedByUserId,
            DecidedAt = DateTime.UtcNow
        };

        _db.AgentWorkflowApprovals.Add(approval);

        // Participate in aggregate root optimistic concurrency
        workflow.Version++;
        workflow.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Recorded human approval {ApprovalId} for Stage {Stage}, Decision {Decision} in Workflow {WorkflowId}.",
            approval.Id, stage, decision, workflowId);

        return approval;
    }

    public async Task<AgentWorkflowExecutionResult> RecordExecutionResultAsync(
        Guid workflowId,
        WorkflowExecutionType executionType,
        WorkflowExecutionStatus status,
        string? resultJson = null,
        string? errorMessage = null,
        Guid? stepId = null,
        CancellationToken cancellationToken = default)
    {
        var workflow = await _db.AgentWorkflows.FirstOrDefaultAsync(w => w.Id == workflowId, cancellationToken)
            ?? throw new NotFoundException($"AgentWorkflow with ID '{workflowId}' was not found.");

        var result = new AgentWorkflowExecutionResult
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflowId,
            WorkflowStepId = stepId,
            ExecutionType = executionType,
            Status = status,
            ResultJson = resultJson,
            ErrorMessage = errorMessage,
            ExecutedAt = DateTime.UtcNow
        };

        _db.AgentWorkflowExecutionResults.Add(result);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Recorded execution result {ResultId} for Type {Type}, Status {Status} in Workflow {WorkflowId}.",
            result.Id, executionType, status, workflowId);

        return result;
    }
}
