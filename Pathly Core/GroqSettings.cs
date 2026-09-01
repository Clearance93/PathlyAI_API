namespace Pathly_Core
{
    public class GroqSettings
    {
        public string? Model { get; set; }
        public string? BaseUrl { get; set; }
        public string? GroqApiKey { get; set; }
        public List<GroqKeySettings> FallbackKeys { get; set; } = new();
    }

    public class GroqKeySettings
    {
        public string? Name { get; set; }
        public string? GroqApiKey { get; set; }
        public string? BaseUrl { get; set; }
        public string? Model { get; set; }
    }
}
