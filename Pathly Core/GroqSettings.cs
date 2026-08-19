namespace Pathly_Core
{
    public class GroqSettings
    {
        public string? Model { get; set; }
        public string? BaseUrl { get; set; }
        public string? GroqApiKey { get; set; }
        public List<GroqKeySettings> FallbackKeys { get; set; } = new();

        public IEnumerable<string> GetAllKeys()
        {
            if (!string.IsNullOrWhiteSpace(GroqApiKey))
                yield return GroqApiKey!;

            foreach (var key in FallbackKeys)
            {
                if (!string.IsNullOrWhiteSpace(key.GroqApiKey))
                    yield return key.GroqApiKey!;
            }
        }
    }

    public class GroqKeySettings
    {
        public string? Name { get; set; }
        public string? GroqApiKey { get; set; }
        public string? BaseUrl { get; set; }
        public string? Model { get; set; }
    }
}
