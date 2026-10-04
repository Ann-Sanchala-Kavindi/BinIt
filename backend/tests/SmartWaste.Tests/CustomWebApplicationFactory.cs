using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SmartWaste.Application.Reporting.Interfaces;
using SmartWaste.Application.Workflow.DTOs.Transport;
using SmartWaste.Application.Workflow.Interfaces;
using SmartWaste.Tests.Reporting.Fakes;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace SmartWaste.Tests;

[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "IntegrationTests";
}

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public FakeFileStorageService FakeStorage { get; } = new();

    private sealed class FakePythonOrchestrationClient : IPythonOrchestrationClient
    {
        private static JsonElement Json(string value)
        {
            using var document = JsonDocument.Parse(value);
            return document.RootElement.Clone();
        }

        public Task<PythonOrchestrationEnvelope> StartAsync(PythonWorkflowStartRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PythonOrchestrationEnvelope
            {
                WorkflowId = request.WorkflowId,
                Objective = request.Objective,
                Status = "Paused",
                CurrentPhase = "PausedForCollectionApproval",
                ApprovalStage = "CollectionPlanning",
                PlannerResult = Json("{}"),
                WasteAnalysisResult = Json("{}"),
                CollectionPlanningResult = Json("{\"status\":\"completed\",\"isCompleteSnapshot\":true,\"candidateGroups\":[],\"separateHandling\":[],\"deferredNeeds\":[]}"),
                CompletedSpecialists = ["WasteAnalysis", "CollectionPlanning"]
            });

        public Task<PythonOrchestrationEnvelope> ResumeAfterCollectionApprovalAsync(PythonWorkflowResumeRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PythonOrchestrationEnvelope
            {
                WorkflowId = request.Workflow.WorkflowId,
                Objective = request.Workflow.Objective,
                Status = "Paused",
                CurrentPhase = "PausedForDispatchApproval",
                ApprovalStage = "FleetDispatch",
                FleetRouteResult = Json("{\"fleetPlans\":[]}"),
                ValidationOperationsResult = Json("{\"validationOutcome\":\"ReadyForHumanReview\",\"planReviews\":[]}"),
                CompletedSpecialists = ["WasteAnalysis", "CollectionPlanning", "FleetRoute", "ValidationOperations"]
            });

        public Task<PythonOrchestrationEnvelope> ResumeAfterReportVerificationAsync(PythonWorkflowResumeRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Report verification continuation is not used by this test factory.");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            var builtConfig = config.Build();
            var existingConn = builtConfig.GetConnectionString("DefaultConnection");
            var connToUse = (!string.IsNullOrWhiteSpace(existingConn) && !existingConn.Contains("CHANGE_ME"))
                ? existingConn
                : "Host=localhost;Port=5432;Database=smartwaste_db;Username=postgres;Password=postgres";

            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connToUse,
                ["Jwt:Key"] = builtConfig["Jwt:Key"] ?? "ThisIsASecretKeyForSmartWasteDevelopmentOnly12345!",
                ["Jwt:Issuer"] = "SmartWaste.Api",
                ["Jwt:Audience"] = "SmartWaste.Clients",
                ["Jwt:ExpiryMinutes"] = "60",
                ["Storage:Supabase:BaseUrl"] = "https://mock.supabase.co",
                ["Storage:Supabase:SecretKey"] = "sb_secret_mock_test_key_12345",
                ["Storage:Supabase:Bucket"] = "waste-report-attachments",
                ["Storage:Supabase:SignedUrlExpirySeconds"] = "900",
                ["InternalService:ApiKey"] = "TestInternalServiceKey_12345!",
                ["ReportTriggeredWorkflowWorker:Enabled"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IFileStorageService>();
            services.AddSingleton<IFileStorageService>(FakeStorage);
            services.RemoveAll<IPythonOrchestrationClient>();
            services.AddScoped<IPythonOrchestrationClient, FakePythonOrchestrationClient>();
        });
    }
}
