using System.Drawing;
using System.Windows.Forms;

namespace OpenCodexQuotaWidget;

internal sealed class MainWindow : Form
{
    private readonly Label remainingLabel = new() { AutoSize = true, Font = new Font("Segoe UI", 30, FontStyle.Bold), ForeColor = Color.White };
    private readonly Label resetLabel = new() { AutoSize = true, ForeColor = Color.FromArgb(210, 218, 235), MaximumSize = new Size(310, 0) };
    private readonly Label updatedLabel = new() { AutoSize = true, ForeColor = Color.FromArgb(145, 155, 180), Font = new Font("Segoe UI", 8) };
    private readonly System.Windows.Forms.Timer refreshTimer = new() { Interval = 5000 };

    public MainWindow()
    {
        Text = "Codex Quota Widget";
        ClientSize = new Size(340, 174);
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        TopMost = true;
        BackColor = Color.FromArgb(23, 27, 36);

        var titleLabel = new Label
        {
            AutoSize = true,
            Text = "Codex 可用额度",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(185, 199, 255)
        };
        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(18),
            BackColor = BackColor
        };
        layout.Controls.Add(titleLabel);
        layout.Controls.Add(remainingLabel);
        layout.Controls.Add(resetLabel);
        layout.Controls.Add(updatedLabel);
        Controls.Add(layout);

        Load += (_, _) =>
        {
            var area = Screen.PrimaryScreen!.WorkingArea;
            Location = new Point(area.Right - Width - 22, area.Top + 22);
            RefreshQuota();
            refreshTimer.Tick += (_, _) => RefreshQuota();
            refreshTimer.Start();
        };
    }

    private void RefreshQuota()
    {
        var snapshot = QuotaReader.ReadLatest();
        if (snapshot is null)
        {
            remainingLabel.Text = "暂无额度信息";
            resetLabel.Text = "请先在 Codex 中进行一次对话，然后稍候重试。";
        }
        else
        {
            remainingLabel.Text = $"{snapshot.RemainingPercent:0}% 可用";
            resetLabel.Text = $"预计重置：{snapshot.ResetsAt.LocalDateTime:yyyy-MM-dd HH:mm}";
        }

        updatedLabel.Text = $"已更新：{DateTime.Now:HH:mm:ss}（每 5 秒自动刷新）";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) refreshTimer.Dispose();
        base.Dispose(disposing);
    }
}
