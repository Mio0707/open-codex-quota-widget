using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using CodexQuotaIsland.Audio;

namespace CodexQuotaIsland.Skins;

public sealed class PlanktonCatSkin : UserControl, IQuotaSkin, IDisposable
{
	private readonly WasapiLoopbackAnalyzer _analyzer = new();
	private readonly TextBlock _percentageText;
	private readonly TextBlock _resetText;
	private readonly TextBlock _audioStateText;

	public string Id => "plankton-cat";
	public string DisplayName => "浮游猫";
	public double DesignWidth => 340;
	public double DesignHeight => 460;
	public FrameworkElement View => this;

	public PlanktonCatSkin()
	{
		Width = DesignWidth;
		Height = DesignHeight;
		var root = new Border
		{
			Margin = new Thickness(8),
			CornerRadius = new CornerRadius(28),
			Background = new SolidColorBrush(Color.FromArgb(243, 5, 7, 15)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(42, 116, 174, 255)),
			BorderThickness = new Thickness(1),
			ClipToBounds = true
		};
		var layout = new Grid();
		root.Child = layout;
		Content = root;
		layout.Children.Add(new PlanktonCatVisual(_analyzer));

		_audioStateText = new TextBlock
		{
			Text = "监听系统声音",
			Margin = new Thickness(24, 22, 0, 0),
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Foreground = new SolidColorBrush(Color.FromArgb(130, 132, 185, 255)),
			FontFamily = new FontFamily("Microsoft YaHei UI"),
			FontSize = 9,
			IsHitTestVisible = false
		};
		layout.Children.Add(_audioStateText);

		var quota = new Grid
		{
			Margin = new Thickness(24, 0, 24, 19),
			VerticalAlignment = VerticalAlignment.Bottom,
			IsHitTestVisible = false
		};
		quota.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		quota.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		quota.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		quota.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		var percentageRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
		_percentageText = new TextBlock
		{
			Text = "--",
			Foreground = new SolidColorBrush(Color.FromRgb(226, 242, 255)),
			FontFamily = new FontFamily("Segoe UI Variable Display"),
			FontSize = 46,
			FontWeight = FontWeights.SemiBold
		};
		percentageRow.Children.Add(_percentageText);
		percentageRow.Children.Add(new TextBlock
		{
			Text = "%",
			Margin = new Thickness(4, 20, 0, 0),
			Foreground = new SolidColorBrush(Color.FromArgb(180, 138, 170, 215)),
			FontSize = 16
		});
		Grid.SetRow(percentageRow, 1);
		Grid.SetColumnSpan(percentageRow, 2);
		quota.Children.Add(percentageRow);

		_resetText = new TextBlock
		{
			Text = "正在读取额度…",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Foreground = new SolidColorBrush(Color.FromArgb(165, 143, 159, 186)),
			FontFamily = new FontFamily("Microsoft YaHei UI"),
			FontSize = 10
		};
		Grid.SetColumnSpan(_resetText, 2);
		Grid.SetRow(_resetText, 0);
		quota.Children.Add(_resetText);
		layout.Children.Add(quota);

		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}

	public void UpdateQuota(QuotaViewData data)
	{
		_percentageText.Text = data.HasData ? Math.Round(data.RemainingPercent).ToString("0") : "--";
		_resetText.Text = data.HasData ? data.ResetText : "暂无额度记录";
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		_analyzer.Start();
		_audioStateText.Text = "监听系统声音";
	}

	private void OnUnloaded(object sender, RoutedEventArgs e) => _analyzer.Stop();

	public void Dispose()
	{
		Loaded -= OnLoaded;
		Unloaded -= OnUnloaded;
		_analyzer.Dispose();
	}
}

