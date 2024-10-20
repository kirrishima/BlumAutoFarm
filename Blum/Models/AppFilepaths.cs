using System.Text;

namespace Blum.Models
{
    public readonly struct AppFilepaths
    {
        public static readonly string BaseDirectory = "BlumBotData";

        /// <summary>
        /// Folder path, that contains settings.json
        /// </summary>
        public static readonly string SettingsFolderPath = Path.Combine(BaseDirectory, "Settings");
        /// <summary>
        /// settings.json file pat
        /// </summary>
        public static readonly string ConfigPath = Path.Combine(SettingsFolderPath, "settings.json");

        public static readonly string LogsFolderPath = Path.Combine(BaseDirectory, "Logs");

        public static string LogsFilePath
        {
            get
            {
                DateTime currentDateTime = DateTime.Now;

                string formattedDateTime = currentDateTime.ToString("dd-MM-yyyy_HH-mm-ss");

                string fileName = $"logs_{formattedDateTime}.txt";

                return Path.Combine(LogsFolderPath, fileName);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path">Path without or with file</param>
        /// <returns><see cref="true"/> if was created at least one directory from <paramref name="path"/></returns>
        public static bool EnsureDirectories(string path)
        {
            string directoryPath = Path.GetDirectoryName(path) ?? "";

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                return true;
            }

            return false;
        }

        public static bool SaveWriteToFile(string filepath, string content)
        {
            try
            {
                EnsureDirectories(filepath);

                using (FileStream fs = File.Create(filepath))
                {
                    byte[] info = new UTF8Encoding(true).GetBytes(content);
                    fs.Write(info, 0, info.Length);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public readonly struct TelegramSessions
        {
            /// <summary>
            /// Folder path, that contains accounts.dat and telegram session files
            /// </summary>
            public static readonly string SessionsFolderPath = Path.Combine(BaseDirectory, "TelegramSessions");
            /// <summary>
            /// accounts.dat file path
            /// </summary>
            public static readonly string AccountsDataFilePath = Path.Combine(SessionsFolderPath, "accounts.dat");

            public static string GetWTelegramLogFilePath(string accountName)
            {
                return $"{accountName} WTelegram logs.txt";
            }
        }
    }
}
