namespace CJ.Plug.UserManageModels
{
    public class RoleManageDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? RoleType { get; set; }
        public bool IsSystem => RoleType == "系统角色";
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class CreateRoleRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? RoleType { get; set; }
    }

    public class UpdateRoleRequest
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? RoleType { get; set; }
    }
}
