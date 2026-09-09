using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CodexQuotaIsland.Skins;

public partial class FishingSkin : UserControl, IQuotaSkin, IComponentConnector
{
	private int _currentStage = 100;

	public string Id => "fishing";

	public string DisplayName => "垂钓";

	public double DesignWidth => 190.0;

	public double DesignHeight => 410.0;

	public FrameworkElement View => this;

	public FishingSkin()
	{
		InitializeComponent();
	}

	public void UpdateQuota(QuotaViewData data)
	{
		PercentageText.Text = (data.HasData ? Math.Round(data.RemainingPercent).ToString("0") : "--");
		ResetText.Text = (data.HasData ? data.ResetText : "暂无额度记录");
		if (!data.HasData)
		{
			FishGroup.Visibility = Visibility.Hidden;
			return;
		}
		FishGroup.Visibility = Visibility.Visible;
		double num = Math.Clamp(data.RemainingPercent, 0.0, 100.0);
		Canvas.SetTop(FishGroup, 128.0 + 87.0 * num / 100.0);
		bool flag = num <= 0.5;
		FishRotate.Angle = (flag ? (-24) : 0);
		Color color = (flag ? Color.FromRgb(238, 205, 155) : ((num <= 25.0) ? Color.FromRgb(210, 196, 168) : Color.FromRgb(192, 198, 195)));
		HookPath.Stroke = new SolidColorBrush(color);
		HookTip.Fill = new SolidColorBrush(color);
		FishGroup.ToolTip = (flag ? "额度已用完 · 鱼已上钩" : $"{data.WindowLabel} · 距离上钩还有 {num:0.#}%");
		int num2 = ((!(num <= 0.5)) ? ((num <= 25.0) ? 25 : ((num <= 50.0) ? 50 : ((num <= 75.0) ? 75 : 100))) : 0);
		if (num2 != _currentStage)
		{
			_currentStage = num2;
			PlayMilestoneAnimation(num2);
		}
	}

	private void PlayMilestoneAnimation(int stage)
	{
		ClearMilestoneAnimations();
		switch (stage)
		{
		case 75:
			Animate(FishMilestoneMove, TranslateTransform.XProperty, new DoubleAnimation(28.0, 0.0, TimeSpan.FromSeconds(1.25))
			{
				EasingFunction = new BackEase
				{
					Amplitude = 0.35,
					EasingMode = EasingMode.EaseOut
				},
				FillBehavior = FillBehavior.Stop
			});
			PlayRipple(0.9);
			break;
		case 50:
			AnimateKeyFrames(FishMilestoneMove, TranslateTransform.XProperty, (0.0, 0.0), (0.28, -11.0), (0.62, 7.0), (1.0, 0.0));
			AnimateKeyFrames(FishMilestoneMove, TranslateTransform.YProperty, (0.0, 0.0), (0.28, -9.0), (0.62, 5.0), (1.0, 0.0));
			AnimateKeyFrames(FishMilestoneRotate, RotateTransform.AngleProperty, (0.0, 0.0), (0.28, -10.0), (0.62, 7.0), (1.0, 0.0));
			PlayRipple(1.35);
			break;
		case 25:
			Animate(FishMilestoneMove, TranslateTransform.XProperty, new DoubleAnimation(0.0, -10.0, TimeSpan.FromSeconds(0.24))
			{
				AutoReverse = true,
				RepeatBehavior = new RepeatBehavior(3.0),
				FillBehavior = FillBehavior.Stop
			});
			Animate(HookShake, RotateTransform.AngleProperty, new DoubleAnimation(-1.8, 1.8, TimeSpan.FromSeconds(0.13))
			{
				AutoReverse = true,
				RepeatBehavior = new RepeatBehavior(6.0),
				FillBehavior = FillBehavior.Stop
			});
			PlayRipple(1.7);
			break;
		case 0:
			Animate(HookShake, RotateTransform.AngleProperty, new DoubleAnimation(0.0, -3.0, TimeSpan.FromSeconds(0.18))
			{
				AutoReverse = true,
				RepeatBehavior = new RepeatBehavior(3.0),
				FillBehavior = FillBehavior.Stop
			});
			PlayRipple(2.0);
			break;
		}
	}

	private void PlayRipple(double scaleTo)
	{
		Animate(MilestoneRipple, UIElement.OpacityProperty, new DoubleAnimationUsingKeyFrames
		{
			KeyFrames =
			{
				(DoubleKeyFrame)new LinearDoubleKeyFrame(0.0, KeyTime.FromPercent(0.0)),
				(DoubleKeyFrame)new LinearDoubleKeyFrame(0.65, KeyTime.FromPercent(0.3)),
				(DoubleKeyFrame)new LinearDoubleKeyFrame(0.0, KeyTime.FromPercent(1.0))
			},
			Duration = TimeSpan.FromSeconds(1.2),
			FillBehavior = FillBehavior.Stop
		});
		Animate(RippleScale, ScaleTransform.ScaleXProperty, new DoubleAnimation(0.55, scaleTo, TimeSpan.FromSeconds(1.2))
		{
			FillBehavior = FillBehavior.Stop
		});
		Animate(RippleScale, ScaleTransform.ScaleYProperty, new DoubleAnimation(0.55, scaleTo, TimeSpan.FromSeconds(1.2))
		{
			FillBehavior = FillBehavior.Stop
		});
	}

	private static void Animate(Animatable target, DependencyProperty property, AnimationTimeline animation)
	{
		target.BeginAnimation(property, animation);
	}

	private static void Animate(UIElement target, DependencyProperty property, AnimationTimeline animation)
	{
		target.BeginAnimation(property, animation);
	}

	private static void AnimateKeyFrames(Animatable target, DependencyProperty property, params (double time, double value)[] frames)
	{
		DoubleAnimationUsingKeyFrames doubleAnimationUsingKeyFrames = new DoubleAnimationUsingKeyFrames
		{
			Duration = TimeSpan.FromSeconds(1.8),
			FillBehavior = FillBehavior.Stop
		};
		for (int i = 0; i < frames.Length; i++)
		{
			var (percent, value) = frames[i];
			doubleAnimationUsingKeyFrames.KeyFrames.Add(new SplineDoubleKeyFrame(value, KeyTime.FromPercent(percent), new KeySpline(0.25, 0.1, 0.25, 1.0)));
		}
		target.BeginAnimation(property, doubleAnimationUsingKeyFrames);
	}

	private void ClearMilestoneAnimations()
	{
		FishMilestoneMove.BeginAnimation(TranslateTransform.XProperty, null);
		FishMilestoneMove.BeginAnimation(TranslateTransform.YProperty, null);
		FishMilestoneRotate.BeginAnimation(RotateTransform.AngleProperty, null);
		HookShake.BeginAnimation(RotateTransform.AngleProperty, null);
		MilestoneRipple.BeginAnimation(UIElement.OpacityProperty, null);
	}
}
