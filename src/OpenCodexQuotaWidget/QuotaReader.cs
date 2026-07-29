using System.Globalization;
using System.IO;
using System.Text.Json;

namespace OpenCodexQuotaWidget;

internal sealed record QuotaSnapshot(double RemainingPercent, DateTimeOffset ResetsAt);

internal static class QuotaReader
{
    public static QuotaSnapshot? ReadLatest()
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex", "sessions");
        if (!Directory.Exists(directory)) return null;

        try
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*.jsonl", SearchOption.AllDirectories).OrderByDescending(File.GetLastWriteTimeUtc).Take(12))
            foreach (var line in File.ReadLines(file).Reverse().Take(600))
            {
                var snapshot = TryRead(line);
                if (snapshot is not null) return snapshot;
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return null;
    }

    private static QuotaSnapshot? TryRead(string line)
    {
        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;
            if (!root.TryGetProperty("payload", out var payload) || !payload.TryGetProperty("rate_limits", out var limits) ||
                !limits.TryGetProperty("primary", out var primary) || primary.ValueKind == JsonValueKind.Null ||
                !primary.TryGetProperty("used_percent", out var usedValue) || !primary.TryGetProperty("resets_at", out var resetValue)) return null;
            if (!double.TryParse(usedValue.GetRawText(), NumberStyles.Float, CultureInfo.InvariantCulture, out var used) ||
                !long.TryParse(resetValue.GetRawText(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var resetUnix)) return null;
            return new QuotaSnapshot(Math.Clamp(100 - used, 0, 100), DateTimeOffset.FromUnixTimeSeconds(resetUnix));
        }
        catch (JsonException) { return null; }
        catch (ArgumentOutOfRangeException) { return null; }
    }
}
