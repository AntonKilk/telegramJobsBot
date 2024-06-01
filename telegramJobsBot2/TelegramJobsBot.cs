using jobs_api;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Telegram.Bot;

namespace telegramJobsBot2
{
    public class TelegramJobsBot
    {
        private readonly ILogger _logger;
        private TelegramBotClient _botClient;
        private string? _chatId;
        private string? _botToken;
        private string? _JobsApiUrl;

        public TelegramJobsBot(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<TelegramJobsBot>();
            _botToken = Environment.GetEnvironmentVariable("MyToken");
            _JobsApiUrl = Environment.GetEnvironmentVariable("JobsApiUrl");
            _chatId = Environment.GetEnvironmentVariable("chatId");

            if (string.IsNullOrWhiteSpace(_botToken) || string.IsNullOrWhiteSpace(_JobsApiUrl) || string.IsNullOrWhiteSpace(_chatId))
            {
                _logger.LogError("Missing environment variables.");
                throw new InvalidOperationException("Missing environment variables.");
            }

            _botClient = new TelegramBotClient(_botToken);
        }

        [Function("Function1")]
        public async Task Run([TimerTrigger("0 0 5 * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
            await FetchAndSendJobsAsync();
        }

        private async Task FetchAndSendJobsAsync()
        {
            using CancellationTokenSource cts = new();
            var jsonString = new FetchApi(_logger, _JobsApiUrl);
            var result = await jsonString.GetJobsAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var jobs = JsonSerializer.Deserialize<List<Job>>(result, options);

            if (jobs != null && jobs.Count > 0)
            {
                var message = $"Found {jobs.Count} jobs today.";
                await SendMessageAsync(message);

                int messageCount = 0;
                foreach (var job in jobs)
                {
                    var jobString = $"Link: {job.Link}, Level: {job.Lvl}, Title: {job.Title}";
                    await SendMessageAsync(jobString);
                    messageCount++;
                    // Delay 1.5 min  between messages after 19 messages to avoid telegram rate limit
                    if (messageCount % 19 == 0)
                    {
                        await Task.Delay(90000, cts.Token);
                    }
                }
            }
            else
            {
                await SendMessageAsync("No jobs found today.");
            }
            cts.Cancel();
        }

        private async Task SendMessageAsync(string messageText)
        {
            if (!string.IsNullOrWhiteSpace(messageText) && _chatId != null)
            {
                await _botClient.SendTextMessageAsync(
                    chatId: _chatId,
                    text: messageText,
                    disableNotification: true);
            }
        }
    }
}
