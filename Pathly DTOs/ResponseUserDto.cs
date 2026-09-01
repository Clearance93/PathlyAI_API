namespace Pathly_DTOs
{
    public class ResponseUserDto
    {
        public string? Token { get; set; }

        public DateTime ExpirationDate { get; set; }

        public string? Email { get; set; }

        /// <summary>ApplicationUser.Id of the logged-in user — the UI stores this and sends it
        /// back when persisting psychometric assessments against the account.</summary>
        public string? UserId { get; set; }

        public string? FullName { get; set; }
    }
}
