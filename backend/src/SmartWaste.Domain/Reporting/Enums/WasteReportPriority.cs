namespace SmartWaste.Domain.Reporting.Enums;

/// <summary>
/// Operational priority assigned to a waste report.
/// Priority is nullable and remains null during Component 1 verification.
/// </summary>
public enum WasteReportPriority
{
    Low,
    Medium,
    High,
    Urgent
}
