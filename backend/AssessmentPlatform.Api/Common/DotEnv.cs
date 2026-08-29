namespace AssessmentPlatform.Api.Common;

/// <summary>
/// Minimal .env loader for local development so the Neon branch-first flow "just works":
/// `neon env pull` / `neon checkout` write DATABASE_URL etc. into .env.local, and this reads them.
/// Parses KEY=VALUE lines (optionally quoted), searches upward from both the working directory and
/// the app base directory, and never overrides a variable already set in the real environment.
/// No-op when the files are absent.
/// </summary>
public static class DotEnv
{
    public static void Load(params string[] fileNames)
    {
        var roots = new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory };
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in roots)
        {
            var directory = root;
            for (var depth = 0; depth < 6 && directory is not null; depth++)
            {
                foreach (var name in fileNames)
                {
                    var path = Path.Combine(directory, name);
                    if (seen.Add(path) && File.Exists(path))
                        LoadFile(path);
                }
                directory = Directory.GetParent(directory)?.FullName;
            }
        }
    }

    private static void LoadFile(string path)
    {
        var applied = 0;
        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var separator = line.IndexOf('=');
            if (separator <= 0)
                continue;

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim().Trim('"', '\'');

            if (Environment.GetEnvironmentVariable(key) is null)
            {
                Environment.SetEnvironmentVariable(key, value);
                applied++;
            }
        }

        if (applied > 0)
            Console.Error.WriteLine($"[DotEnv] loaded {applied} variable(s) from {path}");
    }
}
