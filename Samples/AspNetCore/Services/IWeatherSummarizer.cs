namespace StrongInject.Samples.AspNetCore.Services
{
    public interface IWeatherSummarizer
    {
        string Summarize(int temperatureC);
    }
}