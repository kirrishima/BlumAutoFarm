using Blum.Models;
using Blum.Models.Json;
using System.Collections.Immutable;
using System.Text.Json;
using System.Web;

namespace Blum.Core
{
    internal partial class BlumBot
    {
        protected static string? ValidateGameId(object? gameId)
        {
            if (gameId is null)
                return null;

            if (gameId is string str)
                return str;
            else if (gameId is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String)
                return jsonElement.GetString();
            else
                return null;
        }

        protected static string SplitAndProcessURL(string URL)
        {
            string[] parts = URL.Split(new[] { "tgWebAppData=" }, StringSplitOptions.None);
            if (parts.Length > 1)
            {
                string dataPart = parts[1].Split(new[] { "&tgWebAppVersion" }, StringSplitOptions.None)[0];
                string decodedData = HttpUtility.UrlDecode(HttpUtility.UrlDecode(URL.Split("tgWebAppData=")[1].Split("&tgWebAppVersion")[0]));
                return decodedData;
            }
            return URL;
        }

        protected static ImmutableList<string> GetPayloadServersIDList()
        {
            string json;

            using (var client = new HttpClient())
            {
                json = client.GetStringAsync(BlumUrls.PAYLOAD_ENDPOINTS_DATABASE).Result;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var root = JsonSerializer.Deserialize<PayloadServersListJson>(json, options);

            return (root?.PayloadServers
                .Where(s => s.Status == 1)
                .Select(s => s.Id)
                .ToList() ?? [])
                .ToImmutableList();
        }

        public void PrintPayloadServersIDAsync()
        {
            var ids = PayloadServersIDList;

            foreach (var id in ids)
            {
                Console.WriteLine(id);
            }
        }

        public void AddElement(string element)
        {
            lock (_listLock)
            {
                if (!PayloadServersIDList.Contains(element))
                {
                    PayloadServersIDList = PayloadServersIDList.Add(element);
                }
            }
        }

        public void RemoveElement(string element)
        {
            lock (_listLock)
            {
                if (PayloadServersIDList.Contains(element))
                {
                    PayloadServersIDList = PayloadServersIDList.Remove(element);
                }
            }
        }
    }
}
