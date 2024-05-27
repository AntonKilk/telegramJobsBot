namespace jobs_api
{
    public class Job : IJob
    {
        public string Link { get; set; } = string.Empty;
        public string Lvl { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }
}
