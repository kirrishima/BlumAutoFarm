using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Blum.Exceptions;
using Blum.Utilities;

namespace Blum.Models
{
    internal class TelegramSettings
    {
        /// <summary>
        /// Api Id from <see href="https://my.telegram.org/"/>
        /// </summary>
        public static string ApiId { get; set; } = string.Empty;
        /// <summary>
        /// Api hash from <see href="https://my.telegram.org/"/>
        /// </summary>
        public static string ApiHash { get; set; } = string.Empty;
        /// <summary>
        /// Proxy uri
        /// </summary>
        public static string? Proxy;
        /// <summary>
        /// Filepath for config input/output
        /// </summary>
        public static readonly string settingsDirectory = "Settings";
        public static readonly string configPath = Path.Combine(settingsDirectory, "telegram_settings.json");
        private static readonly Logger logger = new();
        private static readonly JsonSerializerOptions options = new()
        {
            WriteIndented = true
        };
        private static readonly string _defaultJsonString = JsonSerializer.Serialize(new JsonSettings(), options);

        static TelegramSettings()
        {
            if (!Directory.Exists(settingsDirectory))
            {
                Directory.CreateDirectory(settingsDirectory);
            }
        }

        /// <summary>
        /// Method that initializes all fields BEFORE access.
        /// Will throw exeption every time if config file is not found even if the first action is to call <see cref="CreateEmptyConfigFile"/>.
        /// </summary>
        /// <exception cref="BlumException"></exception>
        public static void ParseConfig()
        {
            if (File.Exists(configPath))
            {
                string jsonString = File.ReadAllText(configPath);

                try
                {
                    var settings = JsonSerializer.Deserialize<JsonSettings>(jsonString)
                        ?? throw new BlumException("Failed to deserialize JSON.");

                    ApiId = settings.ApiId ?? throw new BlumException("Missing 'api_id' in config.");
                    if (!IsValidApiId(settings.ApiId))
                        throw new BlumException("Invalid 'api_id' in config.");

                    ApiHash = settings.ApiHash;
                    if (!IsValidApiHash(settings.ApiHash))
                        throw new BlumException("Missing or invalid 'api_hash' in config.");

                    Proxy = string.IsNullOrWhiteSpace(settings.Proxy) ? null : settings.Proxy;
                }
                catch (JsonException)
                {
                    throw new BlumException("Invalid JSON format.");
                }
            }
            else
            {
                string absolutePath = CreateEmptyConfigFile();
                throw new BlumException("Config file was not found.");
            }
        }

        public static bool TryParseConfig(bool verbose = true)
        {
            logger.DebugMode = verbose;
            if (File.Exists(configPath))
            {
                string jsonString = File.ReadAllText(configPath);

                try
                {
                    var settings = JsonSerializer.Deserialize<JsonSettings>(jsonString);
                    if (settings == null)
                    {
                        logger.Debug(Logger.LogMessageType.Error, $"Failed to deserialize JSON from {configPath}");
                        return false;
                    }

                    if (settings.ApiId == null)
                    {
                        logger.Debug(Logger.LogMessageType.Error, $"Missing 'api_id' in config {configPath}");
                        return false;
                    }

                    if (!IsValidApiId(settings.ApiId))
                    {
                        logger.Debug(Logger.LogMessageType.Error, $"Invalid 'api_id' in config {configPath}");
                        return false;
                    }

                    if (!IsValidApiHash(settings.ApiHash))
                    {
                        logger.Debug(Logger.LogMessageType.Error, $"Missing or invalid 'api_hash' in config {configPath}");
                        return false;
                    }

                    ApiId = settings.ApiId;
                    ApiHash = settings.ApiHash;
                    Proxy = string.IsNullOrWhiteSpace(settings.Proxy) ? null : settings.Proxy;

                    logger.DebugMode = false;
                    return true;
                }
                catch (JsonException)
                {
                    logger.DebugMode = false;
                    return false;
                }
            }
            else
            {
                logger.Debug(Logger.LogMessageType.Error, $"{configPath}: file not found");
                logger.DebugMode = false;
                return false;
            }
        }

        public static bool IsValidApiId(string ApiId) => int.TryParse(ApiId, out int _);

        public static bool IsValidApiHash(string ApiHash) => !string.IsNullOrWhiteSpace(ApiHash);

        /// <summary>
        /// Method that creates an empty configuration file
        /// </summary>
        /// <returns></returns>
        public static string CreateEmptyConfigFile()
        {
            using (FileStream fs = File.Create(configPath))
            {
                byte[] info = new UTF8Encoding(true).GetBytes(_defaultJsonString);
                fs.Write(info, 0, info.Length);
            }
            return Path.GetFullPath(configPath);
        }

        public static string CreateConfigFileWithCurrentSettings()
        {
            string jsonString = _defaultJsonString;
            if (IsValidApiHash(ApiHash) && IsValidApiId(ApiId))
                jsonString = JsonSerializer.Serialize(new JsonSettings() { ApiId = ApiId, ApiHash = ApiHash, Proxy = Proxy }, options);
            using (FileStream fs = File.Create(configPath))
            {
                byte[] info = new UTF8Encoding(true).GetBytes(jsonString);
                fs.Write(info, 0, info.Length);
            }
            return Path.GetFullPath(configPath);
        }

        /// <summary>
        /// A class for deserializing JSON data from a file into an object
        /// </summary>
        private class JsonSettings
        {
            [JsonPropertyName("api_id")]
            public string ApiId { get; set; } = "";

            [JsonPropertyName("api_hash")]
            public string ApiHash { get; set; } = "";

            [JsonPropertyName("proxy")]
            public string? Proxy { get; set; } = null;
        }
    }
}
