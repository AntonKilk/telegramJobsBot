using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
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
        //private readonly IConfiguration _configuration = null!;
        private static readonly SecretClient secretClient = new SecretClient(
        new Uri("https://tgjobskeyvault.vault.azure.net/"), new DefaultAzureCredential());

        public TelegramJobsBot(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<TelegramJobsBot>();
        }

        public async Task InitializeAsync()
        {
            var chatId = await secretClient.GetSecretAsync("chatId");
            _chatId = chatId.Value.Value;
            var myToken = await secretClient.GetSecretAsync("MyToken");
            _botToken = myToken.Value.Value;
            _botClient = new TelegramBotClient(_botToken);
            var JobsApiUrl = await secretClient.GetSecretAsync("JobsApiUrl");
            _JobsApiUrl = JobsApiUrl.Value.Value;
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
            await InitializeAsync();
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
                    // Delay 1min  between messages after 18 messages to avoid telegram rate limit
                    if (messageCount % 18 == 0)
                    {
                        await Task.Delay(60000, cts.Token);
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
