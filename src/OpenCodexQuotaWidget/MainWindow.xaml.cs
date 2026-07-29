using System.Windows;
using System.Windows.Threading;

namespace OpenCodexQuotaWidget;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer refreshTimer = new() { Interval = TimeSpan.FromSeconds(5) };

    public MainWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            Left = SystemParameters.WorkArea.Right - Width - 22;
            Top = SystemParameters.WorkArea.Top + 22;
            Refresh();
            refreshTimer.Tick += (_, _) => Refresh();
            refreshTimer.Start();
        };
    }

    private void Refresh()
    {
        var snapshot = QuotaReader.ReadLatest();
        if (snapshot is null)
        {
            RemainingText.Text = "暂无额度信息";
            ResetText.Text = "请先在 Codex 中进行一次对话，然后稍候重试。";
        }
        else
        {
            RemainingText.Text = $"{snapshot.RemainingPercent:0}% 可用";
            ResetText.Text = $"预计重置：{snapshot.ResetsAt.LocalDateTime:yyyy-MM-dd HH:mm}";
        }

        UpdatedText.Text = $"已更新：{DateTime.Now:HH:mm:ss}（每 5 秒自动刷新）";
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

}

