using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media.Animation;

namespace CodexQuotaIsland.Skins;

public partial class BoulderingSkin : UserControl, IQuotaSkin, IComponentConnector
{
	private int _currentStage = 100;

	public string Id => "bouldering";

	public string DisplayName => "抱石";

	public double DesignWidth => 240.0;

	public double DesignHeight => 420.0;

	public FrameworkElement View => this;

	public BoulderingSkin()
	{
		InitializeComponent();
	}

	public void UpdateQuota(QuotaViewData data)
	{
		PercentageText.Text = (data.HasData ? Math.Round(data.RemainingPercent).ToString("0") : "--");
		ResetText.Text = (data.HasData ? data.ResetText : "暂无额度记录");
		if (!data.HasData)
		{
			StageText.Text = "等待出发";
			return;
		}
		double num = Math.Clamp(data.RemainingPercent, 0.0, 100.0);
		int num2 = ((!(num <= 0.5)) ? ((num <= 25.0) ? 25 : ((num <= 50.0) ? 50 : ((!(num <= 75.0)) ? 100 : 75))) : 0);
		TextBlock stageText = StageText;
		stageText.Text = num2 switch
		{
			100 => "准备起步",
			75 => "稳稳上墙",
			50 => "通过难点",
			25 => "冲向终点",
			_ => "完成抱石！",
		};
		if (num2 == _currentStage)
		{
			return;
		}
		foreach (var item in Frames())
		{
			var (num3, _) = item;
			FadeTo(item.Frame, (num3 == num2) ? 1 : 0);
		}
		_currentStage = num2;
	}

	private IEnumerable<(int Stage, FrameworkElement Frame)> Frames()
	{
		yield return (Stage: 100, Frame: Frame100);
		yield return (Stage: 75, Frame: Frame75);
		yield return (Stage: 50, Frame: Frame50);
		yield return (Stage: 25, Frame: Frame25);
		yield return (Stage: 0, Frame: Frame0);
	}

	private static void FadeTo(UIElement frame, double opacity)
	{
		frame.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(opacity, TimeSpan.FromMilliseconds(520.0))
		{
			EasingFunction = new CubicEase
			{
				EasingMode = EasingMode.EaseInOut
			},
			FillBehavior = FillBehavior.HoldEnd
		});
	}

	internal void SnapToStage(int stage)
	{
		foreach (var (num, frameworkElement) in Frames())
		{
			frameworkElement.BeginAnimation(UIElement.OpacityProperty, null);
			frameworkElement.Opacity = ((num == stage) ? 1 : 0);
		}
		_currentStage = stage;
	}
}
