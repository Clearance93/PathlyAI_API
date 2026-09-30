namespace Pathly_DTOs
{
    /// <summary>Refresh-token payload for the refresh/logout endpoints. Optional because the token
    /// may instead be supplied as an httpOnly cookie.</summary>
    public class RefreshTokenDto
    {
        public string? RefreshToken { get; set; }
    }
}
