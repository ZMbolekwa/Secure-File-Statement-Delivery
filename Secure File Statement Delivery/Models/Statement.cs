namespace Secure_File_Statement_Delivery.Models
{
    public class Statement
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public Customer? Customer  { get; set; }
        public string StatementPeriod { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string StoragePath { get; set; } = string.Empty;

        public string FileHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<DownloadToken> DownloadTokens { get; set; } = new List<DownloadToken>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    }
}
