# 构建与发布

## 本地构建

在 Windows 上运行 `build.cmd`。脚本会查找以下编译器之一：

```text
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe
```

输出文件为仓库根目录的 `AgentDock.exe`。构建过程只依赖 .NET Framework 自带程序集：Windows Forms、Drawing、XML 和 UI Automation。

## 验证清单

发布前至少检查：

1. 程序启动后默认最大化，且不覆盖 Windows 任务栏。
2. 新建标签页和横向窗格后，浏览器可正常挂载并随窗格宽度变化。
3. 关闭当前窗格后，剩余窗格重新均分。
4. 标签页右键菜单的重命名、收藏和关闭操作正常。
5. 命令面板可打开、失去焦点自动关闭，并能保存快捷键。
6. 批量提问框不出现在任务栏，空消息时发送按钮禁用。
7. 至少用三个浏览器窗格验证批量输入、提交和主窗口焦点恢复。
8. 关闭主窗口后，AgentDock 挂载的浏览器窗口不会逐个闪回桌面。

## 发布包

发布包至少包含：

```text
AgentDock.exe
AgentDock.ico
README.md
```

发布包不包含用户的 `%LOCALAPPDATA%\AgentDock` 数据，也不包含浏览器配置文件、登录信息或聊天记录。

版本号采用 `vMAJOR.MINOR.PATCH`。`v0.1.1` 是首个统一使用 AgentDock 名称的发布版本；窗口挂载和批量发送仍受浏览器更新及网站结构影响。
