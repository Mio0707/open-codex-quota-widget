using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace CodexQuotaIsland;

public static class QuotaReader
{
	private const int MaxTailBytes = 2097152;

	public static QuotaSnapshot? ReadLatest()
	{
		string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex", "sessions");
		if (!Directory.Exists(path))
		{
			return null;
		}
		QuotaSnapshot quotaSnapshot = null;
		IEnumerable<string> enumerable;
		try
		{
			enumerable = Directory.EnumerateFiles(path, "*.jsonl", SearchOption.AllDirectories).OrderByDescending(File.GetLastWriteTimeUtc).Take(12)
				.ToArray();
		}
		catch
		{
			return null;
		}
		foreach (string item in enumerable)
		{
			QuotaSnapshot quotaSnapshot2 = ReadLatestFromFile(item);
			if ((object)quotaSnapshot2 != null && ((object)quotaSnapshot == null || quotaSnapshot2.Timestamp > quotaSnapshot.Timestamp))
			{
				quotaSnapshot = quotaSnapshot2;
			}
		}
		return quotaSnapshot;
	}

	private static QuotaSnapshot? ReadLatestFromFile(string file)
	{
		try
		{
			using FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
			int num = (int)Math.Min(fileStream.Length, 2097152L);
			fileStream.Seek(-num, SeekOrigin.End);
			byte[] array = new byte[num];
			fileStream.Read(array, 0, num);
			foreach (string item in Enumerable.Reverse(Encoding.UTF8.GetString(array).Split('\n', StringSplitOptions.RemoveEmptyEntries)))
			{
				if (item.Contains("\"rate_limits\"", StringComparison.Ordinal))
				{
					QuotaSnapshot quotaSnapshot = ParseLine(item.Trim());
					if ((object)quotaSnapshot != null)
					{
						return quotaSnapshot;
					}
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static QuotaSnapshot? ParseLine(string line)
	{
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(line);
			JsonElement rootElement = jsonDocument.RootElement;
			if (!rootElement.TryGetProperty("payload", out var value) || !value.TryGetProperty("rate_limits", out var value2) || value2.ValueKind == JsonValueKind.Null || !value2.TryGetProperty("primary", out var value3))
			{
				return null;
			}
			LimitWindow limitWindow = ParseWindow(value3);
			if ((object)limitWindow == null)
			{
				return null;
			}
			LimitWindow secondary = null;
			if (value2.TryGetProperty("secondary", out var value4) && value4.ValueKind == JsonValueKind.Object)
			{
				secondary = ParseWindow(value4);
			}
			JsonElement value5;
			string planType = (value2.TryGetProperty("plan_type", out value5) ? (value5.GetString() ?? "Codex") : "Codex");
			JsonElement value6;
			DateTimeOffset result;
			DateTimeOffset timestamp = ((rootElement.TryGetProperty("timestamp", out value6) && DateTimeOffset.TryParse(value6.GetString(), out result)) ? result : DateTimeOffset.MinValue);
			return new QuotaSnapshot(planType, limitWindow, secondary, timestamp);
		}
		catch
		{
			return null;
		}
	}

	private static LimitWindow? ParseWindow(JsonElement element)
	{
		if (!element.TryGetProperty("used_percent", out var value) || !element.TryGetProperty("window_minutes", out var value2) || !element.TryGetProperty("resets_at", out var value3))
		{
			return null;
		}
		return new LimitWindow(value.GetDouble(), value2.GetInt32(), value3.GetInt64());
	}
}
