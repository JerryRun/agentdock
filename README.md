# AgentDock

AgentDock 是一个 Windows 桌面工作台：参考 Windows Terminal 的标签页交互，在一个主窗口中横向挂载多个 Edge 或 Chrome 网页窗口，用于同时使用多个 AI Agent Chat。

## 功能

- Windows Terminal 风格的标签页、收藏下拉菜单和无系统标题栏布局
- 每个标签页支持多个横向浏览器窗格，窗格宽度自动均分
- 支持 Microsoft Edge 和 Google Chrome 的 `--app` 模式
- 浏览器网页保留自己的登录状态、免费额度和多模态能力，不使用 iframe 或 API
- 标签页右键菜单支持重命名、收藏、关闭窗格、关闭标签页和关闭其他标签
- 收藏保存标签名称、浏览器类型和启动网址，可恢复多窗格工作区
- 命令面板支持自定义快捷键和向当前标签页所有窗格批量发送消息
- 主窗口最大化时保留 Windows 任务栏；批量输入框不创建独立任务栏图标
- 主窗口关闭时会隐藏并关闭由 AgentDock 挂载的浏览器窗口

## 下载运行

从 GitHub Releases 下载 `AgentDesk-v0.1.0-win-x64.zip`，解压后运行 `AgentDesk.exe`。

程序默认最大化，但不会覆盖系统任务栏。点击顶部 `+` 新建标签页，点击下拉按钮可以打开收藏、创建横向窗格或进入命令面板。

新建窗格后，先选择 Edge 或 Chrome，再输入网址并按回车。浏览器启动后会自动隐藏地址栏和浏览器标签栏，并挂载到当前窗格。

## 快捷键

| 快捷键 | 操作 |
| --- | --- |
| `Ctrl+Shift+P` | 打开命令面板 |
| `Ctrl+Shift++` | 新增横向窗格，支持主键盘和数字小键盘加号 |
| `Ctrl+Shift+Enter` | 打开批量提问框，向当前标签页所有窗格发送消息 |
| `Ctrl+Shift+T` | 新建标签页 |

命令面板中可以点击快捷键区域录入新的组合键。批量发送会使用 Windows UI Automation 查找网页输入框和发送按钮；无法确认目标控件时会跳过该页面并提示，不会盲目点击网页中的其他控件。

## 从源码构建

### 环境要求

- Windows 10 或 Windows 11
- Microsoft .NET Framework 4.x 的 C# 编译器 `csc.exe`
- Microsoft Edge 或 Google Chrome

### 构建

在 Windows 命令提示符中进入仓库目录并运行：

```bat
build.cmd
```

构建输出为 `AgentDesk.exe`。脚本使用系统 .NET Framework 编译器和仓库中的 `AgentDesk.ico`，不需要 NuGet 或联网下载依赖。

也可以使用 PowerShell：

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\build.cmd
.\AgentDesk.exe
```

## 数据和隐私

AgentDock 只负责启动、挂载和排列本机浏览器窗口。网页内容、登录凭据和聊天记录由浏览器自己的用户配置文件管理，程序不会调用 AI 服务 API，也不会上传消息。

本地工作区数据保存在 `%LOCALAPPDATA%\AgentDesk`：

- `favorites.txt`：网址收藏
- `workspaces.xml`：标签页收藏和窗格布局
- `settings.txt`：快捷键设置

收藏保存的是启动网址，不包含网页内部导航后的地址、网页聊天内容或登录凭据。

## 已知限制

- 浏览器窗口挂载依赖 Win32 `SetParent`，属于非官方窗口组合方式，浏览器更新可能影响兼容性。
- 浏览器弹窗、下载窗口和开发者工具通常会在主窗口外单独打开。
- 不同 AI 网站的 DOM 和无障碍控件结构不同，批量发送功能无法保证适配所有网站。
- 程序需要在 Windows 桌面会话中运行；不支持 Windows Sandbox、服务进程或无交互会话。

## 项目结构

```text
AgentDesk.cs       主程序和 Win32/UI Automation 逻辑
AgentDesk.ico      应用多尺寸图标
AgentDesk-icon.png 图标预览图
build.cmd          Windows 本地构建脚本
docs/BUILD.md      构建、测试和发布说明
```

## 许可证

当前仓库尚未声明开源许可证。除非仓库后续加入许可证文件，否则请不要将代码作为已授权的开源软件再分发。
