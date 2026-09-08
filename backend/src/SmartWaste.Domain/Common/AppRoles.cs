namespace SmartWaste.Domain.Common;

/// <summary>
/// Standard application roles for the Smart Waste Management System.
/// </summary>
public static class AppRoles
{
    public const string Citizen = "Citizen";
    public const string WasteOfficer = "WasteOfficer";
    public const string Driver = "Driver";
    public const string MunicipalManager = "MunicipalManager";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Citizen,
        WasteOfficer,
        Driver,
        MunicipalManager
    };
}
