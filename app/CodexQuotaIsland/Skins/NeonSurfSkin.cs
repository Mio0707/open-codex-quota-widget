using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using CodexQuotaIsland.Audio;

namespace CodexQuotaIsland.Skins;

public sealed class NeonSurfSkin : UserControl, IQuotaSkin, IDisposable
{
	private readonly WasapiLoopbackAnalyzer _analyzer = new();
	private readonly TextBlock _percentage;
	private readonly TextBlock _reset;

	public string Id => "neon-surf";
	public string DisplayName => "夜光冲浪";
	public double DesignWidth => 420;
	public double DesignHeight => 320;
	public FrameworkElement View => this;

	public NeonSurfSkin()
	{
		Width = DesignWidth;
		Height = DesignHeight;
		var card = new Border
		{
			Margin = new Thickness(8),
			CornerRadius = new CornerRadius(28),
			Background = new SolidColorBrush(Color.FromArgb(246, 3, 10, 24)),
			BorderBrush = new SolidColorBrush(Color.FromArgb(52, 88, 183, 255)),
			BorderThickness = new Thickness(1),
			ClipToBounds = true
		};
		var layout = new Grid();
		card.Child = layout;
		Content = card;
		layout.Children.Add(new NeonSurfVisual(_analyzer));
		layout.Children.Add(new TextBlock
		{
			Text = "SYSTEM AUDIO · 夜光冲浪",
			Margin = new Thickness(24, 20, 0, 0),
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Foreground = new SolidColorBrush(Color.FromArgb(148, 109, 188, 255)),
			FontFamily = new FontFamily("Segoe UI"),
			FontSize = 9,
			IsHitTestVisible = false
		});

		var quota = new Grid { Margin = new Thickness(24, 0, 24, 17), VerticalAlignment = VerticalAlignment.Bottom, IsHitTestVisible = false };
		quota.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		quota.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		var percentageRow = new StackPanel { Orientation = Orientation.Horizontal };
		_percentage = new TextBlock { Text = "--", Foreground = Brushes.White, FontFamily = new FontFamily("Segoe UI Variable Display"), FontSize = 25, FontWeight = FontWeights.SemiBold };
		percentageRow.Children.Add(_percentage);
		percentageRow.Children.Add(new TextBlock { Text = "%", Margin = new Thickness(2, 6, 0, 0), Foreground = new SolidColorBrush(Color.FromArgb(185, 119, 183, 244)), FontSize = 11 });
		quota.Children.Add(percentageRow);
		_reset = new TextBlock { Text = "正在读取额度…", HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(Color.FromArgb(168, 146, 168, 200)), FontFamily = new FontFamily("Microsoft YaHei UI"), FontSize = 9.5 };
		Grid.SetColumn(_reset, 1);
		quota.Children.Add(_reset);
		layout.Children.Add(quota);
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}

	public void UpdateQuota(QuotaViewData data)
	{
		_percentage.Text = data.HasData ? Math.Round(data.RemainingPercent).ToString("0") : "--";
		_reset.Text = data.HasData ? data.ResetText : "暂无额度记录";
	}

	private void OnLoaded(object sender, RoutedEventArgs e) => _analyzer.Start();
	private void OnUnloaded(object sender, RoutedEventArgs e) => _analyzer.Stop();
	public void Dispose() => _analyzer.Dispose();
}

internal sealed class NeonSurfVisual : FrameworkElement
{
	private sealed class Spray
	{
		public double X;
		public double Y;
		public double VX;
		public double VY;
		public double Life;
		public double MaxLife;
	}

