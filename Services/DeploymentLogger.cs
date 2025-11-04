using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DeployHelper.Services;

public static class DeploymentLogger
{
    private static readonly string LogFile = Path.Combine(AppContext.BaseDirectory, "deployments.json");

    public static void Log(DeploymentRecord record)
    {
        List<DeploymentRecord> records = new();
        if (File.Exists(LogFile))
        {
            var existing = File.ReadAllText(LogFile);
            if (!string.IsNullOrWhiteSpace(existing))
                records = JsonSerializer.Deserialize<List<DeploymentRecord>>(existing) ?? new();
        }

        records.Add(record);

        var json = JsonSerializer.Serialize(records, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(LogFile, json);
    }
}
