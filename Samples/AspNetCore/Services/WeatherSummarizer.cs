namespace StrongInject.Samples.AspNetCore.Services
{
    public class WeatherSummarizer : IWeatherSummarizer
    {
        public string Summarize(int temperatureC)
        {
            return temperatureC switch
            {
                < 0 => "Freezing",
                < 5 => "Bracing",
                < 10 => "Chilly",
                < 15 => "Cool",
                < 20 => "Mild",
                < 25 => "Warm",
                < 30 => "Balmy",
                < 35 => "Hot",
                < 40 => "Sweltering",
                _ => "Scorching",
            };
        }
    }
}