internal sealed class PlanktonCatVisual : FrameworkElement
{
	private static readonly StreamGeometry BodyGeometry = CreateBodyGeometry();
	private static readonly StreamGeometry EyesGeometry = CreateEyesGeometry();
	private static readonly Pen WideGlowPen = CreatePen(18, 20);
	private static readonly Pen MediumGlowPen = CreatePen(10, 42);
	private static readonly Pen NearGlowPen = CreatePen(5.5, 82);
	private static readonly Pen CoreGlowPen = CreatePen(2.9, 250);
	private static readonly SolidColorBrush BlueParticleBrush = CreateSolidBrush(Color.FromRgb(91, 174, 255));
	private static readonly SolidColorBrush VioletParticleBrush = CreateSolidBrush(Color.FromRgb(166, 112, 255));

	private sealed class Particle
	{
		public double X;
		public double Y;
		public double VX;
		public double VY;
		public double Radius;
		public double Life;
		public double MaxLife;
		public bool Violet;
	}

	private readonly WasapiLoopbackAnalyzer _analyzer;
	private readonly Stopwatch _clock = Stopwatch.StartNew();
	private readonly DispatcherTimer _frameTimer = new() { Interval = TimeSpan.FromMilliseconds(85) };
	private readonly Random _random = new();
	private readonly List<Particle> _particles = new();
	private double _lastTime;
	private double _time;
	private double _overall;
	private double _bass;
	private double _mid;
	private double _treble;
	private double _beatTarget;
	private double _beatVisual;
	private double _pulseOffset;
	private double _pulseVelocity;

	public PlanktonCatVisual(WasapiLoopbackAnalyzer analyzer)
	{
		_analyzer = analyzer;
		IsHitTestVisible = false;
		SnapsToDevicePixels = false;
		_frameTimer.Tick += OnFrame;
		Loaded += (_, _) => _frameTimer.Start();
		Unloaded += (_, _) => _frameTimer.Stop();
		for (int i = 0; i < 5; i++) AddParticle(true);
	}

	private void OnFrame(object? sender, EventArgs e)
	{
		bool audioActive = _analyzer.Overall > 0.015f || _analyzer.Bass > 0.02f || _analyzer.Treble > 0.02f;
		TimeSpan desiredInterval = TimeSpan.FromMilliseconds(audioActive ? 40 : 85);
		if (_frameTimer.Interval != desiredInterval) _frameTimer.Interval = desiredInterval;
		double now = _clock.Elapsed.TotalSeconds;
		double dt = _lastTime <= 0 ? 1.0 / 12.0 : Math.Min(0.12, now - _lastTime);
		_lastTime = now;
		_time += dt;
		_overall = Approach(_overall, _analyzer.Overall, dt, 7);
		_bass = Approach(_bass, _analyzer.Bass, dt, 10);
		_mid = Approach(_mid, _analyzer.Mid, dt, 6);
		_treble = Approach(_treble, _analyzer.Treble, dt, 8);
		if (_analyzer.ConsumeBeat())
		{
			_beatTarget = 1;
			_pulseVelocity = Math.Max(-125, _pulseVelocity - 74);
		}
		_beatTarget *= Math.Exp(-dt * 4.2);
		_beatVisual = Approach(_beatVisual, _beatTarget, dt, 13);
		_pulseVelocity += -_pulseOffset * 34 * dt;
		_pulseVelocity *= Math.Exp(-dt * 7.2);
		_pulseOffset += _pulseVelocity * dt;
		UpdateParticles(dt);
		double spawnRate = 0.08 + _treble * 10 + _overall * 2.5;
		if (_particles.Count < 16 && _random.NextDouble() < spawnRate * dt) AddParticle(false);
		InvalidateVisual();
	}

