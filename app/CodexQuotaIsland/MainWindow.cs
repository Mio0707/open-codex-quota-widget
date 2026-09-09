using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using CodexQuotaIsland.Skins;

namespace CodexQuotaIsland;

public partial class MainWindow : Window, IComponentConnector
{
	private readonly DispatcherTimer _refreshTimer = new DispatcherTimer
	{
		Interval = TimeSpan.FromSeconds(5.0)
	};

	private readonly Dictionary<string, IQuotaSkin> _skins;

	private readonly SkinSettings _settings;

	private IQuotaSkin _activeSkin;

	private QuotaViewData _latestData = new QuotaViewData(HasData: false, 0.0, 0.0, "", "暂无额度记录");

	private double _baseWidth = 176.0;

	private double _baseHeight = 386.0;

	public MainWindow()
	{
		InitializeComponent();
		_settings = SkinSettings.Load();
		ApplyTheme(_settings.ThemeId, save: false);
		_skins = new IQuotaSkin[7]
		{
			new IncenseSkin(),
			new FishingSkin(),
			new CatYarnSkin(),
			new PlanktonCatSkin(),
			new NeonSurfSkin(),
			new BoulderingSkin(),
			new MinimalSkin()
		}.ToDictionary((IQuotaSkin skin) => skin.Id);
		string text = Environment.GetCommandLineArgs().FirstOrDefault((string arg) => arg.StartsWith("--skin=", StringComparison.OrdinalIgnoreCase))?.Split('=', 2).LastOrDefault();
		_activeSkin = _skins.GetValueOrDefault(text ?? _settings.SkinId) ?? _skins["incense"];
		ApplySkin(_activeSkin, save: false, keepCenter: false);
		_refreshTimer.Tick += async delegate
		{
			await RefreshQuotaAsync();
		};
	}

	private async void Window_Loaded(object sender, RoutedEventArgs e)
	{
		PositionAtTopCenter();
		await RefreshQuotaAsync();
		_refreshTimer.Start();
	}

	private void PositionAtTopCenter()
	{
		Rect workArea = SystemParameters.WorkArea;
		base.Left = workArea.Left + (workArea.Width - base.Width) / 2.0;
		base.Top = workArea.Top + 18.0;
	}

	private async Task RefreshQuotaAsync()
	{
		RefreshButton.IsEnabled = false;
		QuotaSnapshot quotaSnapshot = await Task.Run((Func<QuotaSnapshot>)QuotaReader.ReadLatest);
		RefreshButton.IsEnabled = true;
		if ((object)quotaSnapshot == null)
		{
			_latestData = new QuotaViewData(HasData: false, 0.0, 0.0, "", "暂无额度记录");
		}
		else
		{
			LimitWindow limitWindow = quotaSnapshot.Secondary ?? quotaSnapshot.Primary;
			double remainingPercent = Math.Clamp(100.0 - limitWindow.UsedPercent, 0.0, 100.0);
			_latestData = new QuotaViewData(HasData: true, remainingPercent, limitWindow.UsedPercent, FormatWindow(limitWindow.WindowMinutes), FormatReset(limitWindow.ResetsAt));
		}
		_activeSkin.UpdateQuota(_latestData);
	}

	private void ApplySkin(IQuotaSkin skin, bool save, bool keepCenter)
	{
		double num = base.Left + base.Width / 2.0;
		double num2 = base.Top + base.Height / 2.0;
		double num3 = ((base.Width > 0.0 && base.Height > 0.0) ? Math.Min(base.Width / _baseWidth, base.Height / _baseHeight) : 1.0);
		_activeSkin = skin;
		_baseWidth = skin.DesignWidth;
		_baseHeight = skin.DesignHeight;
		DesignSurface.Width = _baseWidth;
		DesignSurface.Height = _baseHeight;
		SkinHost.Content = skin.View;
		base.Width = Math.Clamp(_baseWidth * num3, base.MinWidth, base.MaxWidth);
		base.Height = Math.Clamp(_baseHeight * num3, base.MinHeight, base.MaxHeight);
		if (keepCenter)
		{
			base.Left = num - base.Width / 2.0;
			base.Top = num2 - base.Height / 2.0;
		}
		skin.UpdateQuota(_latestData);
		foreach (MenuItem item in SkinMenu.Items.OfType<MenuItem>())
		{
			item.IsChecked = object.Equals(item.Tag?.ToString(), skin.Id);
		}
		if (save)
		{
			_settings.SkinId = skin.Id;
			_settings.Save();
		}
	}

	private void ApplyTheme(string themeId, bool save)
	{
		bool flag = string.Equals(themeId, "light", StringComparison.OrdinalIgnoreCase);
		ResourceDictionary resources = Application.Current.Resources;
		resources["WindowBackgroundBrush"] = Brush(flag ? "#FFF7F5F2" : "#F3121214");
		resources["WindowBorderBrush"] = Brush(flag ? "#24000000" : "#22FFFFFF");
		resources["PrimaryTextBrush"] = Brush(flag ? "#202026" : "#F3F0EA");
		resources["SecondaryTextBrush"] = Brush(flag ? "#6F6870" : "#8E8992");
		resources["MutedTextBrush"] = Brush(flag ? "#7E777E" : "#77727B");
		resources["TrackBackgroundBrush"] = Brush(flag ? "#DED8DE" : "#302B33");
		resources["ControlForegroundBrush"] = Brush(flag ? "#5F5962" : "#A9A4AD");
		resources["ControlHoverBrush"] = Brush(flag ? "#14000000" : "#16FFFFFF");
		foreach (MenuItem item in ThemeMenu.Items.OfType<MenuItem>())
		{
			item.IsChecked = object.Equals(item.Tag?.ToString(), flag ? "light" : "dark");
		}
		if (save)
		{
			_settings.ThemeId = (flag ? "light" : "dark");
			_settings.Save();
		}
	}

