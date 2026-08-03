namespace Pathly_DTOs
{
    public class ResponseUserDto
    {
        public string? Token { get; set; }

        public DateTime ExpirationDate { get; set; }

        public string? Email { get; set; }
    }
}
