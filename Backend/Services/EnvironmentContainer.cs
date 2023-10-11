namespace PDPWebsite.Services;

public class EnvironmentContainer
{
    public Dictionary<string, string> Config { get; set; } = new();

    public EnvironmentContainer(string path = "")
    {
        if (path == "")
#if DEBUG
            path = System.Environment.CurrentDirectory + @"\.env";
#else
            path = Path.Combine(AppContext.BaseDirectory, ".env");
#endif
        Load(path);
#if DEBUG
        Load(path + ".dev");
#endif
    }

    public string Get(string key)
    {
        return Config[key];
    }

    public string Get(string key, string defaultValue)
    {
        return Config.TryGetValue(key, out var value) ? value : defaultValue;
    }

    public void Load(string path)
    {
        if (!File.Exists(path))
            return;
        var lines = File.ReadAllLines(path);
        foreach (var line in lines)
        {
            var split = line.Split('=');
            Config[split[0]] = split[1];
        }
    }
}
