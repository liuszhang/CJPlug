namespace CJ.Plug.UserManageModels
{
    public class UserManageDto
    {
        public int Id { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public List<string> RoleNames { get; set; } = [];
        public DataStatus Status { get; set; } = DataStatus.Active;
        public DateTime CreatedAt { get; set; }
    }

    public class CreateUserRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public int? DepartmentId { get; set; }
        public List<string>? RoleNames { get; set; }
        public DataStatus Status { get; set; } = DataStatus.Active;
    }

    public class UpdateUserRequest
    {
        public int Id { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public int? DepartmentId { get; set; }
        public List<string>? RoleNames { get; set; }
        public DataStatus Status { get; set; } = DataStatus.Active;
    }

    public class AssignRolesRequest
    {
        public int UserId { get; set; }
        public List<string> RoleNames { get; set; } = [];
    }
}
