using Microsoft.AspNetCore.Identity;
using Pathly_Enums;

namespace Pathly_Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }

        public string? Password { get; set; }

        public string? ProofilePictures { get; set; }  

        public AuthProviders AuthProvider { get; set; }

        public string? GoogleId { get; set; }

        public string? MicrosoftId { get; set; }

        public string? Subscription { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
