namespace SavedByTheMaid.Api.Auth;

/// <summary>
/// System roles
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Employee = "Employee";
    public const string Customer = "Customer";
    
    public static readonly string[] All = { Admin, Employee, Customer };
}

/// <summary>
/// Authorization policies
/// </summary>
public static class Policies
{
    public const string AdminOnly = "AdminOnly";
    public const string EmployeeOrAdmin = "EmployeeOrAdmin";
    public const string Authenticated = "Authenticated";
}
