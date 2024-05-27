using Microsoft.Extensions.Logging;

namespace jobs_api
{
    public class FetchApi
    {
        static readonly HttpClient _client = new HttpClient();
        private readonly string? _jobsApiUrl;
        private readonly ILogger _logger;
        public FetchApi(ILogger logger, string url)
        {
            _jobsApiUrl = url;
            _logger = logger;
        }

        public async Task<string> GetJobsAsync()
        {
            if (string.IsNullOrEmpty(_jobsApiUrl))
            {
                _logger.LogError("Jobs API URL is not configured.");
                throw new InvalidOperationException("Jobs API URL is not configured.");
            }

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(_jobsApiUrl),
            };

            try
            {
                _logger.LogInformation($"Attempting to fetch jobs from {_jobsApiUrl}");
                using var response = await _client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync();
                return jsonString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching jobs.");
                throw;
            }
        }
    }

}
