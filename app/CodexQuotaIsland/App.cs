using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CodexQuotaIsland.Skins;
using CodexQuotaIsland.Audio;

namespace CodexQuotaIsland;

public partial class App : Application
{
	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);
		string surfPreview = e.Args.FirstOrDefault((string arg) => arg.StartsWith("--render-surf=", StringComparison.OrdinalIgnoreCase));
		if (surfPreview != null)
		{
			RenderSurfPreview(surfPreview.Split('=', 2)[1]);
			Shutdown(0);
			return;
		}
		string audioProbe = e.Args.FirstOrDefault((string arg) => arg.StartsWith("--probe-audio=", StringComparison.OrdinalIgnoreCase));
		if (audioProbe != null)
		{
			using WasapiLoopbackAnalyzer analyzer = new WasapiLoopbackAnalyzer();
			analyzer.Start();
			Thread.Sleep(1800);
			File.WriteAllText(audioProbe.Split('=', 2)[1], $"connected={analyzer.IsConnected};overall={analyzer.Overall:0.000};bass={analyzer.Bass:0.000};mid={analyzer.Mid:0.000};treble={analyzer.Treble:0.000}");
			Shutdown(analyzer.IsConnected ? 0 : 2);
			return;
		}
		string planktonPreview = e.Args.FirstOrDefault((string arg) => arg.StartsWith("--render-plankton=", StringComparison.OrdinalIgnoreCase));
		if (planktonPreview != null)
		{
			RenderPlanktonPreview(planktonPreview.Split('=', 2)[1]);
			Shutdown(0);
			return;
		}
		string text = e.Args.FirstOrDefault((string arg) => arg.StartsWith("--render-bouldering=", StringComparison.OrdinalIgnoreCase));
		if (text != null)
		{
			string text2 = e.Args.FirstOrDefault((string arg) => arg.StartsWith("--remaining=", StringComparison.OrdinalIgnoreCase));
			double result;
			double remaining = ((text2 != null && double.TryParse(text2.Split('=', 2)[1], out result)) ? Math.Clamp(result, 0.0, 100.0) : 100.0);
			RenderBoulderingPreview(text.Split('=', 2)[1], remaining);
			Shutdown(0);
			return;
		}
		try
		{
			Window window = (base.MainWindow = new MainWindow());
			((MainWindow)window).Show();
		}
		catch (Exception ex)
		{
			string text3 = Path.Combine(AppContext.BaseDirectory, "启动错误.txt");
			File.WriteAllText(text3, ex.ToString());
			MessageBox.Show("悬浮窗启动失败，错误详情已保存到：\n" + text3 + "\n\n" + ex.Message, "Codex 额度悬浮窗", MessageBoxButton.OK, MessageBoxImage.Hand);
			Shutdown(1);
		}
	}

	private static void RenderSurfPreview(string outputPath)
	{
		using NeonSurfSkin skin = new NeonSurfSkin();
		skin.UpdateQuota(new QuotaViewData(HasData: true, 68, 32, "每周额度", "8月2日 15:05 重置"));
		Size size = new Size(skin.DesignWidth, skin.DesignHeight);
		skin.Measure(size);
		skin.Arrange(new Rect(size));
		skin.UpdateLayout();
		RenderTargetBitmap bitmap = new RenderTargetBitmap((int)skin.DesignWidth, (int)skin.DesignHeight, 96, 96, PixelFormats.Pbgra32);
		bitmap.Render(skin);
		PngBitmapEncoder encoder = new PngBitmapEncoder();
		encoder.Frames.Add(BitmapFrame.Create(bitmap));
		using FileStream stream = File.Create(outputPath);
		encoder.Save(stream);
	}

	private static void RenderPlanktonPreview(string outputPath)
	{
		using PlanktonCatSkin skin = new PlanktonCatSkin();
		skin.UpdateQuota(new QuotaViewData(HasData: true, 68, 32, "每周额度", "8月2日 15:05 重置"));
		Size size = new Size(skin.DesignWidth, skin.DesignHeight);
		skin.Measure(size);
		skin.Arrange(new Rect(size));
		skin.UpdateLayout();
		RenderTargetBitmap bitmap = new RenderTargetBitmap((int)skin.DesignWidth, (int)skin.DesignHeight, 96, 96, PixelFormats.Pbgra32);
		bitmap.Render(skin);
		PngBitmapEncoder encoder = new PngBitmapEncoder();
		encoder.Frames.Add(BitmapFrame.Create(bitmap));
		using FileStream stream = File.Create(outputPath);
		encoder.Save(stream);
	}

	private static void RenderBoulderingPreview(string outputPath, double remaining)
	{
		BoulderingSkin boulderingSkin = new BoulderingSkin();
		boulderingSkin.UpdateQuota(new QuotaViewData(HasData: true, remaining, 100.0 - remaining, "每周额度", "8月2日 15:05 重置"));
		int stage = ((!(remaining <= 0.5)) ? ((remaining <= 25.0) ? 25 : ((remaining <= 50.0) ? 50 : ((!(remaining <= 75.0)) ? 100 : 75))) : 0);
		boulderingSkin.SnapToStage(stage);
		Size size = new Size(boulderingSkin.DesignWidth, boulderingSkin.DesignHeight);
		boulderingSkin.Measure(size);
		boulderingSkin.Arrange(new Rect(size));
		boulderingSkin.UpdateLayout();
		RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap((int)boulderingSkin.DesignWidth, (int)boulderingSkin.DesignHeight, 96.0, 96.0, PixelFormats.Pbgra32);
		renderTargetBitmap.Render(boulderingSkin);
		PngBitmapEncoder pngBitmapEncoder = new PngBitmapEncoder();
		pngBitmapEncoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));
		using FileStream stream = File.Create(outputPath);
		pngBitmapEncoder.Save(stream);
	}
}
