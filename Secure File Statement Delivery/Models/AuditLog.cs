namespace Secure_File_Statement_Delivery.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public int StatementId { get; set; }
        public Statement? Statement  { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? IpAddress { get; set; } 
        public string? UserAgent { get; set; } 
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
    }
}
