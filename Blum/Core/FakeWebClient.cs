using Blum.Models;
using RestSharp;
using System.Net;

namespace Blum.Core
{
    public class FakeWebClient : IDisposable
    {
        private RestClient _client;
        private RestClientOptions _options;
        private DeviceProfile _currentProfile;
        private Dictionary<string, string> _headers = [];

        private bool _disposed = false;

        public FakeWebClient(DeviceProfile? deviceProfile = null, string? proxyUri = null)
        {
            InitializeRestClient(deviceProfile, proxyUri);
        }

        ~FakeWebClient()
        {
            Dispose(false);
        }

        private void InitializeRestClient(DeviceProfile? deviceProfile, string? proxyUri)
        {
            if (deviceProfile is null)
            {
                Random random = new Random();
                int index = random.Next(DeviceProfiles.Profiles.Length);
                deviceProfile = DeviceProfiles.Profiles[index];
            }
            _currentProfile = deviceProfile ?? DeviceProfiles.AndroidPixel5;

            _options = new RestClientOptions
            {
                Proxy = !string.IsNullOrEmpty(proxyUri) ? new WebProxy(proxyUri, true) : null,
                ThrowOnAnyError = true,
                Timeout = TimeSpan.FromSeconds(60),
                UserAgent = _currentProfile.UserAgent
            };

            _client = new RestClient(_options);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _client?.Dispose();
                _options = null;
                _currentProfile = null;
                _headers.Clear();
                _headers = null;
            }

            _disposed = true;
        }

        public void RecreateRestClient()
        {
            var existingHeaders = _client.DefaultParameters;

            Dispose();

            string? proxyUri = (_options.Proxy as WebProxy)?.Address?.ToString();
            InitializeRestClient(_currentProfile, proxyUri);

            foreach (var parameter in existingHeaders)
            {
                _client.AddDefaultHeader(parameter.Name, parameter.Value.ToString());
            }
        }

        public void SetHeader(string name, string value)
        {
            if (_headers.ContainsKey(name))
            {
                _headers[name] = value;
            }
            else
            {
                _headers.Add(name, value);
            }
        }

        public string GetHeader(string name)
        {
            if (_headers.ContainsKey(name))
            {
                return _headers[name];
            }

            return string.Empty;
        }

        public void ClearHeaders(params string[] names)
        {
            if (names.Length == 0)
            {
                _headers.Clear();
            }
            else
            {
                foreach (var name in names)
                {
                    if (_headers.ContainsKey(name))
                    {
                        _headers.Remove(name);
                    }
                }
            }
        }

        public void SetDeviceProfile(DeviceProfile newProfile)
        {
            _currentProfile = newProfile;
            SetHeader("User-Agent", _currentProfile.UserAgent);
        }

        public async Task<(RestResponse? RestResponse, string? ResponseContent, Exception? Exception)> TryGetAsync(string url)
        {
            RestResponse? response = null;
            try
            {
                var request = new RestRequest(url, Method.Get);

                if (_headers != null && _headers.Any())
                    request.AddHeaders(_headers);

                response = await _client.ExecuteAsync(request);

                if (response.IsSuccessful)
                    return (response, response.Content, null);
                else
                    return (response, null, null);
            }
            catch (Exception e)
            {
                return (response, null, e);
            }
        }

        public async Task<(RestResponse? restResponse, string? responseContent, Exception? exception)> TryPostAsync(string url, string? jsonData = null)
        {
            RestResponse? response = null;
            try
            {
                var request = new RestRequest(url, Method.Post);

                if (_headers != null && _headers.Any())
                    request.AddHeaders(_headers);

                if (jsonData != null)
                    request.AddStringBody(jsonData, RestSharp.ContentType.Json);

                response = await _client.ExecuteAsync(request);

                if (response.IsSuccessful)
                    return (response, response.Content, null);
                else
                    return (response, null, null);
            }
            catch (Exception e)
            {
                return (response, null, e);
            }
        }

        public void SetTimeout(TimeSpan timeout)
        {
            _options.Timeout = timeout;
        }
    }
}
