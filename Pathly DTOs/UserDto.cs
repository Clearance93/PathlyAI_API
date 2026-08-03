using Pathly_Enums;

namespace Pathly_DTOs
{
    public class UserDto
    {
        public string? Id { get; set; }

        public string? FullName { get; set; }

        public string? Password { get; set; }

        public string? ProofilePictures { get; set; }

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public AuthProviders AuthProvider { get; set; }

        public string? GoogleId { get; set; }

        public string? MicrosoftId { get; set; }

        public string? Subscription { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}