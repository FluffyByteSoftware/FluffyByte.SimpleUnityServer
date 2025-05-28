using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FluffyByte.SimpleUnityServer.Game.Managers
{
    internal static class AuthHashManager
    {
        private static readonly string _secret = "YourSuperSecretKey12345";

        public static string ComputeExpectedHash(string nonce)
        {
            string combined = _secret + nonce;
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(combined));

            return Convert.ToHexString(bytes);
        }

        /// <summary>
        /// Validates the client-supplied hash.
        /// </summary>
        public static bool VerifyClientHash(string nonce, string clientHash)
        {
            string expected = ComputeExpectedHash(nonce);
            return string.Equals(clientHash, expected, StringComparison.OrdinalIgnoreCase);
        }
    }
}
