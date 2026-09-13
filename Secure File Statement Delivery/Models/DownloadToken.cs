namespace Secure_File_Statement_Delivery.Models
{
    public class DownloadToken
    {
        public int Id { get; set; }
        public int StatementId { get; set; }
        public Statement? Statement { get; set; }
        public string TokenHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; } 
        public DateTime? UsedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
    }
}
