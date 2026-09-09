using System;
using System.IO;
using System.Text.Json;

namespace CodexQuotaIsland;

public sealed class SkinSettings
{
	public string SkinId { get; set; } = "plankton-cat";

	public string ThemeId { get; set; } = "dark";

	private static string SettingsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CodexQuotaIsland", "settings.json");

	public static SkinSettings Load()
	{
		try
		{
			return JsonSerializer.Deserialize<SkinSettings>(File.ReadAllText(SettingsPath)) ?? new SkinSettings();
		}
		catch
		{
			return new SkinSettings();
		}
	}

	public void Save()
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
			File.WriteAllText(SettingsPath, JsonSerializer.Serialize(this));
		}
		catch
		{
		}
	}
}
