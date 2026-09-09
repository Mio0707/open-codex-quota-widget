using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace CodexQuotaIsland.Skins;

public partial class MinimalSkin : UserControl, IQuotaSkin, IComponentConnector
{
	public string Id => "minimal";

	public string DisplayName => "极简条";

	public double DesignWidth => 356.0;

	public double DesignHeight => 92.0;

	public FrameworkElement View => this;

	public MinimalSkin()
	{
		InitializeComponent();
	}

	public void UpdateQuota(QuotaViewData data)
	{
		PercentageText.Text = (data.HasData ? Math.Round(data.RemainingPercent).ToString("0") : "--");
		ResetText.Text = (data.HasData ? data.ResetText : "暂无额度记录");
		ProgressFill.Width = (data.HasData ? (166.0 * data.RemainingPercent / 100.0) : 0.0);
		ProgressFill.ToolTip = (data.HasData ? $"{data.WindowLabel} · 剩余 {data.RemainingPercent:0.#}%" : "尚未发现 Codex 限额记录");
	}
}
