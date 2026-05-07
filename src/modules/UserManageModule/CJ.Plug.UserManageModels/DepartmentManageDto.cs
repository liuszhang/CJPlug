namespace CJ.Plug.UserManageModels
{
    public class DepartmentManageDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Code { get; set; }
        public int? ParentId { get; set; }
        public string? ParentName { get; set; }
        public string? Manager { get; set; }
        public DataStatus Status { get; set; } = DataStatus.Active;
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class CreateDepartmentRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public int? ParentId { get; set; }
        public string? Manager { get; set; }
        public DataStatus Status { get; set; } = DataStatus.Active;
    }

    public class UpdateDepartmentRequest
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Code { get; set; }
        public int? ParentId { get; set; }
        public string? Manager { get; set; }
        public DataStatus Status { get; set; } = DataStatus.Active;
    }
}
