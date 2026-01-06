namespace SavedByTheMaid.Api.Auth;

/// <summary>
/// Roles del sistema
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Employee = "Employee";
    public const string Customer = "Customer";
    
    public static readonly string[] All = { Admin, Employee, Customer };
}

/// <summary>
/// Políticas de autorización
/// </summary>
public static class Policies
{
    public const string AdminOnly = "AdminOnly";
    public const string EmployeeOrAdmin = "EmployeeOrAdmin";
    public const string Authenticated = "Authenticated";
}
