namespace Services
{
    public class CacheInfo
    {
        public bool IsFromCache { get; set; }
        public double DurationMs { get; set; }
        public string Source => IsFromCache ? "Redis" : "Database";
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}