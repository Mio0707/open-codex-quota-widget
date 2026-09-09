namespace CodexQuotaIsland;

public sealed record LimitWindow(double UsedPercent, int WindowMinutes, long ResetsAt);
