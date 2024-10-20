using Blum.Exceptions;
using Blum.Utilities;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Blum.Models.AppFilepaths;

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

        private static readonly (int Min, int Max) DefaultPointsRange = (180, 220);
        private static readonly int DefaultMaxPlays = 14;

        public static (int Min, int Max) PointsRange = DefaultPointsRange;
        public static int MaxPlays = DefaultMaxPlays;

        //private static readonly ;
        private static readonly JsonSerializerOptions options = new()
        {
            WriteIndented = true
        };
        private static readonly string _defaultJsonString = JsonSerializer.Serialize(new JsonSettings(), options);

        static TelegramSettings()
        {
            TryParseConfig(false);
        }

        /// <summary>
        /// Method that initializes all fields BEFORE access.
        /// Will throw exeption every time if config file is not found even if the first action is to call <see cref="CreateEmptyConfigFile"/>.
        /// </summary>
        /// <exception cref="BlumException"></exception>
        public static void ParseConfig()
        {
            if (File.Exists(ConfigPath))
            {
                string jsonString = File.ReadAllText(ConfigPath);

                try
                {
                    var settings = JsonSerializer.Deserialize<JsonSettings>(jsonString)
                        ?? throw new BlumException("Failed to deserialize JSON.");

                    ApiId = settings.ApiId ?? throw new BlumException("Missing or invalid 'api_id' in config.");
                    if (!IsValidApiId(settings.ApiId))
                        throw new BlumException("Invalid 'api_id' in config.");

                    ApiHash = settings.ApiHash ?? throw new BlumException("Missing or invalid 'api_hash' in config.");
                    if (!IsValidApiHash(settings.ApiHash))
                        throw new BlumException("Invalid 'api_hash' in config.");

                    Proxy = string.IsNullOrWhiteSpace(settings.Proxy) ? null : settings.Proxy;

                    if (settings.farmingSettings != null)
                    {
                        var (minPoints, maxPoints) = (settings.farmingSettings.PointsRange[0], settings.farmingSettings.PointsRange[1]);

                        if (minPoints <= 0 || minPoints > DefaultPointsRange.Max)
                            throw new BlumException($"Invalid points range: min value must be from 1 to {DefaultPointsRange.Max}.");

                        if (maxPoints <= 0 || maxPoints > DefaultPointsRange.Max)
                            throw new BlumException($"Invalid points range: max value must be from 1 to {DefaultPointsRange.Max}.");

                        if (!IsValidMaxPlays(settings.farmingSettings.MaxPlays))
                            throw new BlumException($"Invalid max plays: {settings.farmingSettings.MaxPlays} < 0.");

                        MaxPlays = settings.farmingSettings.MaxPlays;
                        PointsRange = (minPoints, maxPoints);
                    }
                }
                catch (JsonException)
                {
                    throw new BlumException("Invalid JSON format.");
                }
            }
            else
            {
                throw new BlumException("Config file was not found.");
            }
        }

        /// <summary>
        /// Save version of <see cref="ParseConfig"/>, will not throw exceptions.
        /// </summary>
        /// <param name="verbose">If true, error messages will be printed to the console</param>
        /// <returns><see cref="bool"/> if config parsed successfully</returns>
        public static bool TryParseConfig(bool verbose = true)
        {
            Logger logger = new();
            logger.DebugMode = verbose;

            if (File.Exists(ConfigPath))
            {
                string jsonString = File.ReadAllText(ConfigPath);

                bool areApiSettingsValid = true;
                try
                {
                    var settings = JsonSerializer.Deserialize<JsonSettings>(jsonString);
                    if (settings == null)
                    {
                        logger.Debug(Logger.LogMessageType.Error, $"Failed to deserialize JSON from {ConfigPath}");
                        return false;
                    }

                    if (settings.ApiId == null)
                    {
                        logger.Debug(Logger.LogMessageType.Error, $"Missing 'api_id' in config {ConfigPath}");
                        areApiSettingsValid = false;
                    }

                    if (!IsValidApiId(settings.ApiId ?? string.Empty))
                    {
                        logger.Debug(Logger.LogMessageType.Error, $"Invalid 'api_id' in config {ConfigPath}");
                        areApiSettingsValid = false;
                    }

                    if (settings.ApiHash == null)
                    {
                        logger.Debug(Logger.LogMessageType.Error, $"Missing 'api_hash' in config {ConfigPath}");
                        areApiSettingsValid = false;
                    }

                    if (!IsValidApiHash(settings.ApiHash ?? string.Empty))
                    {
                        logger.Debug(Logger.LogMessageType.Error, $"Invalid 'api_hash' in config {ConfigPath}");
                        areApiSettingsValid = false;
                    }

                    if (areApiSettingsValid)
                    {
                        ApiId = settings.ApiId;
                        ApiHash = settings.ApiHash;
                    }

                    Proxy = string.IsNullOrWhiteSpace(settings.Proxy) ? null : settings.Proxy;

                    bool areFarmingSettingsValid = true;
                    if (settings.farmingSettings != null)
                    {
                        var (x, y) = (settings.farmingSettings.PointsRange[0], settings.farmingSettings.PointsRange[1]);
                        if (!IsValidPointsRange(x, y))
                        {
                            logger.Debug(Logger.LogMessageType.Error, $"Invalid points range: range must be of positive integers, and the lower bound must be less than the upper bound.");
                            areFarmingSettingsValid = false;
                        }
                        if (!IsValidMaxPlays(settings.farmingSettings.MaxPlays))
                        {
                            logger.Debug(Logger.LogMessageType.Error, $"Invaild max plays: {settings.farmingSettings.MaxPlays} < 0.");
                            areFarmingSettingsValid = false;
                        }

                        if (areFarmingSettingsValid)
                        {
                            MaxPlays = settings.farmingSettings.MaxPlays;
                            PointsRange = (settings.farmingSettings.PointsRange[0], settings.farmingSettings.PointsRange[1]);
                        }
                    }
                    return areApiSettingsValid && areFarmingSettingsValid;
                }
                catch (JsonException ex)
                {
                    logger.Debug(Logger.LogMessageType.Error, ex.Message);

                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Method that checks api_id for validity. The api_id must be a string, representing integer that fits in 32 bits
        /// </summary>
        /// <param name="ApiId"></param>
        /// <returns></returns>
        public static bool IsValidApiId(string? ApiId) => int.TryParse(ApiId, out int _);

        /// <summary>
        /// Method that checks api_hash. Checks only for a non-empty string,
        /// and for a non-string of spaces. Cannot guarantee that api_hash is 100% valid.
        /// </summary>
        /// <param name="ApiHash"></param>
        /// <returns></returns>
        public static bool IsValidApiHash(string? ApiHash) => !string.IsNullOrWhiteSpace(ApiHash);

        /// <summary>
        /// Method for validating max plays limit
        /// </summary>
        /// <param name="maxPlays"></param>
        /// <returns></returns>
        public static bool IsValidMaxPlays(int maxPlays) => int.IsPositive(maxPlays);

        /// <summary>
        /// Method that validates provided points range. 
        /// </summary>
        /// <param name="Min">Min points</param>
        /// <param name="Max">Max points</param>
        /// <returns></returns>
        public static bool IsValidPointsRange(int Min, int Max)
        {
            /*            if (Min <= 0 || Min > DefaultPointsRange.Max)
                            return false;
                        if (Max <= 0 || Max > DefaultPointsRange.Max)
                            return false;*/
            if (Min <= 0 || Max <= 0)
                return false;
            if (Min > Max)
                return false;

            return true;
        }

        /// <summary>
        /// Method that creates an empty configuration file
        /// </summary>
        /// <returns></returns>
        public static string CreateEmptyConfigFile()
        {
            SaveWriteToFile(ConfigPath, _defaultJsonString);
            return Path.GetFullPath(ConfigPath);
        }

        public static string CreateConfigFileWithCurrentSettings()
        {
            string jsonString = _defaultJsonString;

            var a = new JsonSettings()
            {
                ApiId = ApiId,
                ApiHash = ApiHash,
                Proxy = Proxy,
                farmingSettings = new JsonSettings.FarmingSettings
                {
                    MaxPlays = MaxPlays,
                    PointsRange = [PointsRange.Min, PointsRange.Max]
                }
            };
            jsonString = JsonSerializer.Serialize(a, options);

            SaveWriteToFile(ConfigPath, jsonString);

            return Path.GetFullPath(ConfigPath);
        }

        /// <summary>
        /// A class for deserializing JSON data from a file into an object
        /// </summary>
        private class JsonSettings
        {
            public class FarmingSettings
            {
                [JsonPropertyName("max_plays")]
                public int MaxPlays { get; set; } = DefaultMaxPlays;

                [JsonPropertyName("points_range")]
                public int[] PointsRange { get; set; } = [DefaultPointsRange.Min, DefaultPointsRange.Max];
            }

            [JsonPropertyName("api_id")]
            public string ApiId { get; set; } = "";

            [JsonPropertyName("api_hash")]
            public string ApiHash { get; set; } = "";

            [JsonPropertyName("proxy")]
            public string? Proxy { get; set; } = null;

            [JsonPropertyName("farming_settings")]
            public FarmingSettings farmingSettings { get; set; } = new();
        }
    }
}
