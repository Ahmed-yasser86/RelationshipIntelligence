using System;
using System.Security.Cryptography;
using System.Text;

namespace Servicess
{
    public static class DigestActionSigner
    {
        public static TimeSpan DefaultLifetime = TimeSpan.FromDays(7);

        public static string Create(
            string secret, Guid ownerId, Guid personId, Guid deliveryId,
            DateTime? nowUtc = null, TimeSpan? lifetime = null)
        {
            if (string.IsNullOrEmpty(secret))
                throw new ArgumentException("Secret is required.");

            long expiry = ((nowUtc ?? DateTime.UtcNow) + (lifetime ?? DefaultLifetime)).Ticks;
            string payload = $"{ownerId:N}.{personId:N}.{deliveryId:N}.{expiry}";
            string token = $"{payload}.{Sign(secret, payload)}";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(token));
        }

        public static bool TryVerify(
            string secret, string? token,
            out Guid ownerId, out Guid personId, out Guid deliveryId,
            DateTime? nowUtc = null)
        {
            ownerId = Guid.Empty;
            personId = Guid.Empty;
            deliveryId = Guid.Empty;

            try
            {
                if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(token))
                    return false;

                string decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                string[] parts = decoded.Split('.');
                if (parts.Length != 5)
                    return false;

                string payload = string.Join(".", parts, 0, 4);
                if (!CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(Sign(secret, payload)),
                        Encoding.UTF8.GetBytes(parts[4])))
                    return false;

                if (!long.TryParse(parts[3], out long expiryTicks))
                    return false;
                if ((nowUtc ?? DateTime.UtcNow).Ticks > expiryTicks)
                    return false;

                return Guid.TryParseExact(parts[0], "N", out ownerId)
                    && Guid.TryParseExact(parts[1], "N", out personId)
                    && Guid.TryParseExact(parts[2], "N", out deliveryId);
            }
            catch
            {
                return false;
            }
        }

        private static string Sign(string secret, string payload)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        }
    }
}
