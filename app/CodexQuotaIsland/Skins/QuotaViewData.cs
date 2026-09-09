namespace CodexQuotaIsland.Skins;

public sealed record QuotaViewData(bool HasData, double RemainingPercent, double UsedPercent, string WindowLabel, string ResetText);
