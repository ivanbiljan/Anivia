using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Anivia.Options;

public interface IAuditableOptionsSnapshot<out T> : IOptionsMonitor<T> where T : class, new()
{
    void Update(Action<T> updateAction);
}

internal sealed class AuditableOptionsSnapshot<T> : IAuditableOptionsSnapshot<T> where T : class, new()
{
    private readonly IConfigurationSection _configurationSection;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IOptionsMonitor<T> _optionsSnapshot;

    public AuditableOptionsSnapshot(
        IOptionsMonitor<T> optionsSnapshot,
        IConfigurationSection configurationSection,
        IHostEnvironment hostEnvironment)
    {
        _optionsSnapshot = optionsSnapshot;
        _configurationSection = configurationSection;
        _hostEnvironment = hostEnvironment;
    }

    public T Get(string name) => _optionsSnapshot.Get(name);
    public IDisposable OnChange(Action<T, string> listener) => throw new NotImplementedException();

    public T CurrentValue => _optionsSnapshot.CurrentValue;

    public void Update(Action<T> updateAction)
    {
        var appSettingsFile = _hostEnvironment.IsDevelopment() ? "appsettings.Development.json" : "appsettings.json";
        var appSettings = JsonNode.Parse(File.ReadAllText(appSettingsFile))!;

        var optionsInstance = appSettings[_configurationSection.Key].Deserialize<T>()!;
        updateAction(optionsInstance);

        appSettings[_configurationSection.Key] = JsonSerializer.SerializeToNode(optionsInstance);

        File.WriteAllText(appSettingsFile, appSettings.ToJsonString());
    }
}