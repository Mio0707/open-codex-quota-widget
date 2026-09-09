using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace CodexQuotaIsland.Skins;

public partial class IncenseSkin : UserControl, IQuotaSkin, IComponentConnector
{
	public string Id => "incense";

	public string DisplayName => "燃香";

	public double DesignWidth => 176.0;

	public double DesignHeight => 386.0;

	public FrameworkElement View => this;

	public IncenseSkin()
	{
		InitializeComponent();
	}

	public void UpdateQuota(QuotaViewData data)
	{
		PercentageText.Text = (data.HasData ? Math.Round(data.RemainingPercent).ToString("0") : "--");
		ResetText.Text = (data.HasData ? data.ResetText : "暂无额度记录");
		SetIncenseLength(data.HasData ? data.RemainingPercent : 0.0);
		IncenseStick.ToolTip = (data.HasData ? $"{data.WindowLabel} · 已使用 {data.UsedPercent:0.#}%" : "尚未发现 Codex 限额记录");
	}

	private void SetIncenseLength(double remainingPercent)
	{
		double num = 135.0 * Math.Clamp(remainingPercent, 0.0, 100.0) / 100.0;
		double num2 = 214.0 - num;
		IncenseStick.Height = Math.Max(2.0, num - 8.0);
		Canvas.SetTop(IncenseStick, num2 + 8.0);
		Canvas.SetTop(AshCap, num2 - 1.0);
		Canvas.SetTop(EmberGlow, num2 + 6.0);
		Canvas.SetTop(EmberHalo, num2 - 7.0);
		Canvas.SetTop(SmokeGroup, num2 - 70.0);
		Canvas.SetTop(SparkOne, num2 + 2.0);
		Canvas.SetTop(SparkTwo, num2 + 4.0);
		Visibility visibility = ((!(remainingPercent > 0.0)) ? Visibility.Hidden : Visibility.Visible);
		EmberGlow.Visibility = visibility;
		EmberHalo.Visibility = visibility;
		AshCap.Visibility = visibility;
		SmokeGroup.Visibility = visibility;
		SparkOne.Visibility = visibility;
		SparkTwo.Visibility = visibility;
	}
}
