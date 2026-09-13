using Microsoft.AspNetCore.Identity;
namespace Secure_File_Statement_Delivery.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
    }
}
