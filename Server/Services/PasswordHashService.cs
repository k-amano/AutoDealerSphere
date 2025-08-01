namespace AutoDealerSphere.Server.Services
{
    public static class PasswordHashService
    {
        /// <summary>
        /// パスワードをハッシュ化する
        /// </summary>
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        /// <summary>
        /// パスワードとハッシュを検証する
        /// </summary>
        public static bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}