public async Task<(RestResponse? RestResponse, string? ResponseContent, Exception? Exception)> TryOptionsAsync(string url)
{
    RestResponse? response = null;
    try
    {
        var request = new RestRequest(url, Method.Options);

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

public async Task<(RestResponse? RestResponse, string? ResponseContent, Exception? Exception)> TryPutAsync(string url, string jsonData)
{
    RestResponse? response = null;
    try
    {
        var request = new RestRequest(url, Method.Put);

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

public async Task<(RestResponse? RestResponse, string? ResponseContent, Exception? Exception)> TryPatchAsync(string url, string jsonData)
{
    RestResponse? response = null;
    try
    {
        var request = new RestRequest(url, Method.Patch);

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

public async Task<(RestResponse? RestResponse, string? ResponseContent, Exception? Exception)> TryDeleteAsync(string url, string? jsonData = null)
{
    RestResponse? response = null;
    try
    {
        var request = new RestRequest(url, Method.Delete);

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