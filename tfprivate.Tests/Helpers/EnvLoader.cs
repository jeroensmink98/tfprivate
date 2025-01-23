namespace tfprivate.Tests.Helpers;

public static class EnvLoader
{
    public static void Load(string filePath = ".env")
    {
        if (!File.Exists(filePath))
            return;

        foreach (var line in File.ReadAllLines(filePath))
        {
            var parts = line.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) continue;

            var key = parts[0].Trim();
            var value = parts[1].Trim().Trim('"', '\'');
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}