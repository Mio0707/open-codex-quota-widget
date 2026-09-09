using System;

namespace CodexQuotaIsland;

public sealed record QuotaSnapshot(string PlanType, LimitWindow Primary, LimitWindow? Secondary, DateTimeOffset Timestamp);
