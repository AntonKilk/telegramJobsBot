using System.Text.Json;

namespace jobs_api
{
    public class MyJsonParser
    {
        public string ParseResult(string json)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var jobs = JsonSerializer.Deserialize<List<Job>>(json, options);
            if (jobs == null)
            {
                return "No jobs found or JSON is not properly formatted.";
            }
            var result = new System.Text.StringBuilder();
            foreach (var job in jobs)
            {
                result.AppendLine($"Link: {job.Link}, Level: {job.Lvl}, Title: {job.Title}");
            }

            return result.ToString();
        }
    }
}