	private static SolidColorBrush Brush(string value)
	{
		return new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
	}

	private static string FormatWindow(int minutes)
	{
		if (minutes > 360)
		{
			if (minutes != 10080)
			{
				return $"{Math.Max(1, minutes / 1440)} 天额度";
			}
			return "每周额度";
		}
		return $"{Math.Max(1, minutes / 60)} 小时额度";
	}

	private static string FormatReset(long unixSeconds)
	{
		if (unixSeconds <= 0)
		{
			return "重置时间未知";
		}
		DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(unixSeconds).ToLocalTime();
		TimeSpan timeSpan = dateTimeOffset - DateTimeOffset.Now;
		if (timeSpan.TotalMinutes <= 0.0)
		{
			return "等待额度刷新";
		}
		if (timeSpan.TotalHours < 24.0)
		{
			return $"约 {Math.Max(1, (int)Math.Ceiling(timeSpan.TotalHours))} 小时后重置";
		}
		return $"{dateTimeOffset:M月d日 HH:mm} 重置";
	}

	private void ThemeButton_Click(object sender, RoutedEventArgs e)
	{
		ThemeMenu.PlacementTarget = ThemeButton;
		ThemeMenu.Placement = PlacementMode.Bottom;
		ThemeMenu.IsOpen = true;
	}

	private void ThemeMenuItem_Click(object sender, RoutedEventArgs e)
	{
		if (sender is MenuItem { Tag: string tag })
		{
			ApplyTheme(tag, save: true);
		}
	}

	private void SkinButton_Click(object sender, RoutedEventArgs e)
	{
		SkinMenu.PlacementTarget = SkinButton;
		SkinMenu.Placement = PlacementMode.Bottom;
		SkinMenu.IsOpen = true;
	}

	private void SkinMenuItem_Click(object sender, RoutedEventArgs e)
	{
		if (sender is MenuItem { Tag: string tag } && _skins.TryGetValue(tag, out var value))
		{
			ApplySkin(value, save: true, keepCenter: true);
		}
	}

	private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (e.ButtonState == MouseButtonState.Pressed)
		{
			DragMove();
		}
	}

	private void LeftResize_DragDelta(object sender, DragDeltaEventArgs e)
	{
		ResizeLeft(e.HorizontalChange);
	}

	private void RightResize_DragDelta(object sender, DragDeltaEventArgs e)
	{
		ResizeRight(e.HorizontalChange);
	}

	private void TopResize_DragDelta(object sender, DragDeltaEventArgs e)
	{
		ResizeTop(e.VerticalChange);
	}

	private void BottomResize_DragDelta(object sender, DragDeltaEventArgs e)
	{
		ResizeBottom(e.VerticalChange);
	}

	private void TopLeftResize_DragDelta(object sender, DragDeltaEventArgs e)
	{
		ResizeLeft(e.HorizontalChange);
		ResizeTop(e.VerticalChange);
	}

	private void TopRightResize_DragDelta(object sender, DragDeltaEventArgs e)
	{
		ResizeRight(e.HorizontalChange);
		ResizeTop(e.VerticalChange);
	}

	private void BottomLeftResize_DragDelta(object sender, DragDeltaEventArgs e)
	{
		ResizeLeft(e.HorizontalChange);
		ResizeBottom(e.VerticalChange);
	}

	private void BottomRightResize_DragDelta(object sender, DragDeltaEventArgs e)
	{
		ResizeRight(e.HorizontalChange);
		ResizeBottom(e.VerticalChange);
	}

	private void ResizeLeft(double change)
	{
		double width = base.Width;
		base.Width = Math.Clamp(base.Width - change, base.MinWidth, base.MaxWidth);
		base.Left += width - base.Width;
	}

	private void ResizeRight(double change)
	{
		base.Width = Math.Clamp(base.Width + change, base.MinWidth, base.MaxWidth);
	}

	private void ResizeTop(double change)
	{
		double height = base.Height;
		base.Height = Math.Clamp(base.Height - change, base.MinHeight, base.MaxHeight);
		base.Top += height - base.Height;
	}

	private void ResizeBottom(double change)
	{
		base.Height = Math.Clamp(base.Height + change, base.MinHeight, base.MaxHeight);
	}

	private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
	{
		if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.None)
		{
			double num = ((e.Delta > 0) ? 1.08 : 0.92);
			base.Width = Math.Clamp(base.Width * num, base.MinWidth, base.MaxWidth);
			base.Height = Math.Clamp(base.Height * num, base.MinHeight, base.MaxHeight);
			e.Handled = true;
		}
	}

	private async void RefreshButton_Click(object sender, RoutedEventArgs e)
	{
		await RefreshQuotaAsync();
	}

	private void CloseButton_Click(object sender, RoutedEventArgs e)
	{
		Close();
	}
}
