# Open Codex Quota Widget

一个可见、置顶的 Windows 桌面浮窗，显示当前 Windows 用户本机 Codex 的可用额度与预计重置时间。它只读取本机的 Codex 会话记录，不需要 API 密钥，也不会上传额度数据。

## 安装

在仓库的 [Releases](../../releases/latest) 页面下载 `OpenCodexQuotaWidget-Setup-x64.exe`，双击安装即可。安装不需要管理员权限；完成后可以从桌面或开始菜单打开浮窗。

## 使用

- 浮窗显示在屏幕右上角，并每 5 秒刷新一次。
- 可以通过窗口标题栏移动浮窗；右上角的 `×` 可以关闭它。
- 若显示“暂无额度信息”，先在 Codex 中开始一次对话，等待几秒即可。

## 发布维护

推送到 `main` 会自动构建安装包供测试。创建形如 `v1.0.0` 的版本标签后，会自动把安装包发布到 Releases。

