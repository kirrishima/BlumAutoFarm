using Blum.Exceptions;
using Blum.Models;
using Blum.Models.Json;
using Blum.Utilities;
using System.Text.Json;
using TL;
using static Blum.Utilities.RandomUtility.Random;

namespace Blum.Core
{
    internal partial class BlumBot
    {
        public void CloseSession()
        {
            ((IDisposable)_session).Dispose();
        }

        public async Task CreaateSessionIfNeeded()
        {
            await _client.LoginUserIfNeeded();
            _client.Reset();
        }

        public async Task ReloginAsync()
        {
            _session.RecreateRestClient();
            await LoginAsync();
        }

        public async Task<bool> RefreshUsingTokenAsync()
        {
            _logger.Debug(Logger.LogMessageType.Warning, messages: ("RefreshAsync()", null));

            string authToken = _session.GetHeader("Authorization");
            _session.ClearHeaders();

            var data = new { refresh = _refreshToken };
            var jsonData = JsonSerializer.Serialize(data);
            var (rawResponse, response, _) = await _session.TryPostAsync(BlumUrls.Refresh, jsonData);

            if (rawResponse?.IsSuccessStatusCode != true)
            {
                await Task.Delay(RandomDelayMilliseconds(Delay.BeforeRequest));
                (rawResponse, response, _) = await _session.TryPostAsync(BlumUrls.Refresh, jsonData);
            }

            var responseJson = JsonSerializer.Deserialize<Dictionary<string, object>>(response ?? "{}");

            bool success = false;

            if (responseJson?.TryGetValue("access", out object? obj) == true)
            {
                if (obj is string strValue)
                {
                    _session.SetHeader("Authorization", $"Bearer {strValue}");
                }
                else if (obj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String)
                {
                    _session.SetHeader("Authorization", $"Bearer {jsonElement.GetString()}");
                }
                success = true;
            }

            if (responseJson?.TryGetValue("refresh", out obj) == true)
            {
                if (obj is string strValue)
                {
                    _refreshToken = strValue;
                }
                else if (obj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.String)
                {
                    if (!string.IsNullOrWhiteSpace(jsonElement.GetString()))
                    {
                        _refreshToken = jsonElement.GetString();
                    }
                }
                success = true;
            }

            if (!success)
            {
                _session.SetHeader("authToken", authToken);
            }

            await Task.Delay(1000);

            return success;
        }

        public async Task<bool> LoginAsync()
        {
            _logger.Debug(Logger.LogMessageType.Warning, messages: ("LoginAsync()", null));
            try
            {
                var jsonData = new { query = await GetTGWebDataAsync() };
                var json = JsonSerializer.Serialize(jsonData);
                var (RawResponse, ResponseContent, exception) = await _session.TryPostAsync(BlumUrls.ProviderMiniApp, json);

                if (RawResponse?.IsSuccessful != true || exception != null)
                {
                    await Task.Delay(3000);
                    (RawResponse, ResponseContent, exception) = await _session.TryPostAsync(BlumUrls.ProviderMiniApp, json);
                }

                if (RawResponse?.IsSuccessful != true || exception != null)
                {
                    throw new BlumFatalError($"Error while logging in blum: request to blum api failed.", exception);
                }

                _logger.Debug((_accountName, ConsoleColor.DarkCyan), ($"LoginAsync json: {ResponseContent}", null));

                if (string.IsNullOrWhiteSpace(ResponseContent))
                {
                    throw new BlumFatalError($"Error while logging in blum: json content is empty");
                }

                var accessToken = JsonSerializer.Deserialize<BlumAccessTokenJson>(ResponseContent);
                if (accessToken == null || accessToken.Token == null || string.IsNullOrWhiteSpace(accessToken.Token.Access) || string.IsNullOrWhiteSpace(accessToken.Token.Refresh))
                {
                    throw new BlumFatalError($"Error while logging in blum: no access token recived");
                }

                _session.SetHeader("Authorization", string.Format("Bearer {0}", accessToken.Token.Access));
                _refreshToken = accessToken.Token.Refresh;

                _logger.Debug(
                    (_accountName, ConsoleColor.DarkCyan),
                    ($"LoginAsync Token.Access: {accessToken.Token.Access}", null),
                    ($"LoginAsync Token.RefreshAsync: {accessToken.Token.Refresh}", null)
                    );

                return true;
            }
            catch (Exception ex)
            {
                string message = $"An error occurred during logging in: {ex.Message}";
                _logger.Error((_accountName, ConsoleColor.DarkCyan), (message, null));
                throw new BlumFatalError(message, ex.InnerException);
            }
        }

        private async Task<string?> GetTGWebDataAsync()
        {
            _logger.Debug(Logger.LogMessageType.Warning, messages: ("GetTGWebDataAsync()", null));
            try
            {
                var myself = await _client.LoginUserIfNeeded();
                var botPeer = await _client.Contacts_ResolveUsername("BlumCryptoBot");
                var bot = botPeer.User;

                var inputPeer = new InputPeerUser(user_id: bot.ID, access_hash: bot.access_hash);
                var inputUserBase = new InputUser(user_id: bot.ID, access_hash: bot.access_hash);

                var webViewResult = await _client.Messages_RequestWebView(
                    peer: inputPeer,
                    bot: inputUserBase,
                    platform: "android",
                    from_bot_menu: false,
                    url: "https://telegram.blum.codes/"
                );

                _client.Reset();

                _logger.Debug(
                    (_accountName, ConsoleColor.DarkCyan),
                    ($"GetTGWebDataAsync: ", null),
                    ($"webViewResult type: {webViewResult.GetType()}", null)
                    );

                await Task.Delay(1000);
                if (webViewResult is WebViewResultUrl webViewResultUrl)
                    return SplitAndProcessURL(webViewResultUrl.url);
                else
                    throw new BlumFatalError($"Wrong result's data type parsing RequestWebView: got {webViewResult.GetType()}, expected {typeof(WebViewResultUrl)}");
            }
            catch (Exception ex)
            {
                throw new BlumFatalError("Unexpected error while getting data from telegram", ex);
            }
        }
    }
}
