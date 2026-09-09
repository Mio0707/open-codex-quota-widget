using System.Windows;

namespace CodexQuotaIsland.Skins;

public interface IQuotaSkin
{
	string Id { get; }

	string DisplayName { get; }

	double DesignWidth { get; }

	double DesignHeight { get; }

	FrameworkElement View { get; }

	void UpdateQuota(QuotaViewData data);
}
