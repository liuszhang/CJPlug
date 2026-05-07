using CJ.Plug.Models.Shared;

namespace CJ.Plug.Models.Auth
{
    /// <summary>
    /// 授权请求实体
    /// </summary>
    public class AuthRequest
    {
        public int Id { get; set; }
        public int OperationType { get; set; }
        public string TargetDescription { get; set; } = string.Empty;
        public string OperationData { get; set; } = string.Empty;
        public string RequestedBy { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int Status { get; set; } = 0; // 0=Pending, 1=Approved, 2=Rejected, 3=Cancelled
        public string? Remark { get; set; }
    }
}
