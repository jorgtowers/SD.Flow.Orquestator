namespace SD.Flow.Orquestator.Core
{
    public static class PathHelper
    {
        public static string ResolveDynamicPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;

            var now = DateTime.Now;

            return path
                .Replace("YYYY", now.ToString("yyyy")) // 2026
                .Replace("MM", now.ToString("MM"))     // 01
                .Replace("DD", now.ToString("dd"));     // 24
        }
    }
}