	private readonly WasapiLoopbackAnalyzer _analyzer;
	private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(76) };
	private readonly Stopwatch _clock = Stopwatch.StartNew();
	private readonly Random _random = new();
	private readonly List<Spray> _spray = new();
	private double _lastTime;
	private double _time;
	private double _overall;
	private double _bass;
	private double _mid;
	private double _treble;
	private double _impact;

	public NeonSurfVisual(WasapiLoopbackAnalyzer analyzer)
	{
		_analyzer = analyzer;
		IsHitTestVisible = false;
		_timer.Tick += OnFrame;
		Loaded += (_, _) => _timer.Start();
		Unloaded += (_, _) => _timer.Stop();
	}

	private void OnFrame(object? sender, EventArgs e)
	{
		bool active = _analyzer.Overall > 0.015f || _analyzer.Bass > 0.02f || _analyzer.Treble > 0.025f;
		_timer.Interval = TimeSpan.FromMilliseconds(active ? 35 : 76);
		double now = _clock.Elapsed.TotalSeconds;
		double dt = _lastTime <= 0 ? 1.0 / 14 : Math.Min(0.12, now - _lastTime);
		_lastTime = now;
		_time += dt;
		_overall = Approach(_overall, _analyzer.Overall, dt, 6.5);
		_bass = Approach(_bass, _analyzer.Bass, dt, 9);
		_mid = Approach(_mid, _analyzer.Mid, dt, 6);
		_treble = Approach(_treble, _analyzer.Treble, dt, 9);
		if (_analyzer.ConsumeBeat()) _impact = Math.Min(1, _impact + 0.72);
		_impact *= Math.Exp(-dt * 3.8);

		UpdateSpray(dt);
		double sprayRate = _treble * 16 + _overall * 2;
		if (_spray.Count < 30 && _random.NextDouble() < sprayRate * dt) AddSpray();
		InvalidateVisual();
	}

	protected override void OnRender(DrawingContext dc)
	{
		base.OnRender(dc);
		double width = ActualWidth > 1 ? ActualWidth : 420;
		double height = ActualHeight > 1 ? ActualHeight : 320;
		double scale = Math.Min(width / 420, height / 320);
		dc.PushTransform(new TranslateTransform((width - 420 * scale) / 2, (height - 320 * scale) / 2));
		dc.PushTransform(new ScaleTransform(scale, scale));

		DrawMoon(dc);
		double amplitude = 18 + _overall * 66 + _bass * 28 + _impact * 20;
		double phase = _time * (24 + _overall * 68 + _mid * 35);
		double baseY = 209 - Math.Sin(_time * 0.34) * 4;
		StreamGeometry water = CreateWaterGeometry(baseY, amplitude, phase);
		var waterBrush = new LinearGradientBrush(Color.FromRgb(6, 41, 82), Color.FromRgb(52, 25, 105), new Point(0.5, 0), new Point(0.5, 1));
		dc.DrawGeometry(waterBrush, null, water);
		DrawWaveGlow(dc, CreateWaveLine(baseY, amplitude, phase), 0.62 + _overall * 0.38 + _treble * 0.18);

		double surferX = 210 + Math.Sin(_time * (0.36 + _mid * 0.48)) * (76 + _mid * 30) + Math.Sin(_time * 1.1) * 12;
		double surfaceY = WaveY(surferX, baseY, amplitude, phase);
		double slope = WaveY(surferX + 4, baseY, amplitude, phase) - WaveY(surferX - 4, baseY, amplitude, phase);
		DrawSurfer(dc, surferX, surfaceY - 8, slope * 2.8, _overall);
		DrawSpray(dc, phase, baseY, amplitude);

		dc.Pop();
		dc.Pop();
	}

	private void DrawMoon(DrawingContext dc)
	{
		var halo = new RadialGradientBrush(Color.FromArgb(44, 84, 194, 255), Color.FromArgb(0, 84, 194, 255)) { RadiusX = 0.5, RadiusY = 0.5 };
		dc.DrawEllipse(halo, null, new Point(330, 84), 72, 72);
		dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(100, 184, 226, 255)), null, new Point(330, 84), 12, 12);
	}

	private static StreamGeometry CreateWaterGeometry(double baseY, double amplitude, double phase)
	{
		var geometry = new StreamGeometry();
		using StreamGeometryContext c = geometry.Open();
		c.BeginFigure(new Point(0, 320), true, true);
		c.LineTo(new Point(0, WaveY(0, baseY, amplitude, phase)), true, false);
		var points = new List<Point>();
		for (int x = 10; x <= 420; x += 10) points.Add(new Point(x, WaveY(x, baseY, amplitude, phase)));
		c.PolyLineTo(points, true, false);
		c.LineTo(new Point(420, 320), true, false);
		geometry.Freeze();
		return geometry;
	}

	private static StreamGeometry CreateWaveLine(double baseY, double amplitude, double phase)
	{
		var geometry = new StreamGeometry();
		using StreamGeometryContext c = geometry.Open();
		c.BeginFigure(new Point(0, WaveY(0, baseY, amplitude, phase)), false, false);
		var points = new List<Point>();
		for (int x = 7; x <= 420; x += 7) points.Add(new Point(x, WaveY(x, baseY, amplitude, phase)));
		c.PolyLineTo(points, true, true);
		geometry.Freeze();
		return geometry;
	}

	private static double WaveY(double x, double baseY, double amplitude, double phase)
	{
		double primary = Math.Sin((x + phase) * 0.036);
		double secondary = Math.Sin((x + phase * 1.45) * 0.082) * 0.28;
		double crest = Math.Max(0, Math.Sin((x + phase) * 0.036 + 0.45));
		return baseY - amplitude * (primary * 0.52 + secondary + crest * 0.42);
	}

	private static void DrawWaveGlow(DrawingContext dc, Geometry wave, double intensity)
	{
		dc.PushOpacity(Math.Clamp(intensity, 0.35, 1));
		dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromArgb(34, 85, 183, 255)), 18), wave);
		dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromArgb(84, 80, 211, 255)), 8), wave);
		dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromArgb(245, 124, 231, 255)), 2.4), wave);
		dc.Pop();
	}

	private static void DrawSurfer(DrawingContext dc, double x, double y, double tilt, double intensity)
	{
		dc.PushTransform(new RotateTransform(Math.Clamp(tilt, -22, 22), x, y));
		var glow = new SolidColorBrush(Color.FromArgb((byte)(160 + intensity * 80), 156, 229, 255));
		var pen = new Pen(glow, 2.2) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
		dc.DrawLine(pen, new Point(x - 19, y + 8), new Point(x + 22, y + 8));
		dc.DrawEllipse(glow, null, new Point(x + 1, y - 27), 3.6, 3.6);
		dc.DrawLine(pen, new Point(x + 1, y - 22), new Point(x - 3, y - 5));
		dc.DrawLine(pen, new Point(x - 1, y - 17), new Point(x - 12, y - 11));
		dc.DrawLine(pen, new Point(x - 1, y - 17), new Point(x + 10, y - 12));
		dc.DrawLine(pen, new Point(x - 3, y - 5), new Point(x - 13, y + 5));
		dc.DrawLine(pen, new Point(x - 3, y - 5), new Point(x + 10, y + 6));
		dc.Pop();
	}

	private void AddSpray()
	{
		_spray.Add(new Spray
		{
			X = 40 + _random.NextDouble() * 340,
			Y = 145 + _random.NextDouble() * 85,
			VX = -13 + _random.NextDouble() * 26,
			VY = -12 - _random.NextDouble() * 36,
			MaxLife = 0.7 + _random.NextDouble() * 1.1,
			Life = 0.7 + _random.NextDouble() * 1.1
		});
	}

	private void UpdateSpray(double dt)
	{
		for (int i = _spray.Count - 1; i >= 0; i--)
		{
			Spray spray = _spray[i];
			spray.X += spray.VX * dt;
			spray.Y += spray.VY * dt;
			spray.VY += 34 * dt;
			spray.Life -= dt;
			if (spray.Life <= 0) _spray.RemoveAt(i);
		}
	}

	private void DrawSpray(DrawingContext dc, double phase, double baseY, double amplitude)
	{
		foreach (Spray spray in _spray)
		{
			double opacity = Math.Clamp(spray.Life / spray.MaxLife, 0, 1) * (0.3 + _treble * 0.7);
			dc.PushOpacity(opacity);
			dc.DrawEllipse(Brushes.White, null, new Point(spray.X, spray.Y), 1.2, 1.2);
			dc.Pop();
		}
	}

	private static double Approach(double current, double target, double dt, double speed) =>
		current + (target - current) * (1 - Math.Exp(-dt * speed));
}
