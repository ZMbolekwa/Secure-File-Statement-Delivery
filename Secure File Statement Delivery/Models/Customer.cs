namespace Secure_File_Statement_Delivery.Models
{
    public class Customer
    {
        public int Id { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<Statement> Statements { get; set; } = new List<Statement>();

    }
}
