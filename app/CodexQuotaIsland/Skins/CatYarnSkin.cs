using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CodexQuotaIsland.Skins;

public partial class CatYarnSkin : UserControl, IQuotaSkin, IComponentConnector
{
	private double _lastCatLeft = 14.0;

	private double _lastBallLeft = 6.0;

	private double _lastProgressWidth;

	private double _lastBallAngle;

	public string Id => "cat-yarn";

	public string DisplayName => "胜胜滚毛球";

	public double DesignWidth => 480.0;

	public double DesignHeight => 250.0;

	public FrameworkElement View => this;

	public CatYarnSkin()
	{
		InitializeComponent();
		CatImage.Width = 90.0;
		CatImage.Height = 60.0;
		Canvas.SetTop(CatImage, 52.0);
		YarnBallGroup.Width = 24.0;
		YarnBallGroup.Height = 24.0;
		Canvas.SetTop(YarnBallGroup, 88.0);
		if (YarnBallGroup.Children[0] is Viewbox viewbox)
		{
			viewbox.Width = 24.0;
			viewbox.Height = 24.0;
		}
		BallRotation.CenterX = 12.0;
		BallRotation.CenterY = 12.0;
	}

	public void UpdateQuota(QuotaViewData data)
	{
		PercentageText.Text = (data.HasData ? Math.Round(data.RemainingPercent).ToString("0") : "--");
		ResetText.Text = (data.HasData ? data.ResetText : "暂无额度记录");
		if (!data.HasData)
		{
			CatImage.Opacity = 0.45;
			YarnBallGroup.Opacity = 0.45;
			return;
		}
		CatImage.Opacity = 1.0;
		YarnBallGroup.Opacity = 1.0;
		double num = 1.0 - Math.Clamp(data.RemainingPercent, 0.0, 100.0) / 100.0;
		double num2 = 6.0 + 420.0 * num;
		double num3 = Math.Max(14.0, num2 - 96.0);
		double num4 = 1080.0 * num;
		double num5 = 4.0 + 420.0 * num;
		AnimateCanvasLeft(CatImage, _lastCatLeft, num3);
		AnimateCanvasLeft(YarnBallGroup, _lastBallLeft, num2);
		AnimateWidth(JourneyProgress, _lastProgressWidth, num5);
		AnimateAngle(_lastBallAngle, num4);
		MoveDust(num3);
		_lastCatLeft = num3;
		_lastBallLeft = num2;
		_lastProgressWidth = num5;
		_lastBallAngle = num4;
		YarnBallGroup.ToolTip = $"{data.WindowLabel} · 已前进 {num * 100.0:0.#}%";
	}

	private static void AnimateCanvasLeft(UIElement element, double from, double to)
	{
		Canvas.SetLeft(element, to);
		element.BeginAnimation(Canvas.LeftProperty, new DoubleAnimation(from, to, TimeSpan.FromSeconds(0.75))
		{
			EasingFunction = new CubicEase
			{
				EasingMode = EasingMode.EaseOut
			},
			FillBehavior = FillBehavior.Stop
		});
	}

	private void AnimateAngle(double from, double to)
	{
		BallRotation.Angle = to;
		BallRotation.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(from, to, TimeSpan.FromSeconds(0.75))
		{
			EasingFunction = new CubicEase
			{
				EasingMode = EasingMode.EaseOut
			},
			FillBehavior = FillBehavior.Stop
		});
	}

	private static void AnimateWidth(FrameworkElement element, double from, double to)
	{
		element.Width = to;
		element.BeginAnimation(FrameworkElement.WidthProperty, new DoubleAnimation(from, to, TimeSpan.FromSeconds(0.75))
		{
			EasingFunction = new CubicEase
			{
				EasingMode = EasingMode.EaseOut
			},
			FillBehavior = FillBehavior.Stop
		});
	}

	private void MoveDust(double catLeft)
	{
		Canvas.SetLeft(DustOne, catLeft + 12.0);
		Canvas.SetLeft(DustTwo, catLeft + 22.0);
	}
}
