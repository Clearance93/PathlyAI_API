using System.Security.Cryptography;
using System.Text;
using Pathly_DTOs;

namespace Pathly_Helper
{
    /// <summary>
    /// Exact fingerprint for a psychometric profile (Part 13). Deliberately no rounding or
    /// banding — two profiles differing by even a single point across any dimension must
    /// never be treated as the same premium analysis input.
    /// </summary>
    public static class PsychometricProfileFingerprint
    {
        public static string ComputeHash(PsychometricProfileDto profile)
        {
            if (profile is null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            var fingerprint =
                $"r:{profile.Realistic}|i:{profile.Investigative}|a:{profile.Artistic}|" +
                $"s:{profile.Social}|e:{profile.Enterprising}|c:{profile.Conventional}";

            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint));

            return Convert.ToHexString(hashBytes);
        }
    }
}
