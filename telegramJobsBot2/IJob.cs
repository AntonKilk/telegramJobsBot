namespace jobs_api
{
    public interface IJob
    {
        string Link { get; set; }
        string Lvl { get; set; }
        string Title { get; set; }
    }
}