	protected override void OnRender(DrawingContext dc)
	{
		base.OnRender(dc);
		double width = ActualWidth > 1 ? ActualWidth : 340;
		double height = ActualHeight > 1 ? ActualHeight : 460;
		double scale = Math.Min(width / 340, height / 460);
		double offsetX = (width - 340 * scale) / 2;
		double offsetY = (height - 460 * scale) / 2;
		dc.PushTransform(new TranslateTransform(offsetX, offsetY));
		dc.PushTransform(new ScaleTransform(scale, scale));

		double swimX = Math.Sin(_time * 0.26) * (24 + _mid * 42) + Math.Sin(_time * 0.83) * (7 + _mid * 11);
		double swimY = Math.Sin(_time * 0.52) * (20 + _overall * 15) + _pulseOffset;
		double driftX = Math.Sin(_time * 0.75) * (1.4 + _mid * 2.5);
		double driftY = Math.Sin(_time * 1.16) * 2.6;
		double idleBreath = 0.5 + 0.5 * Math.Sin(_time * 1.32);
		double breath = 0.02 + idleBreath * 0.04 + _overall * 0.05;
		double sx = 1 + breath + _beatVisual * 0.095;
		double sy = 1 + breath * 0.62 - _beatVisual * 0.08;
		double creatureScale = 0.34 + _overall * 0.025;

		DrawParticles(dc, swimX, swimY);
		dc.PushTransform(new TranslateTransform(swimX, swimY));
		dc.PushTransform(new ScaleTransform(creatureScale, creatureScale, 170, 230));
		dc.PushTransform(new TranslateTransform(driftX, driftY));
		dc.PushTransform(new ScaleTransform(sx, sy, 170, 168));
		DrawGlow(dc, BodyGeometry, 0.72 + _overall * 0.32 + _treble * 0.18);
		DrawEyes(dc, 0.72 + _overall * 0.22);
		dc.Pop();
		dc.Pop();

		dc.Pop();
		dc.Pop();
		dc.Pop();
		dc.Pop();
	}

	private static StreamGeometry CreateBodyGeometry()
	{
		var geometry = new StreamGeometry();
		using StreamGeometryContext c = geometry.Open();
		c.BeginFigure(new Point(170, 272), false, true);
		c.BezierTo(new Point(160, 272), new Point(159, 253), new Point(147, 253), true, true);
		c.BezierTo(new Point(135, 253), new Point(135, 267), new Point(123, 267), true, true);
		c.BezierTo(new Point(111, 267), new Point(109, 249), new Point(96, 244), true, true);
		c.BezierTo(new Point(83, 239), new Point(74, 230), new Point(70, 218), true, true);
		c.BezierTo(new Point(59, 164), new Point(75, 130), new Point(94, 111), true, true);
		c.BezierTo(new Point(96, 99), new Point(91, 79), new Point(100, 68), true, true);
		c.BezierTo(new Point(109, 58), new Point(123, 74), new Point(136, 77), true, true);
		c.BezierTo(new Point(153, 81), new Point(187, 81), new Point(204, 77), true, true);
		c.BezierTo(new Point(217, 74), new Point(231, 58), new Point(240, 68), true, true);
		c.BezierTo(new Point(249, 79), new Point(244, 99), new Point(246, 111), true, true);
		c.BezierTo(new Point(265, 130), new Point(281, 164), new Point(276, 203), true, true);
		c.BezierTo(new Point(274, 232), new Point(261, 244), new Point(244, 249), true, true);
		c.BezierTo(new Point(229, 253), new Point(229, 268), new Point(215, 268), true, true);
		c.BezierTo(new Point(201, 268), new Point(202, 254), new Point(190, 254), true, true);
		c.BezierTo(new Point(178, 254), new Point(180, 272), new Point(170, 272), true, true);
		geometry.Freeze();
		return geometry;
	}

	private void DrawEyes(DrawingContext dc, double intensity)
	{
		DrawGlow(dc, EyesGeometry, intensity * 0.92);
	}

	private static StreamGeometry CreateEyesGeometry()
	{
		var eyes = new StreamGeometry();
		using (StreamGeometryContext c = eyes.Open())
		{
			c.BeginFigure(new Point(111, 157), false, false);
			c.BezierTo(new Point(118, 168), new Point(130, 168), new Point(137, 157), true, true);
			c.BeginFigure(new Point(203, 157), false, false);
			c.BezierTo(new Point(210, 168), new Point(222, 168), new Point(229, 157), true, true);
		}
		eyes.Freeze();
		return eyes;
	}

