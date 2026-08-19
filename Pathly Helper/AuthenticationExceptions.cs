namespace Pathly_Helper
{
    /// <summary>
    /// Thrown when a login attempt fails because the credentials are invalid or the account
    /// does not exist. Callers (e.g. controllers) should map this to an authentication
    /// failure response rather than letting a raw exception surface.
    /// </summary>
    public class InvalidCredentialsException : Exception
    {
        public InvalidCredentialsException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Thrown when an account is locked out after repeated failed login attempts. Callers
    /// should map this to a lockout response so clients can show a retry-later message.
    /// </summary>
    public class AccountLockedException : Exception
    {
        public DateTimeOffset? LockoutEnd { get; }

        public AccountLockedException(string message, DateTimeOffset? lockoutEnd = null)
            : base(message)
        {
            LockoutEnd = lockoutEnd;
        }
    }
}
