namespace MoneyManager.Api.Administration.Models;

public static class AdminRoles
{
    public const string Viewer = "viewer";
    public const string Operator = "operator";
    public const string Admin = "admin";

    public static bool IsValid(string role)
        => string.Equals(role, Viewer, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, Operator, StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, Admin, StringComparison.OrdinalIgnoreCase);
}

public static class AdminPolicies
{
    public const string Viewer = "AdminViewer";
    public const string Operator = "AdminOperator";
    public const string Admin = "AdminAdmin";
}

public sealed class AdminUserCredential
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = AdminRoles.Viewer;
}