	private void DrawGlow(DrawingContext dc, Geometry geometry, double intensity)
	{
		dc.PushOpacity(Math.Clamp(intensity / 1.08, 0.45, 1));
		dc.DrawGeometry(null, WideGlowPen, geometry);
		dc.DrawGeometry(null, MediumGlowPen, geometry);
		dc.DrawGeometry(null, NearGlowPen, geometry);
		dc.DrawGeometry(null, CoreGlowPen, geometry);
		dc.Pop();
	}

	private static Pen CreatePen(double width, double alpha)
	{
		byte a = (byte)Math.Clamp(alpha, 0, 255);
		var brush = new LinearGradientBrush
		{
			StartPoint = new Point(0.5, 0),
			EndPoint = new Point(0.5, 1),
			GradientStops = new GradientStopCollection
			{
				new(Color.FromArgb(a, 84, 204, 255), 0),
				new(Color.FromArgb(a, 117, 163, 255), 0.58),
				new(Color.FromArgb(a, 187, 92, 255), 1)
			}
		};
		brush.Freeze();
		var pen = new Pen(brush, width) { LineJoin = PenLineJoin.Round, StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
		pen.Freeze();
		return pen;
	}

	private static SolidColorBrush CreateSolidBrush(Color color)
	{
		var brush = new SolidColorBrush(color);
		brush.Freeze();
		return brush;
	}

	private void AddParticle(bool ambient)
	{
		var particle = new Particle
		{
			X = 55 + _random.NextDouble() * 230,
			Y = 100 + _random.NextDouble() * 300,
			VX = -3 + _random.NextDouble() * 6,
			VY = -2 - _random.NextDouble() * (ambient ? 4 : 15),
			Radius = 1.5 + _random.NextDouble() * (ambient ? 2.4 : 4.2),
			MaxLife = ambient ? 10 + _random.NextDouble() * 10 : 2.2 + _random.NextDouble() * 3.5,
			Violet = _random.NextDouble() > 0.55
		};
		particle.Life = ambient ? _random.NextDouble() * particle.MaxLife : particle.MaxLife;
		_particles.Add(particle);
	}

	private void UpdateParticles(double dt)
	{
		for (int i = _particles.Count - 1; i >= 0; i--)
		{
			Particle p = _particles[i];
			p.X += p.VX * dt + Math.Sin(_time * 0.7 + i) * dt * 1.4;
			p.Y += p.VY * dt;
			p.Life -= dt;
			if (p.Life <= 0 || p.Y < 38) _particles.RemoveAt(i);
		}
		while (_particles.Count < 4) AddParticle(true);
	}

	private void DrawParticles(DrawingContext dc, double driftX, double driftY)
	{
		foreach (Particle p in _particles)
		{
			double fadeIn = Math.Min(1, (p.MaxLife - p.Life) * 1.8 + 0.25);
			double fadeOut = Math.Min(1, p.Life * 0.8);
			double opacity = Math.Clamp(fadeIn * fadeOut * (0.52 + _treble * 0.35), 0.08, 0.82);
			SolidColorBrush brush = p.Violet ? VioletParticleBrush : BlueParticleBrush;
			var center = new Point(p.X + driftX * 0.18, p.Y + driftY * 0.18);
			dc.PushOpacity(opacity * 0.18);
			dc.DrawEllipse(brush, null, center, p.Radius * 2.5, p.Radius * 2.5);
			dc.Pop();
			dc.PushOpacity(opacity);
			dc.DrawEllipse(brush, null, center, p.Radius, p.Radius);
			dc.Pop();
		}
	}

	private static double Approach(double current, double target, double dt, double speed) =>
		current + (target - current) * (1 - Math.Exp(-dt * speed));
}
