namespace MoneyManager.Api.Administration.Models;

public sealed class AdminAuthOptions
{
    public string Issuer { get; set; } = "MoneyManager.Admin";
    public string Audience { get; set; } = "MoneyManager.Admin.Users";
    public string SecretKey { get; set; } = "change-this-admin-secret-key-with-at-least-32-characters";
    public int TokenExpirationMinutes { get; set; } = 60;
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = AdminRoles.Admin;
    public List<AdminUserCredential> Users { get; set; } = [];
}
