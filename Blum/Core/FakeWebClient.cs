using RestSharp;
using System.Net;

namespace Blum.Core
{
    public class FakeWebClient : IDisposable
    {
        private RestClient _client;
        private RestClientOptions _options;
        private DeviceProfile _currentProfile;
        private Dictionary<string, string> _auth = [];

        public FakeWebClient(DeviceProfile? deviceProfile = null, string? proxyUri = null)
        {
            InitializeRestClient(deviceProfile, proxyUri);
        }

        private void InitializeRestClient(DeviceProfile? deviceProfile, string? proxyUri)
        {
            _currentProfile = deviceProfile ?? DeviceProfiles.AndroidPocoX5Pro5G;

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
            _client.Dispose();
            _options = null;
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
            if (_auth.ContainsKey(name))
            {
                _auth[name] = value;
            }
            else
            {
                _auth.Add(name, value);
            }
        }

        public void SetDeviceProfile(DeviceProfile newProfile)
        {
            _currentProfile = newProfile;
            SetHeader("User-Agent", _currentProfile.UserAgent);
        }

        public async Task<string?> GetAsync(string url)
        {
            try
            {
                var request = new RestRequest(url, Method.Get);
                request.AddHeaders(_auth);
                var response = await _client.ExecuteAsync(request);
                return response.IsSuccessful ? response.Content : null;
            }
            catch (Exception e)
            {
                //Console.WriteLine($"Request error for {url}: {e.Message}");
                return null;
            }
        }

        public async Task<(RestResponse? RawResponse, string? ResponseContent)> PostAsync(string url, string? jsonData = null)
        {
            try
            {
                var request = new RestRequest(url, Method.Post);
                request.AddHeaders(_auth);

                if (jsonData != null)
                {
                    request.AddStringBody(jsonData, RestSharp.ContentType.Json);
                }

                var response = await _client.ExecuteAsync(request);
                var jsonString = response.IsSuccessful ? response.Content : null;
                return (response, jsonString);
            }
            catch (Exception e)
            {
                //Console.WriteLine($"Request error for {url}: {e.Message}");
                return (null, null);
            }
        }

        public async Task<string?> PutAsync(string url, string jsonData)
        {
            try
            {
                var request = new RestRequest(url, Method.Put);
                request.AddHeaders(_auth);

                request.AddStringBody(jsonData, RestSharp.ContentType.Json);
                var response = await _client.ExecuteAsync(request);
                return response.IsSuccessful ? response.Content : null;
            }
            catch (Exception e)
            {
                //Console.WriteLine($"Request error for {url}: {e.Message}");
                return null;
            }
        }

        public async Task<string?> PatchAsync(string url, string jsonData)
        {
            try
            {
                var request = new RestRequest(url, Method.Patch);
                request.AddHeaders(_auth);

                request.AddStringBody(jsonData, RestSharp.ContentType.Json);
                var response = await _client.ExecuteAsync(request);
                return response.IsSuccessful ? response.Content : null;
            }
            catch (Exception e)
            {
                //Console.WriteLine($"Request error for {url}: {e.Message}");
                return null;
            }
        }

        public async Task<string?> DeleteAsync(string url, string? jsonData = null)
        {
            try
            {
                var request = new RestRequest(url, Method.Delete);

                if (jsonData != null)
                {
                    request.AddStringBody(jsonData, RestSharp.ContentType.Json);
                }

                var response = await _client.ExecuteAsync(request);
                return response.IsSuccessful ? response.Content : null;
            }
            catch (Exception e)
            {
                //Console.WriteLine($"Request error for {url}: {e.Message}");
                return null;
            }
        }

        public void SetTimeout(TimeSpan timeout)
        {
            _options.Timeout = timeout;
        }
    }
}
