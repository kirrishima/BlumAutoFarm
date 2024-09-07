namespace Blum.Models
{
    internal class TelegramSessionsPaths
    {
        public static readonly string SessionsFolder = "sessions";

        public static string GetWTelegramLogFilePath(string accountName)
        {
            return $"{accountName} WTelegram logs.txt";
        }
    }
}