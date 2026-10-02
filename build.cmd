@echo off
setlocal
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" exit /b 1
"%CSC%" /nologo /target:winexe /win32icon:AgentDock.ico /out:AgentDock.exe /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\WPF\UIAutomationClient.dll" /reference:"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\WPF\WindowsBase.dll" /reference:"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\WPF\UIAutomationTypes.dll" /reference:System.Xml.dll AgentDock.cs
