using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Automation;
using System.Xml.Serialization;

internal static class NativeMethods
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MinMaxInfo
    {
        public NativePoint Reserved;
        public NativePoint MaxSize;
        public NativePoint MaxPosition;
        public NativePoint MinTrackSize;
        public NativePoint MaxTrackSize;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct NativeRect { public int Left; public int Top; public int Right; public int Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    public struct GuiThreadInfo
    {
        public int Size; public int Flags; public IntPtr Active; public IntPtr Focus; public IntPtr Capture;
        public IntPtr MenuOwner; public IntPtr MoveSize; public IntPtr Caret; public NativeRect CaretBounds;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct KeyboardInput
    {
        public ushort Key; public ushort ScanCode; public uint Flags; public uint Time; public UIntPtr ExtraInfo;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct MouseInput
    {
        public int X; public int Y; public uint Data; public uint Flags; public uint Time; public UIntPtr ExtraInfo;
    }
    [StructLayout(LayoutKind.Explicit)]
    public struct InputPayload
    {
        [FieldOffset(0)] public KeyboardInput Keyboard;
        [FieldOffset(0)] public MouseInput Mouse;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct InputEvent { public uint Type; public InputPayload Payload; }
    [DllImport("user32.dll", SetLastError = true)] public static extern uint SendInput(uint count, InputEvent[] inputs, int size);
    public static bool SendControlShortcut(ushort key)
    {
        var inputs = new[] {
            new InputEvent { Type = 1, Payload = new InputPayload { Keyboard = new KeyboardInput { Key = 0x11 } } },
            new InputEvent { Type = 1, Payload = new InputPayload { Keyboard = new KeyboardInput { Key = key } } },
            new InputEvent { Type = 1, Payload = new InputPayload { Keyboard = new KeyboardInput { Key = key, Flags = 2 } } },
            new InputEvent { Type = 1, Payload = new InputPayload { Keyboard = new KeyboardInput { Key = 0x11, Flags = 2 } } }
        };
        return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(InputEvent))) == inputs.Length;
    }

    public const int GWL_STYLE = -16;
    public const long WS_CHILD = 0x40000000L;
    public const long WS_VISIBLE = 0x10000000L;
    public const long WS_CLIPSIBLINGS = 0x04000000L;
    public const long WS_CLIPCHILDREN = 0x02000000L;
    public const uint SWP_NOZORDER = 0x0004;
    public const uint SWP_SHOWWINDOW = 0x0040;
    public const int SW_HIDE = 0;
    public const int SW_SHOW = 5;
    public const uint WM_CLOSE = 0x0010;
    public delegate bool EnumWindowsProc(IntPtr handle, IntPtr state);
    public delegate bool EnumChildWindowsProc(IntPtr handle, IntPtr state);
    public delegate IntPtr KeyboardHookProc(int code, IntPtr message, IntPtr data);
    public delegate void WinEventProc(IntPtr hook, uint eventId, IntPtr window, int objectId, int childId, uint thread, uint time);
    [DllImport("user32.dll")] public static extern IntPtr SetWinEventHook(uint first, uint last, IntPtr module, WinEventProc callback, uint process, uint thread, uint flags);
    [DllImport("user32.dll")] public static extern bool UnhookWinEvent(IntPtr hook);
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
    public static void CloakWindow(IntPtr window, bool cloak) { var value = cloak ? 1 : 0; DwmSetWindowAttribute(window, 13, ref value, 4); }
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr handle, out NativeRect rectangle);
    [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr handle, out NativeRect rectangle);
    [DllImport("user32.dll")] public static extern bool ClientToScreen(IntPtr handle, ref NativePoint point);
    [DllImport("user32.dll")] public static extern bool GetGUIThreadInfo(uint thread, ref GuiThreadInfo info);
    [DllImport("user32.dll")] public static extern bool IsChild(IntPtr parent, IntPtr child);
    [DllImport("user32.dll")] public static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern int GetSystemMetricsForDpi(int index, uint dpi);
    public static int CaptionHeight(IntPtr window) { try { return GetSystemMetricsForDpi(4, GetDpiForWindow(window)); } catch (EntryPointNotFoundException) { return GetSystemMetrics(4); } }
    [DllImport("user32.dll", SetLastError = true)] public static extern IntPtr SetWindowsHookEx(int hook, KeyboardHookProc callback, IntPtr module, uint thread);
    [DllImport("user32.dll")] public static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] public static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
    [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr handle);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint flags, uint x, uint y, uint data, UIntPtr extra);
    [DllImport("user32.dll")] public static extern IntPtr SetFocus(IntPtr handle);
    [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr handle);
    [DllImport("user32.dll")] public static extern IntPtr GetFocus();
    [DllImport("user32.dll")] public static extern IntPtr SetActiveWindow(IntPtr handle);
    [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint attach, uint attachTo, bool attachState);
    [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(NativePoint point);
    [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr handle, uint flags);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr GetModuleHandle(string module);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc callback, IntPtr state);
    [DllImport("user32.dll")] public static extern bool EnumChildWindows(IntPtr parent, EnumChildWindowsProc callback, IntPtr state);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr handle);
    [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr handle);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassName(IntPtr handle, StringBuilder text, int length);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr handle, out uint processId);
    [DllImport("user32.dll")] public static extern IntPtr SetParent(IntPtr child, IntPtr parent);
    [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr handle, int x, int y, int width, int height, bool repaint);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr handle, int command);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)] private static extern IntPtr GetWindowLongPtr64(IntPtr handle, int index);
    [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)] private static extern IntPtr GetWindowLong32(IntPtr handle, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)] private static extern IntPtr SetWindowLongPtr64(IntPtr handle, int index, IntPtr value);
    [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)] private static extern IntPtr SetWindowLong32(IntPtr handle, int index, IntPtr value);
    public static IntPtr GetStyle(IntPtr handle) { return IntPtr.Size == 8 ? GetWindowLongPtr64(handle, GWL_STYLE) : GetWindowLong32(handle, GWL_STYLE); }
    public static void SetStyle(IntPtr handle, IntPtr style) { if (IntPtr.Size == 8) SetWindowLongPtr64(handle, GWL_STYLE, style); else SetWindowLong32(handle, GWL_STYLE, style); }
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr handle, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] public static extern bool ReleaseCapture();
    [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr SendMessage(IntPtr handle, uint message, IntPtr wParam, string lParam);
}

internal sealed class BrowserTile : Panel
{
    private readonly IntPtr browser;
    private readonly IntPtr originalStyle;
    private readonly uint processId;
    private readonly uint browserThreadId;
    private readonly bool chromiumFrame;
    private bool inputAttached;
    private IntPtr rendererWindow;
    private readonly Panel browserViewport = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty, BackColor = Color.Black };
    public readonly string BrowserKind;
    public readonly string Url;

    public BrowserTile(IntPtr browserHandle, string browserKind, string url)
    {
        browser = browserHandle; BrowserKind = browserKind; Url = url; originalStyle = NativeMethods.GetStyle(browserHandle); browserThreadId = NativeMethods.GetWindowThreadProcessId(browserHandle, out processId); Dock = DockStyle.None; Margin = new Padding(0); Padding = new Padding(0); BorderStyle = BorderStyle.None; BackColor = Color.Black;
        var windowClass = new StringBuilder(128); NativeMethods.GetClassName(browser, windowClass, windowClass.Capacity); chromiumFrame = windowClass.ToString() == "Chrome_WidgetWin_1";
        Controls.Add(browserViewport); browserViewport.Resize += delegate { ResizeBrowser(); };
        Resize += delegate { ResizeBrowser(); };
        PaddingChanged += delegate { ResizeBrowser(); };
        ParentChanged += delegate { if (Parent != null) AttachBrowser(); };
    }
    private void AttachBrowser()
    {
        NativeMethods.ShowWindow(browser, NativeMethods.SW_HIDE);
        var style = unchecked((uint)originalStyle.ToInt64()); style &= ~0x90CF0000u; style |= (uint)(NativeMethods.WS_CHILD | NativeMethods.WS_CLIPSIBLINGS | NativeMethods.WS_CLIPCHILDREN);
        NativeMethods.SetStyle(browser, new IntPtr((long)style)); NativeMethods.SetParent(browser, browserViewport.Handle); NativeMethods.SetWindowPos(browser, IntPtr.Zero, 0, 0, 0, 0, 0x0053); ResizeBrowser(); NativeMethods.ShowWindow(browser, NativeMethods.SW_SHOW); NativeMethods.CloakWindow(browser, false);
    }
    private void AttachInputQueue()
    {
        if (inputAttached || browserThreadId == 0) return;
        var currentThread = NativeMethods.GetCurrentThreadId();
        inputAttached = currentThread != browserThreadId && NativeMethods.AttachThreadInput(currentThread, browserThreadId, true);
    }
    private void DetachInputQueue()
    {
        if (!inputAttached) return;
        NativeMethods.AttachThreadInput(NativeMethods.GetCurrentThreadId(), browserThreadId, false); inputAttached = false;
    }
    private IntPtr RendererWindow
    {
        get
        {
            if (rendererWindow != IntPtr.Zero && NativeMethods.IsWindow(rendererWindow)) return rendererWindow;
            rendererWindow = IntPtr.Zero;
            NativeMethods.EnumChildWindows(browser, delegate(IntPtr child, IntPtr state)
            {
                var name = new StringBuilder(128); NativeMethods.GetClassName(child, name, name.Capacity);
                if (name.ToString() == "Chrome_RenderWidgetHostHWND") { rendererWindow = child; return false; }
                return true;
            }, IntPtr.Zero);
            return rendererWindow == IntPtr.Zero ? browser : rendererWindow;
        }
    }
    public void ActivateInputQueue()
    {
        var host = FindForm();
        if (host != null) NativeMethods.SetActiveWindow(host.Handle);
        AttachInputQueue();
        NativeMethods.SetFocus(RendererWindow);
    }
    public void ReleaseInputQueue() { DetachInputQueue(); }
    public bool BrowserAlive { get { uint currentProcess; return NativeMethods.IsWindow(browser) && NativeMethods.GetWindowThreadProcessId(browser, out currentProcess) != 0 && currentProcess == processId; } }
    private void ResizeBrowser()
    {
        if (!browserViewport.IsHandleCreated || !BrowserAlive) return;
        var viewport = browserViewport.ClientRectangle;
        var left = 0; var right = 0; var top = 0; var bottom = 0;
        NativeMethods.NativeRect windowBounds, clientBounds; var origin = new NativeMethods.NativePoint();
        if (chromiumFrame && NativeMethods.GetWindowRect(browser, out windowBounds) && NativeMethods.GetClientRect(browser, out clientBounds) && NativeMethods.ClientToScreen(browser, ref origin))
        {
            left = Math.Max(0, origin.X - windowBounds.Left); right = Math.Max(0, windowBounds.Right - origin.X - clientBounds.Right);
            bottom = Math.Max(0, windowBounds.Bottom - origin.Y - clientBounds.Bottom);
            top = Math.Max(0, origin.Y - windowBounds.Top) + NativeMethods.CaptionHeight(browser) + bottom;
        }
        NativeMethods.SetWindowPos(browser, IntPtr.Zero, viewport.Left - left, viewport.Top - top, Math.Max(1, viewport.Width + left + right), Math.Max(1, viewport.Height + top + bottom), 0x0050);
    }
    public void UpdateViewport() { ResizeBrowser(); }
    public bool OwnsFocus(IntPtr focus) { return BrowserAlive && (focus == browser || NativeMethods.IsChild(browser, focus)); }
    public void ForwardMouseMessage(IntPtr message, NativeMethods.NativePoint screenPoint)
    {
        if (!BrowserAlive) return;
        NativeMethods.NativeRect bounds;
        var target = RendererWindow;
        if (!NativeMethods.GetWindowRect(target, out bounds)) return;
        var x = screenPoint.X - bounds.Left; var y = screenPoint.Y - bounds.Top;
        var coordinates = new IntPtr((y << 16) | (x & 0xffff));
        var buttonDown = message.ToInt64() == 0x0201;
        if (buttonDown) ActivateInputQueue();
        NativeMethods.PostMessage(target, unchecked((uint)message.ToInt64()), buttonDown ? new IntPtr(1) : IntPtr.Zero, coordinates);
        if (buttonDown) ActivateInputQueue();
    }
    public void ForwardKeyboardMessage(IntPtr message, IntPtr data)
    {
        if (!BrowserAlive) return;
        var virtualKey = Marshal.ReadInt32(data); var scanCode = Marshal.ReadInt32(data, 4); var flags = Marshal.ReadInt32(data, 8);
        long lParam = 1L | ((long)scanCode << 16);
        if ((flags & 1) != 0) lParam |= 1L << 24;
        if ((flags & 0x80) != 0 || message.ToInt64() == 0x0101 || message.ToInt64() == 0x0105) lParam |= 1L << 30 | 1L << 31;
        NativeMethods.PostMessage(RendererWindow, unchecked((uint)message.ToInt64()), new IntPtr(virtualKey), new IntPtr(lParam));
    }
    public bool FocusChatInput()
    {
        if (!BrowserAlive) return false;
        try
        {
            var host = FindForm(); if (host != null) { NativeMethods.SetForegroundWindow(host.Handle); NativeMethods.SetActiveWindow(host.Handle); }
            var target = FindChatInput(AutomationElement.FromHandle(browser));
            if (target == null) return false;
            return FocusAutomationInput(target);
        }
        catch { return false; }
    }
    private static AutomationElement FindChatInput(AutomationElement root)
    {
        if (root == null) return null;
        var condition = new OrCondition(new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit), new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Document));
        var inputs = root.FindAll(TreeScope.Descendants, condition); var candidates = new List<AutomationElement>();
        for (var index = 0; index < inputs.Count; index++)
        {
            var input = inputs[index]; var state = input.Current;
            if (!state.IsEnabled || !state.IsKeyboardFocusable || state.IsOffscreen || state.IsPassword || state.BoundingRectangle.Width < 1 || state.BoundingRectangle.Height < 1) continue;
            object pattern;
            if (input.TryGetCurrentPattern(ValuePattern.Pattern, out pattern) && ((ValuePattern)pattern).Current.IsReadOnly) continue;
            candidates.Add(input);
        }
        var edits = candidates.Where(item => item.Current.ControlType == ControlType.Edit).ToList();
        var named = edits.Where(item => IsComposerName(item.Current.Name + " " + item.Current.AutomationId + " " + item.Current.ClassName)).ToList();
        var choices = named.Count > 0 ? named : (edits.Count > 0 ? edits : candidates);
        return choices.OrderByDescending(item => item.Current.BoundingRectangle.Width * item.Current.BoundingRectangle.Height).ThenByDescending(item => item.Current.BoundingRectangle.Bottom).FirstOrDefault();
    }
    private static bool IsSendButtonName(string name)
    {
        if (String.IsNullOrWhiteSpace(name)) return false;
        var value = name.ToLowerInvariant();
        return value.Contains("send") || value.Contains("submit") || value.Contains("prompt") || value.Contains("发送") || value.Contains("提交");
    }
    private static bool FocusAutomationInput(AutomationElement input)
    {
        if (input == null) return false;
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                input.SetFocus();
                if (input.Current.HasKeyboardFocus) return true;
            }
            catch { }
            Application.DoEvents(); System.Threading.Thread.Sleep(40);
        }
        return false;
    }
    private static bool ClickAutomationElement(AutomationElement element)
    {
        if (element == null) return false;
        var bounds = element.Current.BoundingRectangle;
        if (bounds.Width < 1 || bounds.Height < 1 || bounds.Left < -30000 || bounds.Top < -30000) return false;
        var x = (int)(bounds.Left + bounds.Width / 2.0); var y = (int)(bounds.Top + bounds.Height / 2.0);
        NativeMethods.SetCursorPos(x, y); NativeMethods.mouse_event(0x0002, 0, 0, 0, UIntPtr.Zero); NativeMethods.mouse_event(0x0004, 0, 0, 0, UIntPtr.Zero);
        Application.DoEvents(); System.Threading.Thread.Sleep(80); return true;
    }
    private static bool TrySetAutomationValue(AutomationElement input, string text)
    {
        object pattern;
        if (input == null || !input.TryGetCurrentPattern(ValuePattern.Pattern, out pattern)) return false;
        try
        {
            var value = (ValuePattern)pattern;
            if (value.Current.IsReadOnly) return false;
            value.SetValue(text);
            Application.DoEvents(); System.Threading.Thread.Sleep(100);
            return value.Current.Value == text;
        }
        catch { return false; }
    }
    private static bool InvokeAutomationElement(AutomationElement element)
    {
        object pattern;
        if (element == null || !element.TryGetCurrentPattern(InvokePattern.Pattern, out pattern)) return false;
        try { ((InvokePattern)pattern).Invoke(); return true; }
        catch { return false; }
    }
    private static AutomationElement FindSendButton(AutomationElement root, AutomationElement input)
    {
        if (root == null || input == null) return null;
        var condition = new OrCondition(new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button), new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.SplitButton));
        var buttons = root.FindAll(TreeScope.Descendants, condition); var candidates = new List<Tuple<AutomationElement, int>>(); var inputBounds = input.Current.BoundingRectangle;
        for (var index = 0; index < buttons.Count; index++)
        {
            var button = buttons[index]; var state = button.Current;
            if (!state.IsEnabled || state.IsOffscreen) continue;
            object invokePattern; if (!button.TryGetCurrentPattern(InvokePattern.Pattern, out invokePattern)) continue;
            var bounds = state.BoundingRectangle;
            if (bounds.Bottom < inputBounds.Top - 100 || bounds.Top > inputBounds.Bottom + 120 || bounds.Right < inputBounds.Right - 160 || bounds.Left > inputBounds.Right + 100) continue;
            var value = (state.Name + " " + state.AutomationId).ToLowerInvariant();
            var className = state.ClassName.ToLowerInvariant();
            if (value.Contains("attach") || value.Contains("upload") || value.Contains("voice") || value.Contains("record") || value.Contains("stop") || value.Contains("附件") || value.Contains("上传") || value.Contains("语音") || className.Contains("ds-button--disabled")) continue;
            var deepSeekSend = className.Contains("ds-button--primary") && className.Contains("ds-button--filled") && className.Contains("ds-button--circle");
            if (!IsSendButtonName(value) && !deepSeekSend) continue;
            var priority = IsSendButtonName(value) ? 0 : 1;
            candidates.Add(Tuple.Create(button, priority));
        }
        return candidates.OrderBy(item => item.Item2).ThenBy(item => Distance(inputBounds, item.Item1.Current.BoundingRectangle)).Select(item => item.Item1).FirstOrDefault();
    }
    private static double Distance(System.Windows.Rect first, System.Windows.Rect second)
    {
        var firstX = first.Left + first.Width / 2.0; var firstY = first.Top + first.Height / 2.0;
        var secondX = second.Left + second.Width / 2.0; var secondY = second.Top + second.Height / 2.0;
        var x = firstX - secondX; var y = firstY - secondY; return x * x + y * y;
    }
    public bool SendChatMessage(string text)
    {
        if (!BrowserAlive) return false;
        try
        {
            var host = FindForm(); if (host != null) { NativeMethods.SetForegroundWindow(host.Handle); NativeMethods.SetActiveWindow(host.Handle); }
            ReleaseInputQueue();
            var root = AutomationElement.FromHandle(browser); var input = FindChatInput(root); if (input == null) return false;
            if (!TrySetAutomationValue(input, text))
            {
                ActivateInputQueue();
                if (!ClickAutomationElement(input)) return false;
                Clipboard.SetText(text); if (!NativeMethods.SendControlShortcut(0x41)) return false;
                Application.DoEvents(); System.Threading.Thread.Sleep(60);
                if (!NativeMethods.SendControlShortcut(0x56)) return false;
                Application.DoEvents(); System.Threading.Thread.Sleep(120);
                ReleaseInputQueue();
            }
            AutomationElement button = null;
            for (var attempt = 0; attempt < 10 && button == null; attempt++)
            {
                Application.DoEvents();
                var currentRoot = AutomationElement.FromHandle(browser); var currentInput = FindChatInput(currentRoot); if (currentInput != null) { root = currentRoot; input = currentInput; }
                button = FindSendButton(root, input);
                if (button == null) System.Threading.Thread.Sleep(50);
            }
            if (button == null) return false;
            return InvokeAutomationElement(button) || ClickAutomationElement(button);
        }
        catch { return false; }
        finally { ReleaseInputQueue(); }
    }
    public Task<bool> FocusChatInputAsync() { return Task.FromResult(FocusChatInput()); }
    private static bool IsComposerName(string name) { return new[] { "message", "prompt", "chat input", "ask", "deepseek", "chatgpt", "textarea", "消息", "提问", "输入", "询问" }.Any(value => name.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0); }
    public bool HasInputFocus()
    {
        var info = new NativeMethods.GuiThreadInfo { Size = Marshal.SizeOf(typeof(NativeMethods.GuiThreadInfo)) };
        return NativeMethods.GetGUIThreadInfo(0, ref info) && OwnsFocus(info.Focus);
    }
    public void HideBrowser() { if (!BrowserAlive) return; NativeMethods.CloakWindow(browser, true); NativeMethods.ShowWindow(browser, NativeMethods.SW_HIDE); }
    public void CloseBrowser()
    {
        if (!BrowserAlive) return;
        HideBrowser(); DetachInputQueue(); NativeMethods.SetParent(browser, IntPtr.Zero);
        NativeMethods.SetStyle(browser, new IntPtr(originalStyle.ToInt64() & ~NativeMethods.WS_VISIBLE));
        NativeMethods.SetWindowPos(browser, IntPtr.Zero, -32000, -32000, 800, 600, 0x0034);
        HideBrowser(); NativeMethods.PostMessage(browser, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
    }
    protected override void Dispose(bool disposing) { if (disposing) DetachInputQueue(); base.Dispose(disposing); }
}

internal sealed class Workspace : Panel
{
    public string FavoriteId;
    private Control activePane;
    public Control ActivePane
    {
        get { return activePane; }
        set
        {
            if (activePane == value) return;
            var previousBrowser = activePane as BrowserTile;
            if (previousBrowser != null) previousBrowser.ReleaseInputQueue();
            activePane = value;
            foreach (var pane in Panes) pane.Invalidate();
        }
    }
    public readonly Panel Canvas = new Panel();
    public readonly List<BrowserTile> Tiles = new List<BrowserTile>();
    public readonly List<BrowserLauncherTile> Launchers = new List<BrowserLauncherTile>();
    public readonly List<Control> Panes = new List<Control>();
    public Workspace(string name) { Text = name; Dock = DockStyle.Fill; Margin = Padding.Empty; Canvas.Dock = DockStyle.Fill; Canvas.BackColor = Color.FromArgb(12, 12, 12); Canvas.Resize += delegate { LayoutPanes(); }; Controls.Add(Canvas); }
    public void AddPane(Control pane) { Panes.Add(pane); TrackPane(pane); Canvas.Controls.Add(pane); LayoutPanes(); ActivePane = pane; }
    public void RemovePane(Control pane) { Panes.Remove(pane); if (ActivePane == pane) ActivePane = Panes.LastOrDefault(); Canvas.Controls.Remove(pane); LayoutPanes(); }
    public void LayoutPanes()
    {
        if (Panes.Count == 0) return;
        for (var index = 0; index < Panes.Count; index++)
        {
            var left = (int)((long)Canvas.ClientSize.Width * index / Panes.Count);
            var right = (int)((long)Canvas.ClientSize.Width * (index + 1) / Panes.Count);
            Panes[index].Padding = new Padding(index == 0 ? 0 : 1, 0, index < Panes.Count - 1 ? 1 : 0, 0);
            Panes[index].BackColor = Color.FromArgb(90, 90, 90);
            Panes[index].SetBounds(left, 0, right - left, Canvas.ClientSize.Height);
            var browserPane = Panes[index] as BrowserTile;
            if (browserPane != null) browserPane.UpdateViewport();
        }
        Canvas.Invalidate(true);
    }
    private void TrackPane(Control pane)
    {
        pane.Enter += delegate { ActivePane = pane; };
        pane.Paint += delegate(object sender, PaintEventArgs args) { DrawPaneBorder(pane, args.Graphics); };
        TrackPaneClicks(pane, pane);
    }
    private void TrackPaneClicks(Control control, Control pane)
    {
        control.MouseDown += delegate
        {
            ActivePane = pane;
            var launcher = pane as BrowserLauncherTile;
            if (launcher != null && !launcher.ContainsFocus) launcher.FocusBrowser();
        };
        foreach (Control child in control.Controls) TrackPaneClicks(child, pane);
    }
    private void DrawPaneBorder(Control pane, Graphics graphics)
    {
        var index = Panes.IndexOf(pane); if (index < 0 || pane.Width < 1 || pane.Height < 1) return;
        var selectedColor = Color.FromArgb(76, 194, 255); var inactiveColor = Color.FromArgb(184, 191, 203);
        using (var border = new Pen(pane == ActivePane ? selectedColor : inactiveColor))
        {
            if (pane.Top > 0) graphics.DrawLine(border, 0, 0, pane.Width - 1, 0);
            if (pane.Bottom < Canvas.ClientSize.Height) graphics.DrawLine(border, 0, pane.Height - 1, pane.Width - 1, pane.Height - 1);
            if (pane.Left > 0)
            {
                var previousSelected = index > 0 && Panes[index - 1] == ActivePane;
                using (var edge = new Pen(pane == ActivePane || previousSelected ? selectedColor : inactiveColor)) graphics.DrawLine(edge, 0, 0, 0, pane.Height - 1);
            }
        }
        var nextSelected = index + 1 < Panes.Count && Panes[index + 1] == ActivePane;
        if (pane.Right < Canvas.ClientSize.Width)
            using (var edge = new Pen(pane == ActivePane || nextSelected ? selectedColor : inactiveColor)) graphics.DrawLine(edge, pane.Width - 1, 0, pane.Width - 1, pane.Height - 1);
    }
    public void ReplacePane(Control previous, Control next) { var index = Panes.IndexOf(previous); if (index < 0) return; var wasActive = ActivePane == previous; next.Bounds = previous.Bounds; next.Padding = previous.Padding; next.BackColor = previous.BackColor; Panes[index] = next; TrackPane(next); Canvas.Controls.Remove(previous); Canvas.Controls.Add(next); LayoutPanes(); if (wasActive) ActivePane = next; }
}

internal sealed class RoundedInput : Panel
{
    private readonly Control input;
    public RoundedInput(Control control)
    {
        input = control; BackColor = Color.FromArgb(38, 38, 38); Margin = new Padding(0, 0, 0, 6); Dock = DockStyle.Fill;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        input.BackColor = BackColor; input.ForeColor = Color.White; Controls.Add(input);
        input.GotFocus += delegate { Invalidate(); }; input.LostFocus += delegate { Invalidate(); };
        MouseDown += delegate { input.Focus(); };
    }
    protected override void OnLayout(LayoutEventArgs args)
    {
        base.OnLayout(args);
        if (input != null) input.SetBounds(10, Math.Max(2, (ClientSize.Height - input.PreferredSize.Height) / 2), Math.Max(1, ClientSize.Width - 20), input.PreferredSize.Height);
    }
    protected override void OnPaint(PaintEventArgs args)
    {
        base.OnPaint(args);
        if (Width < 12 || Height < 12) return;
        args.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using (var outline = new GraphicsPath())
        using (var fill = new SolidBrush(BackColor))
        using (var border = new Pen(input.ContainsFocus ? Color.FromArgb(140, 190, 220) : Color.FromArgb(82, 82, 82)))
        {
            outline.AddArc(0, 0, 12, 12, 180, 90); outline.AddArc(Width - 13, 0, 12, 12, 270, 90);
            outline.AddArc(Width - 13, Height - 13, 12, 12, 0, 90); outline.AddArc(0, Height - 13, 12, 12, 90, 90); outline.CloseFigure();
            args.Graphics.Clear(Parent == null ? Color.FromArgb(12, 12, 12) : Parent.BackColor);
            args.Graphics.FillPath(fill, outline); args.Graphics.DrawPath(border, outline);
        }
    }
}

internal sealed class RoundedEditor : Panel
{
    private readonly Control input;
    public int Radius = 16;
    public RoundedEditor(Control control)
    {
        input = control; BackColor = Color.FromArgb(28, 28, 28); Margin = Padding.Empty;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        input.BackColor = BackColor; input.ForeColor = Color.White; Controls.Add(input);
        input.GotFocus += delegate { Invalidate(); }; input.LostFocus += delegate { Invalidate(); };
        MouseDown += delegate { input.Focus(); };
    }
    protected override void OnLayout(LayoutEventArgs args)
    {
        base.OnLayout(args);
        if (input != null) input.SetBounds(12, 10, Math.Max(1, ClientSize.Width - 24), Math.Max(1, ClientSize.Height - 20));
    }
    protected override void OnPaint(PaintEventArgs args)
    {
        base.OnPaint(args); if (Width < 20 || Height < 20) return;
        args.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using (var outline = new GraphicsPath())
        using (var fill = new SolidBrush(BackColor))
        using (var border = new Pen(input.ContainsFocus ? Color.FromArgb(76, 194, 255) : Color.FromArgb(78, 78, 78), input.ContainsFocus ? 2 : 1))
        {
            var radius = Math.Min(Radius, Math.Min(Width, Height) / 2); var diameter = radius * 2;
            outline.AddArc(0, 0, diameter, diameter, 180, 90); outline.AddArc(Width - diameter - 1, 0, diameter, diameter, 270, 90);
            outline.AddArc(Width - diameter - 1, Height - diameter - 1, diameter, diameter, 0, 90); outline.AddArc(0, Height - diameter - 1, diameter, diameter, 90, 90); outline.CloseFigure();
            args.Graphics.Clear(Parent == null ? Color.FromArgb(38, 38, 38) : Parent.BackColor);
            args.Graphics.FillPath(fill, outline); args.Graphics.DrawPath(border, outline);
        }
    }
}

internal sealed class RoundedCard : Panel
{
    public Color BorderColor = Color.FromArgb(218, 226, 238);
    public int Radius = 14;
    public RoundedCard()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        BackColor = Color.White; Padding = Padding.Empty;
    }
    protected override void OnPaint(PaintEventArgs args)
    {
        base.OnPaint(args); if (Width < Radius * 2 || Height < Radius * 2) return;
        args.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using (var path = new GraphicsPath())
        using (var fill = new SolidBrush(BackColor))
        using (var border = new Pen(BorderColor))
        {
            var rect = new Rectangle(0, 0, Width - 1, Height - 1); var diameter = Radius * 2;
            path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90); path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90); path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90); path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90); path.CloseFigure();
            args.Graphics.FillPath(fill, path); args.Graphics.DrawPath(border, path);
        }
    }
}

internal sealed class RoundedActionButton : Button
{
    public Color FillColor = Color.FromArgb(49, 124, 255);
    public Color HoverColor = Color.FromArgb(69, 140, 255);
    public Color DisabledColor = Color.FromArgb(218, 225, 235);
    public Color TextColor = Color.White;
    public int CornerRadius = 9;
    private bool hovered;
    public RoundedActionButton()
    {
        FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0; UseVisualStyleBackColor = false; TabStop = false; Padding = Padding.Empty;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        MouseEnter += delegate { hovered = true; Invalidate(); }; MouseLeave += delegate { hovered = false; Invalidate(); };
    }
    protected override void OnPaint(PaintEventArgs args)
    {
        args.Graphics.SmoothingMode = SmoothingMode.AntiAlias; args.Graphics.Clear(Parent == null ? Color.White : Parent.BackColor);
        var rect = new Rectangle(0, 0, Width - 1, Height - 1); var radius = Math.Min(CornerRadius, Height / 2);
        using (var path = new GraphicsPath()) using (var fill = new SolidBrush(!Enabled ? DisabledColor : hovered ? HoverColor : FillColor))
        {
            var diameter = radius * 2; path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90); path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90); path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90); path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90); path.CloseFigure(); args.Graphics.FillPath(fill, path);
        }
        var color = !Enabled ? Color.FromArgb(145, 155, 170) : TextColor; var textBounds = ClientRectangle;
        if (Image != null) { var imageSize = Image.Size; var total = imageSize.Width + 8 + TextRenderer.MeasureText(Text, Font).Width; var left = Math.Max(8, (Width - total) / 2); args.Graphics.DrawImage(Image, left, (Height - imageSize.Height) / 2, imageSize.Width, imageSize.Height); textBounds = new Rectangle(left + imageSize.Width + 8, 0, Math.Max(1, Width - left - imageSize.Width - 8), Height); }
        TextRenderer.DrawText(args.Graphics, Text, Font, textBounds, color, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
    }
}

internal sealed class BrowserLauncherTile : Panel
{
    private readonly ComboBox browser;
    private readonly TextBox url;
    private readonly Label status;
    public event Action<BrowserLauncherTile, string, string> Submit;
    public event Action<BrowserLauncherTile> Cancel;
    public bool Busy { get; private set; }
    public BrowserLauncherTile()
    {
        BackColor = Color.FromArgb(12, 12, 12); BorderStyle = BorderStyle.None; Margin = Padding.Empty;
        var body = new TableLayoutPanel { Dock = DockStyle.Fill, Margin = Padding.Empty, Padding = new Padding(8, 12, 8, 0), ColumnCount = 2, RowCount = 4, BackColor = BackColor }; body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 98)); body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        body.RowStyles.Add(new RowStyle(SizeType.Absolute, 44)); body.RowStyles.Add(new RowStyle(SizeType.Absolute, 44)); body.RowStyles.Add(new RowStyle(SizeType.Absolute, 64)); body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var browserLabel = ShellLabel("browser >"); body.Controls.Add(browserLabel, 0, 0); browser = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11), AccessibleName = "浏览器" }; browser.Items.AddRange(new object[] { "Microsoft Edge", "Google Chrome" }); browser.SelectedIndex = 0; var browserInput = new RoundedInput(browser); body.Controls.Add(browserInput, 1, 0);
        browser.DrawMode = DrawMode.OwnerDrawFixed; browser.ItemHeight = 24;
        browser.DrawItem += delegate(object sender, DrawItemEventArgs args)
        {
            var selected = (args.State & DrawItemState.Selected) != 0 && (args.State & DrawItemState.ComboBoxEdit) == 0;
            using (var background = new SolidBrush(selected ? Color.FromArgb(65, 65, 65) : browser.BackColor)) args.Graphics.FillRectangle(background, args.Bounds);
            if (args.Index >= 0) TextRenderer.DrawText(args.Graphics, browser.Items[args.Index].ToString(), browser.Font, new Rectangle(args.Bounds.X + 3, args.Bounds.Y, Math.Max(1, args.Bounds.Width - 6), args.Bounds.Height), browser.Enabled ? Color.White : Color.Gray, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis);
        };
        var urlLabel = ShellLabel("url >"); body.Controls.Add(urlLabel, 0, 1); url = new TextBox { Text = "https://chatgpt.com/", BorderStyle = BorderStyle.None, Font = new Font("Consolas", 11), AccessibleName = "网址" }; url.KeyDown += UrlKeyDown; var urlInput = new RoundedInput(url); body.Controls.Add(urlInput, 1, 1);
        status = new Label { Dock = DockStyle.Fill, ForeColor = Color.Silver, Font = new Font("Consolas", 9), Margin = Padding.Empty, Padding = new Padding(0, 5, 0, 0) }; body.Controls.Add(status, 0, 2); body.SetColumnSpan(status, 2); Controls.Add(body);
        var stacked = false;
        Resize += delegate
        {
            var nextStacked = ClientSize.Width < 300;
            if (stacked == nextStacked) return;
            stacked = nextStacked; body.SuspendLayout();
            body.RowCount = stacked ? 6 : 4; body.RowStyles.Clear();
            var heights = stacked ? new[] { 28, 44, 28, 44, 64 } : new[] { 44, 44, 64 };
            foreach (var height in heights) body.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            body.ColumnStyles[0].Width = stacked ? 0 : 98;
            body.SetCellPosition(browserLabel, new TableLayoutPanelCellPosition(stacked ? 1 : 0, 0));
            body.SetCellPosition(browserInput, new TableLayoutPanelCellPosition(1, stacked ? 1 : 0));
            body.SetCellPosition(urlLabel, new TableLayoutPanelCellPosition(stacked ? 1 : 0, stacked ? 2 : 1));
            body.SetCellPosition(urlInput, new TableLayoutPanelCellPosition(1, stacked ? 3 : 1));
            body.SetCellPosition(status, new TableLayoutPanelCellPosition(0, stacked ? 4 : 2)); body.ResumeLayout(true);
        };
        browser.SelectedIndexChanged += delegate { url.Focus(); };
    }
    private static Label ShellLabel(string text) { return new Label { Text = text, Dock = DockStyle.Fill, ForeColor = Color.Gainsboro, Font = new Font("Consolas", 11), TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, Margin = new Padding(0, 0, 0, 6), UseMnemonic = false }; }
    private void UrlKeyDown(object sender, KeyEventArgs e) { if (Busy) return; if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; if (Submit != null) Submit(this, browser.SelectedIndex == 1 ? "chrome" : "msedge", url.Text.Trim()); } else if (e.KeyCode == Keys.Escape) { e.SuppressKeyPress = true; if (Cancel != null) Cancel(this); } }
    public void SetBusy(string message) { Busy = true; url.Enabled = false; browser.Enabled = false; status.Text = message; }
    public void SetError(string message) { Busy = false; url.Enabled = true; browser.Enabled = true; status.Text = message; url.Focus(); }
    public void FocusBrowser() { browser.Focus(); }
    public void SetUrl(string value) { url.Text = value; url.Focus(); }
    public void SetBrowser(string value) { browser.SelectedIndex = value == "chrome" ? 1 : 0; }
    public string BrowserKind { get { return browser.SelectedIndex == 1 ? "chrome" : "msedge"; } }
    public string Url { get { return url.Text.Trim(); } }
}

public sealed class PaneBookmark
{
    public string Browser { get; set; }
    public string Url { get; set; }
    public bool Launcher { get; set; }
}

public sealed class WorkspaceBookmark
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<PaneBookmark> Panes { get; set; }
    public WorkspaceBookmark() { Panes = new List<PaneBookmark>(); }
}

internal sealed class FavoriteItem
{
    public string Name;
    public string Url;
    public override string ToString() { return Name; }
}

internal sealed class TabMenuColors : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground { get { return Color.FromArgb(38, 38, 38); } }
    public override Color ImageMarginGradientBegin { get { return ToolStripDropDownBackground; } }
    public override Color ImageMarginGradientMiddle { get { return ToolStripDropDownBackground; } }
    public override Color ImageMarginGradientEnd { get { return ToolStripDropDownBackground; } }
    public override Color MenuBorder { get { return Color.FromArgb(70, 70, 70); } }
    public override Color MenuItemSelected { get { return Color.FromArgb(60, 60, 60); } }
    public override Color MenuItemBorder { get { return MenuItemSelected; } }
    public override Color SeparatorDark { get { return MenuBorder; } }
    public override Color SeparatorLight { get { return MenuBorder; } }
}

internal sealed class TabMenuRenderer : ToolStripProfessionalRenderer
{
    public TabMenuRenderer() : base(new TabMenuColors()) { RoundedEdges = false; }
    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs args) { args.ArrowColor = args.Item.Enabled ? Color.Gainsboro : Color.Gray; base.OnRenderArrow(args); }
    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs args)
    {
        var entry = args.Item as ToolStripMenuItem;
        if (entry != null && entry.ShowShortcutKeys && entry.ShortcutKeys != Keys.None)
        {
            var shortcutText = ShortcutSettingsDialog.FormatShortcut(entry.ShortcutKeys);
            var shortcutWidth = TextRenderer.MeasureText(shortcutText, args.TextFont).Width;
            var shortcutBounds = new Rectangle(args.Item.Width - shortcutWidth - 16, 0, shortcutWidth, args.Item.Height);
            TextRenderer.DrawText(args.Graphics, shortcutText, args.TextFont, shortcutBounds, Color.Silver, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
            TextRenderer.DrawText(args.Graphics, entry.Text, args.TextFont, new Rectangle(36, 0, Math.Max(1, shortcutBounds.Left - 48), args.Item.Height), Color.Gainsboro, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
            return;
        }
        var bounds = new Rectangle(36, 0, Math.Max(1, args.Item.Width - 56), args.Item.Height);
        TextRenderer.DrawText(args.Graphics, args.Text, args.TextFont, bounds, args.Item.Enabled ? Color.Gainsboro : Color.Gray, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
    }
    protected override void OnRenderItemImage(ToolStripItemImageRenderEventArgs args)
    {
        if (args.Image != null) args.Graphics.DrawImage(args.Image, new Rectangle(10, (args.Item.Height - 16) / 2, 16, 16));
    }
}

internal sealed class ShortcutSettingsDialog : Form
{
    private readonly TextBox shortcut;
    public Keys Shortcut { get; private set; }
    public ShortcutSettingsDialog(Keys current)
    {
        Text = "快捷键设置"; ClientSize = new Size(360, 140); StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog; MinimizeBox = false; MaximizeBox = false; KeyPreview = true; BackColor = Color.FromArgb(38, 38, 38); ForeColor = Color.Gainsboro;
        var label = new Label { Text = "新增横向窗格", AutoSize = true, Location = new Point(16, 18) };
        shortcut = new TextBox { ReadOnly = true, BackColor = Color.FromArgb(25, 25, 25), ForeColor = Color.White, Bounds = new Rectangle(16, 44, 328, 26), Text = FormatShortcut(current) };
        var save = new Button { Text = "保存", DialogResult = DialogResult.OK, Bounds = new Rectangle(180, 96, 76, 28) };
        var cancel = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Bounds = new Rectangle(268, 96, 76, 28) };
        Controls.Add(label); Controls.Add(shortcut); Controls.Add(save); Controls.Add(cancel); AcceptButton = save; CancelButton = cancel; Shortcut = current;
        KeyDown += CaptureShortcut;
        Shown += delegate { shortcut.Focus(); };
    }
    private void CaptureShortcut(object sender, KeyEventArgs args)
    {
        if (args.KeyCode == Keys.Escape) return;
        var modifiers = args.Modifiers & (Keys.Control | Keys.Shift | Keys.Alt);
        if (modifiers == Keys.None || args.KeyCode == Keys.ControlKey || args.KeyCode == Keys.ShiftKey || args.KeyCode == Keys.Menu) return;
        Shortcut = modifiers | (args.KeyCode == Keys.Add ? Keys.Oemplus : args.KeyCode);
        shortcut.Text = FormatShortcut(Shortcut); args.SuppressKeyPress = true; args.Handled = true;
    }
    public static string FormatShortcut(Keys value)
    {
        var text = "";
        if ((value & Keys.Control) != Keys.None) text += "Ctrl+";
        if ((value & Keys.Shift) != Keys.None) text += "Shift+";
        if ((value & Keys.Alt) != Keys.None) text += "Alt+";
        var key = value & Keys.KeyCode;
        return text + (key == Keys.Oemplus ? "Plus" : key.ToString());
    }
}

internal sealed class BroadcastMessageDialog : Form
{
    private readonly TextBox message;
    private readonly int targetCount;
    public string MessageText { get { return message.Text.Trim(); } }
    public BroadcastMessageDialog() : this(0) { }
    public BroadcastMessageDialog(int targetCount)
    {
        this.targetCount = targetCount; Text = "批量提问"; ClientSize = new Size(680, 360); StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; BackColor = Color.FromArgb(32, 32, 32); ForeColor = Color.Gainsboro; Font = new Font("Segoe UI", 9);
        var card = new RoundedCard { Bounds = new Rectangle(0, 0, 680, 360), BackColor = Color.FromArgb(45, 45, 45), BorderColor = Color.FromArgb(78, 78, 78), Radius = 7 };
        var icon = new PictureBox { Image = CreateBatchIcon(27), SizeMode = PictureBoxSizeMode.CenterImage, Bounds = new Rectangle(22, 19, 28, 28), BackColor = Color.Transparent };
        var title = new Label { Text = "批量提问", AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI Semibold", 14), Location = new Point(60, 20) };
        var subtitle = new Label { Text = "发送到当前标签页的 " + targetCount + " 个窗格", AutoSize = true, ForeColor = Color.FromArgb(190, 190, 190), Font = new Font("Segoe UI", 9), Location = new Point(61, 47) };
        var close = new Button { Bounds = new Rectangle(638, 17, 28, 28), FlatStyle = FlatStyle.Flat, Image = CreateBatchCloseIcon(16), ImageAlign = ContentAlignment.MiddleCenter, BackColor = Color.FromArgb(45, 45, 45), TabStop = false, AccessibleName = "关闭" };
        close.FlatAppearance.BorderSize = 0; close.FlatAppearance.MouseOverBackColor = Color.FromArgb(65, 65, 65); close.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
        message = new TextBox { Multiline = true, AcceptsReturn = true, ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 11), Bounds = new Rectangle(0, 0, 1, 1), BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.FromArgb(242, 242, 242), AccessibleName = "批量消息" };
        var editor = new RoundedEditor(message) { Bounds = new Rectangle(22, 82, 636, 170), BackColor = Color.FromArgb(30, 30, 30), Radius = 6 };
        message.BackColor = Color.FromArgb(30, 30, 30);
        var divider = new Panel { Bounds = new Rectangle(0, 273, 680, 1), BackColor = Color.FromArgb(63, 63, 63) };
        var target = new Label { Text = "当前标签页  ·  " + targetCount + " 个窗格", AutoSize = true, ForeColor = Color.FromArgb(180, 180, 180), Font = new Font("Segoe UI", 9), Location = new Point(22, 299) };
        var clear = new Button { Text = "清空", Bounds = new Rectangle(352, 291, 64, 34), FlatStyle = FlatStyle.Flat, Image = CreateBatchTrashIcon(15), ImageAlign = ContentAlignment.MiddleLeft, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(205, 205, 205), BackColor = Color.FromArgb(45, 45, 45), TabStop = false, AccessibleName = "清空消息" };
        clear.FlatAppearance.BorderSize = 0; clear.FlatAppearance.MouseOverBackColor = Color.FromArgb(65, 65, 65); clear.Click += delegate { message.Clear(); message.Focus(); };
        var cancel = new Button { Text = "取消", Bounds = new Rectangle(486, 291, 76, 34), FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel, Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(235, 235, 235), BackColor = Color.FromArgb(59, 59, 59), TabStop = false, AccessibleName = "取消" };
        cancel.FlatAppearance.BorderSize = 0; cancel.FlatAppearance.MouseOverBackColor = Color.FromArgb(75, 75, 75);
        var send = new RoundedActionButton { Text = "发送", Bounds = new Rectangle(570, 291, 88, 34), Font = new Font("Segoe UI Semibold", 9), Image = CreateBatchSendIcon(15, Color.FromArgb(20, 20, 20)), ImageAlign = ContentAlignment.MiddleLeft, DialogResult = DialogResult.OK, AccessibleName = "发送批量消息", CornerRadius = 4, FillColor = Color.FromArgb(101, 177, 232), HoverColor = Color.FromArgb(124, 192, 238), DisabledColor = Color.FromArgb(68, 68, 68), TextColor = Color.FromArgb(20, 20, 20) };
        send.Enabled = false; message.TextChanged += delegate { send.Enabled = !String.IsNullOrWhiteSpace(message.Text); };
        card.Controls.Add(icon); card.Controls.Add(title); card.Controls.Add(subtitle); card.Controls.Add(close); card.Controls.Add(editor); card.Controls.Add(divider); card.Controls.Add(target); card.Controls.Add(clear); card.Controls.Add(cancel); card.Controls.Add(send); Controls.Add(card); AcceptButton = send; CancelButton = cancel;
        Shown += delegate { message.Focus(); NativeMethods.SendMessage(message.Handle, 0x1501, new IntPtr(1), "请输入你想问的问题..."); };
    }
    private static Bitmap CreateBatchIcon(int size) { return CreateBatchGlyph(size, Color.FromArgb(47, 124, 255), 0); }
    private static Bitmap CreateBatchTargetIcon(int size) { return CreateBatchGlyph(size, Color.FromArgb(91, 111, 143), 1); }
    private static Bitmap CreateBatchSendIcon(int size) { return CreateBatchSendIcon(size, Color.White); }
    private static Bitmap CreateBatchSendIcon(int size, Color color) { return CreateBatchGlyph(size, color, 2); }
    private static Bitmap CreateBatchTrashIcon(int size) { return CreateBatchGlyph(size, Color.FromArgb(205, 205, 205), 3); }
    private static Bitmap CreateBatchCloseIcon(int size) { return CreateBatchGlyph(size, Color.FromArgb(123, 143, 175), 4); }
    private static Bitmap CreateBatchGlyph(int size, Color color, int type)
    {
        var bitmap = new Bitmap(size, size); using (var graphics = Graphics.FromImage(bitmap)) using (var pen = new Pen(color, Math.Max(1.2f, size / 8f))) { graphics.SmoothingMode = SmoothingMode.AntiAlias; graphics.Clear(Color.Transparent); var m = size / 2f;
            if (type == 0 || type == 1) { graphics.DrawRectangle(pen, size * .20f, size * .20f, size * .50f, size * .56f); graphics.DrawRectangle(pen, size * .35f, size * .34f, size * .50f, size * .56f); if (type == 0) graphics.DrawLine(pen, size * .52f, size * .55f, size * .76f, size * .55f); }
            else if (type == 2) { graphics.DrawLine(pen, size * .18f, m, size * .78f, m); graphics.DrawLine(pen, size * .56f, size * .30f, size * .80f, m); graphics.DrawLine(pen, size * .56f, size * .70f, size * .80f, m); }
            else if (type == 3) { graphics.DrawLine(pen, size * .28f, size * .34f, size * .72f, size * .34f); graphics.DrawRectangle(pen, size * .34f, size * .40f, size * .32f, size * .42f); graphics.DrawLine(pen, size * .42f, size * .24f, size * .58f, size * .24f); }
            else { graphics.DrawLine(pen, size * .25f, size * .25f, size * .75f, size * .75f); graphics.DrawLine(pen, size * .75f, size * .25f, size * .25f, size * .75f); }
        } return bitmap;
    }
}

internal sealed class CommandPaletteDialog : Form
{
    private sealed class CommandItem
    {
        public string Name;
        public string Description;
        public string Shortcut;
        public bool EditableShortcut;
        public Action Execute;
    }
    private readonly TextBox search;
    private readonly ListBox commands;
    private readonly List<CommandItem> allCommands;
    private readonly Action<string, Keys> applyShortcut;
    private CommandItem editingCommand;
    private Keys pendingShortcut;
    public Action SelectedCommand { get; private set; }
    public void CloseFromOutside()
    {
        if (IsDisposed) return;
        DialogResult = DialogResult.Cancel;
        Close();
    }
    public CommandPaletteDialog(IEnumerable<Tuple<string, string, string, Action>> items, Action<string, Keys> applyShortcut)
    {
        Text = "命令面板"; ClientSize = new Size(720, 520); StartPosition = FormStartPosition.Manual;
        FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; KeyPreview = true; BackColor = Color.FromArgb(38, 38, 38); ForeColor = Color.Gainsboro; Font = new Font("Segoe UI", 9);
        this.applyShortcut = applyShortcut;
        allCommands = items.Select(item => new CommandItem { Name = item.Item1, Description = item.Item2, Shortcut = item.Item3, EditableShortcut = item.Item1 == "向所有 Agent Chat 发送消息" || item.Item1 == "新增横向窗格", Execute = item.Item4 }).ToList();
        search = new TextBox { Bounds = new Rectangle(12, 12, 696, 30), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, Font = new Font("Segoe UI", 11), BackColor = Color.FromArgb(25, 25, 25), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, AccessibleName = "输入命令名称" };
        commands = new ListBox { Bounds = new Rectangle(12, 50, 696, 458), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right, BorderStyle = BorderStyle.None, IntegralHeight = false, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 42, BackColor = Color.FromArgb(38, 38, 38), ForeColor = Color.Gainsboro };
        commands.DrawItem += DrawCommand;
        search.TextChanged += delegate { RefreshCommands(); };
        search.KeyDown += HandleSearchKey; commands.MouseClick += HandleCommandClick; commands.KeyDown += HandleListKey;
        Controls.Add(search); Controls.Add(commands);
        Shown += delegate
        {
            if (Owner != null)
            {
                var width = Math.Min(1120, Math.Max(620, Owner.ClientSize.Width - 80));
                var height = Math.Min(760, Math.Max(420, Owner.ClientSize.Height - 80));
                ClientSize = new Size(width, height);
                Location = Owner.PointToScreen(new Point((Owner.ClientSize.Width - Width) / 2, 40));
            }
            search.Text = ""; NativeMethods.SendMessage(search.Handle, 0x1501, new IntPtr(1), "输入命令名称..."); RefreshCommands(); search.Focus();
        };
        Deactivate += delegate
        {
            BeginInvoke((Action)delegate
            {
                if (IsDisposed || !IsHandleCreated) return;
                var foreground = NativeMethods.GetForegroundWindow();
                if (foreground == Handle || NativeMethods.IsChild(Handle, foreground) || NativeMethods.GetAncestor(foreground, 2) == Handle) return;
                DialogResult = DialogResult.Cancel; Close();
            });
        };
        KeyDown += delegate(object sender, KeyEventArgs args) { if (args.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); args.SuppressKeyPress = true; } };
    }
    private void RefreshCommands()
    {
        var query = search == null ? "" : search.Text.Trim(); commands.BeginUpdate(); commands.Items.Clear();
        foreach (var item in allCommands.Where(item => String.IsNullOrWhiteSpace(query) || item.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 || item.Description.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)) commands.Items.Add(item);
        if (commands.Items.Count > 0) commands.SelectedIndex = 0; commands.EndUpdate();
    }
    private void DrawCommand(object sender, DrawItemEventArgs args)
    {
        if (args.Index < 0) return; var item = (CommandItem)commands.Items[args.Index]; var selected = (args.State & DrawItemState.Selected) != 0;
        using (var background = new SolidBrush(selected ? Color.FromArgb(60, 60, 60) : Color.FromArgb(38, 38, 38))) args.Graphics.FillRectangle(background, args.Bounds);
        var shortcutWidth = Math.Max(160, TextRenderer.MeasureText(item.Shortcut, Font).Width + 16);
        TextRenderer.DrawText(args.Graphics, item.Name, Font, new Rectangle(args.Bounds.X + 12, args.Bounds.Y + 5, Math.Max(1, args.Bounds.Width - shortcutWidth - 24), 18), Color.White, TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
        var shortcutBounds = GetShortcutBounds(args.Bounds);
        if (item == editingCommand)
        {
            using (var fill = new SolidBrush(Color.FromArgb(25, 25, 25))) args.Graphics.FillRectangle(fill, shortcutBounds);
            using (var border = new Pen(Color.FromArgb(140, 190, 220))) args.Graphics.DrawRectangle(border, shortcutBounds);
            var text = pendingShortcut == Keys.None ? "按下快捷键" : ShortcutSettingsDialog.FormatShortcut(pendingShortcut);
            TextRenderer.DrawText(args.Graphics, text, Font, new Rectangle(shortcutBounds.X + 8, shortcutBounds.Y, shortcutBounds.Width - 34, shortcutBounds.Height), Color.White, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(args.Graphics, "\uE73E", new Font("Segoe MDL2 Assets", 11), new Rectangle(shortcutBounds.Right - 28, shortcutBounds.Y, 24, shortcutBounds.Height), pendingShortcut == Keys.None ? Color.Gray : Color.Gainsboro, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
        else
        {
            TextRenderer.DrawText(args.Graphics, String.IsNullOrWhiteSpace(item.Shortcut) ? "无" : item.Shortcut, Font, new Rectangle(shortcutBounds.X, args.Bounds.Y + 5, shortcutBounds.Width, 24), Color.Silver, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
        using (var descriptionFont = new Font("Segoe UI", 8)) TextRenderer.DrawText(args.Graphics, item.Description, descriptionFont, new Rectangle(args.Bounds.X + 12, args.Bounds.Y + 23, args.Bounds.Width - 24, 16), Color.Silver, TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
    }
    private void HandleSearchKey(object sender, KeyEventArgs args) { if (args.KeyCode == Keys.Down) { commands.Focus(); args.SuppressKeyPress = true; } else if (args.KeyCode == Keys.Enter) { ExecuteSelected(); args.SuppressKeyPress = true; } else if (args.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); } }
    private void HandleListKey(object sender, KeyEventArgs args)
    {
        if (editingCommand != null) { CaptureShortcut(args); return; }
        if (args.KeyCode == Keys.Enter) { ExecuteSelected(); args.SuppressKeyPress = true; } else if (args.KeyCode == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); }
    }
    private void HandleCommandClick(object sender, MouseEventArgs args)
    {
        var index = commands.IndexFromPoint(args.Location); if (index < 0 || index >= commands.Items.Count) return;
        var item = (CommandItem)commands.Items[index]; var row = commands.GetItemRectangle(index); var shortcutBounds = GetShortcutBounds(row);
        if (item.EditableShortcut && shortcutBounds.Contains(args.Location))
        {
            if (item == editingCommand && pendingShortcut != Keys.None && args.Location.X >= shortcutBounds.Right - 34) { applyShortcut(item.Name, pendingShortcut); item.Shortcut = ShortcutSettingsDialog.FormatShortcut(pendingShortcut); editingCommand = null; pendingShortcut = Keys.None; commands.Invalidate(row); return; }
            editingCommand = item; pendingShortcut = Keys.None; commands.Focus(); commands.Invalidate(row); return;
        }
        ExecuteSelected();
    }
    private void CaptureShortcut(KeyEventArgs args)
    {
        if (args.KeyCode == Keys.Escape) { editingCommand = null; pendingShortcut = Keys.None; commands.Invalidate(); args.SuppressKeyPress = true; return; }
        if (args.KeyCode == Keys.ControlKey || args.KeyCode == Keys.ShiftKey || args.KeyCode == Keys.Menu) return;
        var modifiers = args.Modifiers & (Keys.Control | Keys.Shift | Keys.Alt);
        if (modifiers == Keys.None) return;
        pendingShortcut = modifiers | (args.KeyCode == Keys.Add ? Keys.Oemplus : args.KeyCode); args.SuppressKeyPress = true; args.Handled = true; commands.Invalidate();
    }
    private static Rectangle GetShortcutBounds(Rectangle row) { return new Rectangle(Math.Max(row.X, row.Right - 188), row.Y + 4, Math.Min(176, row.Width - 12), Math.Max(1, row.Height - 8)); }
    private void ExecuteSelected() { if (commands.SelectedItem == null) return; SelectedCommand = ((CommandItem)commands.SelectedItem).Execute; DialogResult = DialogResult.OK; Close(); }
}

internal sealed class MainForm : Form
{
    private readonly List<Workspace> workspaces = new List<Workspace>();
    private readonly Panel deck = new Panel();
    private Workspace selectedWorkspace;
    private readonly FlowLayoutPanel tabButtons = new FlowLayoutPanel();
    private readonly ContextMenuStrip favorites = new ContextMenuStrip();
    private readonly HashSet<ToolStripDropDown> openMenus = new HashSet<ToolStripDropDown>();
    private readonly List<FavoriteItem> favoriteItems = new List<FavoriteItem>();
    private readonly List<WorkspaceBookmark> workspaceFavorites = new List<WorkspaceBookmark>();
    private readonly string favoritesFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AgentDock", "favorites.txt");
    private readonly string workspaceFavoritesFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AgentDock", "workspaces.xml");
    private readonly string settingsFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AgentDock", "settings.txt");
    private readonly System.Threading.SemaphoreSlim launchLock = new System.Threading.SemaphoreSlim(1, 1);
    private readonly ToolTip tooltips = new ToolTip();
    private readonly System.Windows.Forms.Timer browserMonitor = new System.Windows.Forms.Timer { Interval = 250 };
    private Button maximizeButton;
    private NativeMethods.KeyboardHookProc keyboardCallback;
    private IntPtr keyboardHook;
    private NativeMethods.KeyboardHookProc mouseCallback;
    private IntPtr mouseHook;
    private bool splitKeyHeld;
    private bool commandKeyHeld;
    private bool broadcastKeyHeld;
    private bool hookControlDown;
    private bool hookShiftDown;
    private bool hookAltDown;
    private bool browserInputArmed;
    private int lastHeaderClickTick;
    private Point lastHeaderClickPoint;
    private Icon appIcon;
    private bool broadcasting;
    private CommandPaletteDialog activeCommandPalette;
    private const uint SplitMessage = 0x8001;
    private const uint CommandPaletteMessage = 0x8002;
    private const uint BroadcastMessage = 0x8003;
    private NativeMethods.WinEventProc browserEventCallback;
    private IntPtr browserEventHook;
    private HashSet<IntPtr> launchBaseline;
    private string launchingProcess;
    private IntPtr capturedBrowser;
    private Keys splitShortcut = Keys.Control | Keys.Shift | Keys.Oemplus;
    private Keys broadcastShortcut = Keys.Control | Keys.Shift | Keys.Enter;

    private static Bitmap CreateIconBitmap(string glyph, int size)
    {
        var bitmap = new Bitmap(size, size);
        using (var graphics = Graphics.FromImage(bitmap))
        using (var pen = new Pen(Color.Gainsboro, Math.Max(1.2f, size / 10f)))
        using (var fill = new SolidBrush(Color.Gainsboro))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);
            var inset = size * 0.22f; var middle = size / 2f; var end = size - inset;
            if (glyph == "\uE710")
            {
                graphics.DrawLine(pen, middle, inset, middle, end); graphics.DrawLine(pen, inset, middle, end, middle);
            }
            else if (glyph == "\uE70D")
            {
                graphics.DrawLines(pen, new[] { new PointF(inset, size * .38f), new PointF(middle, size * .64f), new PointF(end, size * .38f) });
            }
            else if (glyph == "\uE921") graphics.DrawLine(pen, inset, middle, end, middle);
            else if (glyph == "\uE922") graphics.DrawRectangle(pen, inset, inset, end - inset, end - inset);
            else if (glyph == "\uE923")
            {
                graphics.DrawRectangle(pen, size * .30f, inset, size * .48f, size * .48f);
                graphics.DrawLine(pen, size * .28f, size * .40f, size * .28f, size * .78f); graphics.DrawLine(pen, size * .28f, size * .78f, size * .68f, size * .78f);
            }
            else if (glyph == "\uE8BB")
            {
                graphics.DrawLine(pen, inset, inset, end, end); graphics.DrawLine(pen, end, inset, inset, end);
            }
            else if (glyph == "\uE734")
            {
                var points = new PointF[10]; for (var index = 0; index < points.Length; index++) { var angle = -Math.PI / 2 + index * Math.PI / 5; var radius = (float)(index % 2 == 0 ? size * .43 : size * .19); points[index] = new PointF(middle + (float)Math.Cos(angle) * radius, middle + (float)Math.Sin(angle) * radius); }
                graphics.FillPolygon(fill, points);
            }
            else if (glyph == "\uE8AC")
            {
                graphics.DrawLine(pen, size * .28f, size * .72f, size * .70f, size * .30f); graphics.DrawLine(pen, size * .24f, size * .78f, size * .38f, size * .74f); graphics.DrawLine(pen, size * .67f, size * .27f, size * .75f, size * .35f);
            }
            else if (glyph == "\uE73E")
            {
                graphics.DrawLine(pen, size * .25f, size * .50f, size * .70f, size * .50f); graphics.DrawLine(pen, size * .52f, size * .32f, size * .73f, size * .50f); graphics.DrawLine(pen, size * .52f, size * .68f, size * .73f, size * .50f);
            }
            else graphics.DrawRectangle(pen, inset, inset, end - inset, end - inset);
        }
        return bitmap;
    }
    private static Bitmap CreateAppIconBitmap(int size)
    {
        var bitmap = new Bitmap(size, size);
        using (var graphics = Graphics.FromImage(bitmap))
        using (var background = new SolidBrush(Color.FromArgb(16, 27, 47)))
        using (var panel = new SolidBrush(Color.FromArgb(76, 194, 255)))
        using (var panelShade = new SolidBrush(Color.FromArgb(42, 131, 211)))
        using (var accent = new SolidBrush(Color.FromArgb(255, 205, 83)))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias; graphics.Clear(Color.Transparent);
            using (var path = new GraphicsPath())
            {
                var radius = Math.Max(3, size / 5); var rect = new Rectangle(1, 1, size - 2, size - 2);
                path.AddArc(rect.Left, rect.Top, radius, radius, 180, 90); path.AddArc(rect.Right - radius, rect.Top, radius, radius, 270, 90); path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90); path.AddArc(rect.Left, rect.Bottom - radius, radius, radius, 90, 90); path.CloseFigure(); graphics.FillPath(background, path);
            }
            var top = size * .28f; var bottom = size * .72f; var width = size * .14f;
            graphics.FillRectangle(panelShade, size * .18f, top, width, bottom - top);
            graphics.FillRectangle(panel, size * .43f, top, width, bottom - top);
            graphics.FillRectangle(panelShade, size * .68f, top, width, bottom - top);
            graphics.FillEllipse(accent, size * .42f, size * .12f, size * .16f, size * .16f);
        }
        return bitmap;
    }
    private static Icon LoadAppIcon()
    {
        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AgentDock.ico");
            if (File.Exists(path)) using (var source = new Icon(path)) return (Icon)source.Clone();
        }
        catch { }
        try
        {
            using (var source = Icon.ExtractAssociatedIcon(Application.ExecutablePath)) return source == null ? null : (Icon)source.Clone();
        }
        catch { }
        using (var bitmap = CreateAppIconBitmap(256))
        {
            var handle = bitmap.GetHicon();
            try { using (var source = Icon.FromHandle(handle)) return (Icon)source.Clone(); }
            finally { NativeMethods.DestroyIcon(handle); }
        }
    }

    public MainForm()
    {
        Text = "AgentDock"; Width = 1280; Height = 820; MinimumSize = new Size(760, 520); StartPosition = FormStartPosition.CenterScreen; FormBorderStyle = FormBorderStyle.None; BackColor = Color.FromArgb(32, 32, 32); ForeColor = Color.White; Font = new Font("Segoe UI", 9); WindowState = FormWindowState.Maximized;
        appIcon = LoadAppIcon(); if (appIcon != null) Icon = appIcon;
        LoadSettings(); LoadFavorites(); LoadWorkspaceFavorites(); favorites.Renderer = new TabMenuRenderer(); favorites.Font = Font; favorites.Padding = new Padding(0, 4, 0, 4); TrackMenu(favorites); BuildUi(); AddWorkspace("工作区 1");
        browserMonitor.Tick += delegate { RemoveClosedBrowsers(); }; browserMonitor.Start();
        FormClosed += delegate { browserMonitor.Dispose(); if (Icon != null) Icon.Dispose(); appIcon = null; };
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty, Padding = Padding.Empty, BackColor = BackColor }; root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); Controls.Add(root);
        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty, BackColor = BackColor };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 138));
        tabButtons.Dock = DockStyle.Fill; tabButtons.Margin = Padding.Empty; tabButtons.Padding = new Padding(8, 0, 0, 0); tabButtons.WrapContents = false; tabButtons.AutoScroll = true; tabButtons.BackColor = BackColor;
        tabButtons.MouseDown += HeaderMouseDown; header.Controls.Add(tabButtons, 0, 0);
        var captions = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = Padding.Empty, WrapContents = false };
        var minimize = MakeCaption("\uE921", "最小化"); minimize.Click += delegate { WindowState = FormWindowState.Minimized; }; captions.Controls.Add(minimize);
        maximizeButton = MakeCaption("\uE922", "最大化 / 还原"); maximizeButton.Click += delegate { ToggleMaximize(); }; captions.Controls.Add(maximizeButton);
        Resize += delegate { maximizeButton.Text = WindowState == FormWindowState.Maximized ? "\uE923" : "\uE922"; SetIcon(maximizeButton, 10); };
        header.DoubleClick += delegate { ToggleMaximize(); }; captions.DoubleClick += delegate { ToggleMaximize(); };
        var close = MakeCaption("\uE8BB", "关闭"); close.Click += delegate { Close(); }; close.FlatAppearance.MouseOverBackColor = Color.FromArgb(196, 43, 28); captions.Controls.Add(close); header.Controls.Add(captions, 1, 0);
        root.Controls.Add(header, 0, 0); deck.Dock = DockStyle.Fill; deck.Margin = Padding.Empty; deck.BackColor = Color.FromArgb(12, 12, 12); root.Controls.Add(deck, 0, 1);
        FormClosing += delegate
        {
            browserMonitor.Stop();
            foreach (var tile in workspaces.SelectMany(item => item.Tiles)) tile.HideBrowser();
            foreach (var workspace in workspaces)
            {
                foreach (var tile in workspace.Tiles)
                {
                    tile.CloseBrowser();
                }
            }
        };
        RefreshFavorites();
    }

    private static Button MakeButton(string text)
    {
        var button = new Button { Text = text, AutoSize = false, Height = 32, FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(32, 32, 32), ForeColor = Color.Gainsboro, Margin = new Padding(0), TabStop = false }; button.FlatAppearance.BorderSize = 0; button.FlatAppearance.MouseOverBackColor = Color.FromArgb(52, 52, 52); return button;
    }
    private static Button MakeTabButton(string text, bool selected)
    {
        var button = MakeButton(text); button.BackColor = selected ? Color.FromArgb(12, 12, 12) : Color.FromArgb(32, 32, 32); button.Padding = new Padding(8, 0, 8, 0); return button;
    }
    private static void SetIcon(Button button, float size)
    {
        var glyph = String.IsNullOrEmpty(button.Text) ? button.Tag as string : button.Text; if (glyph == null) return; button.Tag = glyph; button.Text = "";
        if (button.Image != null) button.Image.Dispose();
        button.Image = CreateIconBitmap(glyph, (int)Math.Max(16, size * 1.8f)); button.ImageAlign = ContentAlignment.MiddleCenter; button.Padding = Padding.Empty;
    }
    private Button MakeIconButton(string glyph, string tooltip, int width)
    {
        var button = MakeButton(glyph); button.Width = width; button.Height = 32; SetIcon(button, 10); tooltips.SetToolTip(button, tooltip); button.AccessibleName = tooltip; return button;
    }
    private void HeaderMouseDown(object sender, MouseEventArgs args)
    {
        if (args.Button != MouseButtons.Left) return;
        var now = Environment.TickCount; var delta = unchecked(now - lastHeaderClickTick);
        var doubleClick = delta >= 0 && delta <= SystemInformation.DoubleClickTime && Math.Abs(args.X - lastHeaderClickPoint.X) <= SystemInformation.DoubleClickSize.Width && Math.Abs(args.Y - lastHeaderClickPoint.Y) <= SystemInformation.DoubleClickSize.Height;
        lastHeaderClickTick = now; lastHeaderClickPoint = args.Location;
        if (doubleClick) { lastHeaderClickTick = 0; ToggleMaximize(); return; }
        DragHeader(sender, args);
    }
    private Workspace CurrentWorkspace() { return selectedWorkspace; }
    private Workspace CreateWorkspace(string name) { var workspace = new Workspace(name); workspaces.Add(workspace); deck.Controls.Add(workspace); SelectWorkspace(workspace); return workspace; }
    private void AddWorkspace(string name) { CreateWorkspace(name); AddLauncher(null); }
    private void SelectWorkspace(Workspace workspace) { selectedWorkspace = workspace; foreach (var item in workspaces) item.Visible = item == workspace; workspace.BringToFront(); workspace.LayoutPanes(); RefreshTabButtons(); }
    private void RefreshTabButtons()
    {
        foreach (Control control in tabButtons.Controls.Cast<Control>().ToArray()) control.Dispose();
        foreach (var workspace in workspaces)
        {
            var item = workspace; var holder = new Panel { Width = 180, Height = 32, Margin = new Padding(0, 0, 2, 0) };
            var button = MakeTabButton(item.Text, item == selectedWorkspace); button.Dock = DockStyle.Fill; button.TextAlign = ContentAlignment.MiddleLeft; button.Click += delegate { SelectWorkspace(item); };
            var close = MakeTabButton("\uE8BB", item == selectedWorkspace); SetIcon(close, 10); close.Dock = DockStyle.Right; close.Width = 32; close.Click += delegate { CloseWorkspace(item); }; tooltips.SetToolTip(close, "关闭标签页"); close.AccessibleName = "关闭标签页";
            var menu = new ContextMenuStrip { Renderer = new TabMenuRenderer(), Font = Font, Padding = new Padding(0, 4, 0, 4), ImageScalingSize = new Size(16, 16) };
            menu.Items.Add(MakeTabMenuItem("重命名", "\uE8AC", delegate { RenameWorkspace(item); }));
            menu.Items.Add(MakeTabMenuItem("收藏标签页", "\uE734", delegate { SaveWorkspaceFavorite(item); }));
            menu.Items.Add(new ToolStripSeparator());
            var closeMenu = MakeTabMenuItem("关闭", null, null);
            closeMenu.DropDown.Renderer = menu.Renderer;
            closeMenu.DropDown.Font = menu.Font; closeMenu.DropDown.Padding = menu.Padding;
            var closePane = MakeTabMenuItem("关闭窗格", null, delegate { ClosePane(item); }); closeMenu.DropDownItems.Add(closePane);
            closeMenu.DropDownItems.Add(MakeTabMenuItem("关闭标签页", null, delegate { CloseWorkspace(item); }));
            var closeOthers = MakeTabMenuItem("关闭其他标签", null, delegate { CloseOtherWorkspaces(item); }); closeMenu.DropDownItems.Add(closeOthers);
            menu.Items.Add(closeMenu);
            menu.Items.Add(MakeTabMenuItem("关闭标签页", "\uE8BB", delegate { CloseWorkspace(item); }));
            menu.Disposed += delegate { foreach (ToolStripItem entry in menu.Items) if (entry.Image != null) entry.Image.Dispose(); };
            TrackMenu(menu);
            menu.Opening += delegate
            {
                var activeLauncher = item.ActivePane as BrowserLauncherTile;
                closePane.Enabled = item.Panes.Count > 0 && (activeLauncher == null || !activeLauncher.Busy);
                closeOthers.Enabled = workspaces.Count > 1;
            };
            holder.ContextMenuStrip = menu; button.ContextMenuStrip = menu; close.ContextMenuStrip = menu; holder.Disposed += delegate { menu.Dispose(); };
            holder.Controls.Add(button); holder.Controls.Add(close); holder.Controls.SetChildIndex(button, 0); tabButtons.Controls.Add(holder);
        }
        var plus = MakeIconButton("\uE710", "新增标签页", 32); plus.BackColor = BackColor; plus.Click += delegate { AddWorkspace("工作区 " + (workspaces.Count + 1)); }; tabButtons.Controls.Add(plus);
        var dropdown = MakeIconButton("\uE70D", "收藏与分屏", 24); dropdown.BackColor = BackColor; dropdown.Click += delegate { favorites.Show(dropdown, new Point(0, dropdown.Height)); }; tabButtons.Controls.Add(dropdown);
    }
    private static ToolStripMenuItem MakeTabMenuItem(string text, string glyph, EventHandler action)
    {
        var entry = new ToolStripMenuItem(text) { AutoSize = true, Padding = new Padding(0, 8, 0, 8) };
        if (action != null) entry.Click += action;
        if (glyph != null)
        {
            var icon = new Bitmap(16, 16);
            using (var source = CreateIconBitmap(glyph, 16)) using (var graphics = Graphics.FromImage(icon)) graphics.DrawImageUnscaled(source, 0, 0);
            entry.Image = icon;
        }
        return entry;
    }
    private void CloseWorkspace(Workspace workspace)
    {
        if (!workspaces.Contains(workspace)) return; foreach (var tile in workspace.Tiles) tile.HideBrowser(); foreach (var tile in workspace.Tiles) tile.CloseBrowser(); workspaces.Remove(workspace); deck.Controls.Remove(workspace); workspace.Dispose();
        if (workspaces.Count == 0) { Close(); return; } SelectWorkspace(workspaces.Contains(selectedWorkspace) ? selectedWorkspace : workspaces[workspaces.Count - 1]);
    }
    private void CloseOtherWorkspaces(Workspace workspace)
    {
        if (!workspaces.Contains(workspace)) return;
        SelectWorkspace(workspace);
        foreach (var other in workspaces.Where(item => item != workspace).ToArray()) CloseWorkspace(other);
    }
    private void TrackMenu(ToolStripDropDown menu)
    {
        menu.Opening += delegate { openMenus.Add(menu); };
        menu.Closed += delegate { openMenus.Remove(menu); };
    }
    private static bool MenuContainsPoint(ToolStripDropDown menu, Point screenPoint)
    {
        if (menu.Visible && menu.Bounds.Contains(screenPoint)) return true;
        foreach (ToolStripItem item in menu.Items)
        {
            var entry = item as ToolStripMenuItem;
            if (entry != null && entry.DropDown.Visible && MenuContainsPoint(entry.DropDown, screenPoint)) return true;
        }
        return false;
    }
    private void CloseOutsideOverlays(Point screenPoint)
    {
        foreach (var menu in openMenus.ToArray()) if (!MenuContainsPoint(menu, screenPoint)) menu.Close(ToolStripDropDownCloseReason.AppClicked);
        if (activeCommandPalette != null && !activeCommandPalette.IsDisposed && activeCommandPalette.IsHandleCreated)
        {
            NativeMethods.NativeRect bounds;
            if (NativeMethods.GetWindowRect(activeCommandPalette.Handle, out bounds) && (screenPoint.X < bounds.Left || screenPoint.X >= bounds.Right || screenPoint.Y < bounds.Top || screenPoint.Y >= bounds.Bottom))
            {
                try { activeCommandPalette.BeginInvoke((Action)activeCommandPalette.CloseFromOutside); } catch { }
            }
        }
    }
    private void AddLauncher(string presetUrl)
    {
        var workspace = CurrentWorkspace(); if (workspace == null) return; AddWorkspaceLauncher(workspace, "msedge", presetUrl);
    }
    private BrowserLauncherTile AddWorkspaceLauncher(Workspace workspace, string browserKind, string presetUrl)
    {
        var launcher = new BrowserLauncherTile(); launcher.Submit += LauncherSubmitted; launcher.Cancel += CancelLauncher; workspace.Launchers.Add(launcher); workspace.AddPane(launcher); launcher.SetBrowser(browserKind); if (!String.IsNullOrWhiteSpace(presetUrl)) launcher.SetUrl(presetUrl); else launcher.FocusBrowser(); return launcher;
    }
    private async void LauncherSubmitted(BrowserLauncherTile launcher, string processName, string url)
    {
        var workspace = workspaces.FirstOrDefault(item => item.Launchers.Contains(launcher)); if (workspace == null || launcher.Busy) return;
        Uri parsed; if (!Uri.TryCreate(url, UriKind.Absolute, out parsed) || (parsed.Scheme != "http" && parsed.Scheme != "https")) { launcher.SetError("请输入有效的 http 或 https 地址。"); return; }
        launcher.SetBusy("正在启动 " + parsed.Host + "...");
        await launchLock.WaitAsync();
        IntPtr handle;
        try { if (launcher.IsDisposed || workspace.IsDisposed) return; handle = await LaunchBrowserAsync(processName, parsed.AbsoluteUri); }
        finally { launchLock.Release(); }
        if (launcher.IsDisposed || workspace.IsDisposed) { if (handle != IntPtr.Zero) { NativeMethods.CloakWindow(handle, true); NativeMethods.ShowWindow(handle, NativeMethods.SW_HIDE); NativeMethods.PostMessage(handle, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero); } return; }
        if (handle == IntPtr.Zero) { launcher.SetError("启动失败，请确认浏览器已安装后重试。"); return; }
        var tile = new BrowserTile(handle, processName, parsed.AbsoluteUri); workspace.Tiles.Add(tile); workspace.Launchers.Remove(launcher); workspace.ReplacePane(launcher, tile); launcher.Dispose(); workspace.ActivePane = tile; AddFavorite(parsed.Host, parsed.AbsoluteUri);
    }
    private async Task<IntPtr> LaunchBrowserAsync(string processName, string url)
    {
        launchBaseline = new HashSet<IntPtr>(EnumerateBrowserWindows(processName)); launchingProcess = processName; capturedBrowser = IntPtr.Zero;
        var mounted = false;
        try
        {
            try { Process.Start(new ProcessStartInfo { FileName = BrowserPath(processName), Arguments = "--app=\"" + url.Replace("\"", "") + "\" --window-position=-32000,-32000 --window-size=800,600", UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden }); } catch { return IntPtr.Zero; }
            for (var attempt = 0; attempt < 750; attempt++)
            {
                if (capturedBrowser == IntPtr.Zero && attempt % 5 == 0) capturedBrowser = EnumerateBrowserWindows(processName).FirstOrDefault(candidate => !launchBaseline.Contains(candidate));
                if (capturedBrowser != IntPtr.Zero && NativeMethods.IsWindow(capturedBrowser)) { NativeMethods.CloakWindow(capturedBrowser, true); NativeMethods.ShowWindow(capturedBrowser, NativeMethods.SW_HIDE); mounted = true; return capturedBrowser; }
                await Task.Delay(20);
            }
            return IntPtr.Zero;
        }
        finally
        {
            launchBaseline = null; launchingProcess = null;
            if (!mounted && capturedBrowser != IntPtr.Zero) { NativeMethods.CloakWindow(capturedBrowser, false); NativeMethods.ShowWindow(capturedBrowser, NativeMethods.SW_SHOW); }
        }
    }
    private void HandleBrowserWindowEvent(IntPtr hook, uint eventId, IntPtr window, int objectId, int childId, uint thread, uint time)
    {
        if (eventId == 0x8005)
        {
            if (broadcasting) return;
            foreach (var workspace in workspaces)
            {
                var pane = workspace.Tiles.FirstOrDefault(item => item.OwnsFocus(window));
                if (pane != null) { workspace.ActivePane = pane; return; }
            }
            return;
        }
        if (launchBaseline == null || window == IntPtr.Zero || objectId != 0 || childId != 0 || (eventId != 0x8000 && eventId != 0x8002) || launchBaseline.Contains(window) || (capturedBrowser != IntPtr.Zero && capturedBrowser != window)) return;
        var windowClass = new StringBuilder(128); NativeMethods.GetClassName(window, windowClass, windowClass.Capacity);
        if (windowClass.ToString() != "Chrome_WidgetWin_1" || NativeMethods.GetAncestor(window, 2) != window) return;
        uint process; NativeMethods.GetWindowThreadProcessId(window, out process);
        try { if (!Process.GetProcessById((int)process).ProcessName.Equals(launchingProcess, StringComparison.OrdinalIgnoreCase)) return; } catch { return; }
        capturedBrowser = window; NativeMethods.CloakWindow(window, true); NativeMethods.ShowWindow(window, NativeMethods.SW_HIDE);
    }
    private void RemoveTile(BrowserTile tile)
    {
        var workspace = workspaces.FirstOrDefault(item => item.Tiles.Contains(tile)); if (workspace == null) return; tile.CloseBrowser(); workspace.Tiles.Remove(tile); workspace.RemovePane(tile); tile.Dispose(); if (workspace.Panes.Count == 0 && workspace == selectedWorkspace) AddLauncher(null);
    }
    private void RemoveClosedBrowsers()
    {
        foreach (var workspace in workspaces.ToArray())
        {
            foreach (var tile in workspace.Tiles.Where(item => !item.BrowserAlive).ToArray())
            {
                workspace.Tiles.Remove(tile); workspace.RemovePane(tile); tile.Dispose();
            }
            if (workspace.Panes.Count == 0) AddWorkspaceLauncher(workspace, "msedge", null);
        }
    }
    private static string BrowserPath(string processName)
    {
        if (processName == "chrome") { var chromePaths = new[] { Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe") }; return chromePaths.FirstOrDefault(File.Exists) ?? "chrome.exe"; }
        var edgePaths = new[] { Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe"), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe") }; return edgePaths.FirstOrDefault(File.Exists) ?? "msedge.exe";
    }
    private static List<IntPtr> EnumerateBrowserWindows(string processName)
    {
        var result = new List<IntPtr>(); NativeMethods.EnumWindows(delegate(IntPtr handle, IntPtr state) { var cls = new StringBuilder(128); NativeMethods.GetClassName(handle, cls, cls.Capacity); if (cls.ToString() != "Chrome_WidgetWin_1") return true; uint pid; NativeMethods.GetWindowThreadProcessId(handle, out pid); try { if (Process.GetProcessById((int)pid).ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase)) result.Add(handle); } catch { } return true; }, IntPtr.Zero); return result;
    }
    private void LoadFavorites()
    {
        favoriteItems.Add(new FavoriteItem { Name = "ChatGPT", Url = "https://chatgpt.com/" }); favoriteItems.Add(new FavoriteItem { Name = "DeepSeek", Url = "https://chat.deepseek.com/" }); favoriteItems.Add(new FavoriteItem { Name = "Claude", Url = "https://claude.ai/" }); favoriteItems.Add(new FavoriteItem { Name = "百度", Url = "https://www.baidu.com/" });
        try { if (!File.Exists(favoritesFile)) return; foreach (var line in File.ReadAllLines(favoritesFile)) { var split = line.IndexOf('|'); if (split > 0) AddFavoriteMemory(line.Substring(0, split), line.Substring(split + 1)); } } catch { }
    }
    private void RenameWorkspace(Workspace workspace)
    {
        if (!workspaces.Contains(workspace)) return;
        using (var dialog = new Form { Text = "重命名标签页", ClientSize = new Size(360, 104), StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MinimizeBox = false, MaximizeBox = false })
        {
            var input = new TextBox { Text = workspace.Text, MaxLength = 80, Bounds = new Rectangle(16, 16, 328, 24) };
            var accept = new Button { Text = "确定", Bounds = new Rectangle(184, 60, 76, 28), DialogResult = DialogResult.OK };
            var cancel = new Button { Text = "取消", Bounds = new Rectangle(268, 60, 76, 28), DialogResult = DialogResult.Cancel };
            dialog.Controls.Add(input); dialog.Controls.Add(accept); dialog.Controls.Add(cancel); dialog.AcceptButton = accept; dialog.CancelButton = cancel;
            dialog.Shown += delegate { input.Focus(); input.SelectAll(); };
            if (dialog.ShowDialog(this) == DialogResult.OK) SetWorkspaceName(workspace, input.Text);
        }
    }
    private void SetWorkspaceName(Workspace workspace, string name)
    {
        if (!workspaces.Contains(workspace) || String.IsNullOrWhiteSpace(name)) return;
        workspace.Text = name.Trim(); RefreshTabButtons();
    }
    private void LoadWorkspaceFavorites()
    {
        if (!File.Exists(workspaceFavoritesFile)) return;
        try
        {
            var serializer = new XmlSerializer(typeof(List<WorkspaceBookmark>));
            using (var stream = File.OpenRead(workspaceFavoritesFile))
            {
                var bookmarks = (List<WorkspaceBookmark>)serializer.Deserialize(stream);
                if (bookmarks != null) workspaceFavorites.AddRange(bookmarks.Where(item => item != null && !String.IsNullOrWhiteSpace(item.Name) && item.Panes != null));
            }
        }
        catch (Exception error) { MessageBox.Show("无法读取标签页收藏：" + error.Message, "AgentDock", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
    private void SaveWorkspaceFavorite(Workspace workspace)
    {
        if (!workspaces.Contains(workspace)) return;
        var bookmark = new WorkspaceBookmark { Id = workspace.FavoriteId ?? Guid.NewGuid().ToString("N"), Name = workspace.Text };
        foreach (var pane in workspace.Panes)
        {
            var browserPane = pane as BrowserTile;
            var launcher = pane as BrowserLauncherTile;
            if (browserPane != null && browserPane.BrowserAlive) bookmark.Panes.Add(new PaneBookmark { Browser = browserPane.BrowserKind, Url = browserPane.Url });
            else if (launcher != null) bookmark.Panes.Add(new PaneBookmark { Browser = launcher.BrowserKind, Url = launcher.Url, Launcher = true });
        }
        if (bookmark.Panes.Count == 0) bookmark.Panes.Add(new PaneBookmark { Browser = "msedge", Url = "https://chatgpt.com/", Launcher = true });
        var updated = workspaceFavorites.Where(item => item.Id != bookmark.Id).ToList(); updated.Add(bookmark);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(workspaceFavoritesFile));
            var temporary = workspaceFavoritesFile + ".tmp";
            using (var stream = File.Create(temporary)) new XmlSerializer(typeof(List<WorkspaceBookmark>)).Serialize(stream, updated);
            if (File.Exists(workspaceFavoritesFile)) File.Replace(temporary, workspaceFavoritesFile, null);
            else File.Move(temporary, workspaceFavoritesFile);
            workspace.FavoriteId = bookmark.Id; workspaceFavorites.Clear(); workspaceFavorites.AddRange(updated); RefreshFavorites();
        }
        catch (Exception error) { MessageBox.Show(this, "无法保存标签页收藏：" + error.Message, "AgentDock", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private void OpenWorkspaceFavorite(WorkspaceBookmark bookmark)
    {
        var workspace = CreateWorkspace(bookmark.Name); workspace.FavoriteId = bookmark.Id;
        foreach (var pane in bookmark.Panes)
        {
            if (pane == null) continue;
            var browserKind = pane.Browser == "chrome" ? "chrome" : "msedge";
            var launcher = AddWorkspaceLauncher(workspace, browserKind, pane.Url);
            if (!pane.Launcher) LauncherSubmitted(launcher, browserKind, pane.Url);
        }
        if (workspace.Panes.Count == 0) AddWorkspaceLauncher(workspace, "msedge", null);
    }
    private void AddFavorite(string name, string url) { if (favoriteItems.Any(item => item.Url.Equals(url, StringComparison.OrdinalIgnoreCase))) return; AddFavoriteMemory(name, url); SaveFavorites(); RefreshFavorites(); }
    private void AddFavoriteMemory(string name, string url) { if (!favoriteItems.Any(item => item.Url.Equals(url, StringComparison.OrdinalIgnoreCase))) favoriteItems.Add(new FavoriteItem { Name = name, Url = url }); }
    private void SaveFavorites() { try { Directory.CreateDirectory(Path.GetDirectoryName(favoritesFile)); File.WriteAllLines(favoritesFile, favoriteItems.Select(item => item.Name.Replace("|", "") + "|" + item.Url.Replace("|", "")).ToArray()); } catch { } }
    private void RefreshFavorites()
    {
        if (favorites.IsDisposed) return;
        foreach (ToolStripItem item in favorites.Items.Cast<ToolStripItem>().ToArray()) item.Dispose();
        favorites.Items.Add("横向分屏", null, delegate { AddLauncher(null); });
        favorites.Items.Add(new ToolStripSeparator());
        var savedTabs = new ToolStripMenuItem("已收藏标签页");
        foreach (var bookmark in workspaceFavorites) { var item = bookmark; savedTabs.DropDownItems.Add(item.Name, null, delegate { OpenWorkspaceFavorite(item); }); }
        savedTabs.Enabled = workspaceFavorites.Count > 0; favorites.Items.Add(savedTabs);
        favorites.Items.Add(new ToolStripSeparator());
        var commandPalette = MakeTabMenuItem("命令面板", "\uE945", delegate { ShowCommandPalette(); }); commandPalette.ShortcutKeys = Keys.Control | Keys.Shift | Keys.P; commandPalette.ShowShortcutKeys = true; favorites.Items.Add(commandPalette);
        favorites.Items.Add(new ToolStripSeparator());
        foreach (var favorite in favoriteItems) { var item = favorite; favorites.Items.Add(item.Name, null, delegate { OpenFavorite(item); }); }
    }
    private void LoadSettings()
    {
        try
        {
            if (!File.Exists(settingsFile)) return;
            var values = File.ReadAllLines(settingsFile).Where(line => !String.IsNullOrWhiteSpace(line)).ToArray();
            var parsed = (Keys)int.Parse(values[0].Trim()); var key = parsed & Keys.KeyCode; var modifiers = parsed & Keys.Modifiers;
            if (key != Keys.None && modifiers != Keys.None) splitShortcut = modifiers | (key == Keys.Add ? Keys.Oemplus : key);
            if (values.Length > 1)
            {
                parsed = (Keys)int.Parse(values[1].Trim()); key = parsed & Keys.KeyCode; modifiers = parsed & Keys.Modifiers;
                if (key != Keys.None && modifiers != Keys.None) broadcastShortcut = modifiers | (key == Keys.Add ? Keys.Oemplus : key);
            }
        }
        catch { }
    }
    private void SaveShortcutSettings()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(settingsFile));
            File.WriteAllLines(settingsFile, new[] { ((int)splitShortcut).ToString(), ((int)broadcastShortcut).ToString() });
        }
        catch (Exception error) { MessageBox.Show(this, "无法保存快捷键设置：" + error.Message, "AgentDock", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }
    private void ShowShortcutSettings()
    {
        using (var dialog = new ShortcutSettingsDialog(splitShortcut))
        {
            if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Shortcut == Keys.None) return;
            splitShortcut = dialog.Shortcut;
            SaveShortcutSettings();
            splitKeyHeld = false;
            RefreshFavorites();
        }
    }
    private void ShowCommandPalette()
    {
        var commands = new[]
        {
            Tuple.Create("向所有 Agent Chat 发送消息", "将消息发送到当前标签页中的每个 Agent Chat", broadcastShortcut == Keys.None ? "" : ShortcutSettingsDialog.FormatShortcut(broadcastShortcut), (Action)ShowBroadcastDialog),
            Tuple.Create("新增横向窗格", "添加一个横向窗格", ShortcutSettingsDialog.FormatShortcut(splitShortcut), (Action)delegate { AddLauncher(null); }),
        };
        Action selectedCommand = null;
        using (var dialog = new CommandPaletteDialog(commands, ApplyCommandShortcut))
        {
            activeCommandPalette = dialog;
            try { dialog.ShowDialog(this); selectedCommand = dialog.SelectedCommand; } finally { activeCommandPalette = null; }
        }
        if (selectedCommand != null) selectedCommand();
    }
    private void ApplyCommandShortcut(string commandName, Keys shortcut)
    {
        if (commandName == "向所有 Agent Chat 发送消息") broadcastShortcut = shortcut;
        else if (commandName == "新增横向窗格") splitShortcut = shortcut;
        else return;
        broadcastKeyHeld = false; splitKeyHeld = false; SaveShortcutSettings(); RefreshFavorites();
    }
    private void ShowBroadcastDialog()
    {
        var workspace = CurrentWorkspace(); var targetCount = workspace == null ? 0 : workspace.Tiles.Count(item => item.BrowserAlive);
        using (var dialog = new BroadcastMessageDialog(targetCount))
        {
            if (dialog.ShowDialog(this) == DialogResult.OK && !String.IsNullOrWhiteSpace(dialog.MessageText)) BroadcastToCurrentWorkspace(dialog.MessageText);
        }
    }
    private void BroadcastToCurrentWorkspace(string text)
    {
        var workspace = CurrentWorkspace(); if (workspace == null) return;
        var tiles = workspace.Tiles.Where(item => item.BrowserAlive).ToArray(); if (tiles.Length == 0) return;
        var failed = new List<string>();
        var previousPane = workspace.ActivePane;
        broadcasting = true;
        try
        {
            foreach (var tile in tiles)
            {
                workspace.ActivePane = tile;
                NativeMethods.SetForegroundWindow(Handle); Application.DoEvents();
                if (!tile.SendChatMessage(text)) failed.Add(tile.Url);
            }
        }
        finally
        {
            broadcasting = false;
            workspace.ActivePane = previousPane;
        }
        if (failed.Count > 0) MessageBox.Show(this, "以下 Agent Chat 未找到可确认的输入框，未发送：\n" + String.Join("\n", failed), "AgentDock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
    private void OpenFavorite(FavoriteItem item) { var workspace = CurrentWorkspace(); if (workspace != null && workspace.Panes.Count == 1 && workspace.Launchers.Count == 1 && !workspace.Launchers[0].Busy) workspace.Launchers[0].SetUrl(item.Url); else AddLauncher(item.Url); }
    private void CancelLauncher(BrowserLauncherTile launcher) { var workspace = workspaces.FirstOrDefault(item => item.Launchers.Contains(launcher)); if (workspace == null || launcher.Busy) return; workspace.Launchers.Remove(launcher); workspace.RemovePane(launcher); launcher.Dispose(); if (workspace.Panes.Count == 0 && workspace == selectedWorkspace) AddLauncher(null); }
    private void ClosePane(Workspace workspace)
    {
        if (workspace == null || !workspaces.Contains(workspace) || workspace.Panes.Count == 0) return;
        var pane = workspace.Panes.Contains(workspace.ActivePane) ? workspace.ActivePane : workspace.Panes.Last();
        var launcher = pane as BrowserLauncherTile;
        if (launcher != null && launcher.Busy) return;
        if (workspace.Panes.Count == 1) { CloseWorkspace(workspace); return; }
        var tile = pane as BrowserTile; if (tile != null) RemoveTile(tile); else CancelLauncher(launcher);
    }
    private void CloseLastPane() { ClosePane(CurrentWorkspace()); }
    private Button MakeCaption(string symbol, string tooltip) { var button = MakeButton(symbol); SetIcon(button, 10); button.Width = 46; button.Height = 32; tooltips.SetToolTip(button, tooltip); button.AccessibleName = tooltip; return button; }
    private void ToggleMaximize() { WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized; }
    private void DragHeader(object sender, MouseEventArgs e) { if (e.Button != MouseButtons.Left) return; NativeMethods.ReleaseCapture(); NativeMethods.SendMessage(Handle, 0x00A1, new IntPtr(2), IntPtr.Zero); }
    protected override void OnHandleCreated(EventArgs args)
    {
        base.OnHandleCreated(args);
        keyboardCallback = HandleSplitShortcut;
        keyboardHook = NativeMethods.SetWindowsHookEx(13, keyboardCallback, NativeMethods.GetModuleHandle(null), 0);
        mouseCallback = HandlePaneClick;
        mouseHook = NativeMethods.SetWindowsHookEx(14, mouseCallback, NativeMethods.GetModuleHandle(null), 0);
        browserEventCallback = HandleBrowserWindowEvent;
        browserEventHook = NativeMethods.SetWinEventHook(0x8000, 0x8005, IntPtr.Zero, browserEventCallback, 0, 0, 2);
        if (keyboardHook == IntPtr.Zero) Trace.WriteLine("Split shortcut hook failed: " + Marshal.GetLastWin32Error());
    }
    protected override void OnHandleDestroyed(EventArgs args)
    {
        if (keyboardHook != IntPtr.Zero) { NativeMethods.UnhookWindowsHookEx(keyboardHook); keyboardHook = IntPtr.Zero; }
        if (mouseHook != IntPtr.Zero) { NativeMethods.UnhookWindowsHookEx(mouseHook); mouseHook = IntPtr.Zero; }
        if (browserEventHook != IntPtr.Zero) { NativeMethods.UnhookWinEvent(browserEventHook); browserEventHook = IntPtr.Zero; }
        splitKeyHeld = false; commandKeyHeld = false; broadcastKeyHeld = false; hookControlDown = false; hookShiftDown = false; hookAltDown = false; browserInputArmed = false; base.OnHandleDestroyed(args);
    }
    private IntPtr HandlePaneClick(int code, IntPtr message, IntPtr data)
    {
        if (code >= 0 && (message.ToInt64() == 0x0201 || message.ToInt64() == 0x0204))
        {
            var outsidePosition = (NativeMethods.NativePoint)Marshal.PtrToStructure(data, typeof(NativeMethods.NativePoint));
            CloseOutsideOverlays(new Point(outsidePosition.X, outsidePosition.Y));
        }
        if (code >= 0 && IsHandleCreated && selectedWorkspace != null && (message.ToInt64() == 0x0201 || message.ToInt64() == 0x0202 || message.ToInt64() == 0x0204 || message.ToInt64() == 0x0205))
        {
            var position = (NativeMethods.NativePoint)Marshal.PtrToStructure(data, typeof(NativeMethods.NativePoint));
            if (NativeMethods.GetAncestor(NativeMethods.WindowFromPoint(position), 2) == Handle)
            {
                var point = selectedWorkspace.Canvas.PointToClient(new Point(position.X, position.Y));
                var pane = selectedWorkspace.Panes.FirstOrDefault(item => item.Bounds.Contains(point));
                if (pane != null)
                {
                    var browserPane = pane as BrowserTile;
                    if (browserPane != null && (message.ToInt64() == 0x0201 || message.ToInt64() == 0x0202))
                    {
                        var clickedWorkspace = selectedWorkspace;
                        BeginInvoke((Action)delegate
                        {
                            if (browserPane.IsDisposed || clickedWorkspace.IsDisposed || selectedWorkspace != clickedWorkspace) return;
                            browserInputArmed = true;
                            if (message.ToInt64() == 0x0201 && NativeMethods.GetForegroundWindow() != Handle)
                            {
                                var previousBrowser = clickedWorkspace.ActivePane as BrowserTile;
                                if (previousBrowser != null) previousBrowser.ReleaseInputQueue();
                                NativeMethods.SetForegroundWindow(Handle);
                            }
                            clickedWorkspace.ActivePane = pane;
                            browserPane.ForwardMouseMessage(message, position);
                            NativeMethods.SetForegroundWindow(Handle);
                            browserPane.ActivateInputQueue();
                        });
                        return new IntPtr(1);
                    }
                    if (message.ToInt64() == 0x0201 || message.ToInt64() == 0x0204) browserInputArmed = false;
                    selectedWorkspace.ActivePane = pane;
                }
                else if (message.ToInt64() == 0x0201 || message.ToInt64() == 0x0204) browserInputArmed = false;
            }
        }
        return NativeMethods.CallNextHookEx(mouseHook, code, message, data);
    }
    private IntPtr HandleSplitShortcut(int code, IntPtr message, IntPtr data)
    {
        if (code >= 0)
        {
            if (broadcasting) return NativeMethods.CallNextHookEx(keyboardHook, code, message, data);
            var key = Marshal.ReadInt32(data);
            var eventType = message.ToInt64();
            var keyDown = eventType == 0x0100 || eventType == 0x0104;
            var keyUp = eventType == 0x0101 || eventType == 0x0105;
            if (IsControlKey(key)) hookControlDown = keyDown;
            if (IsShiftKey(key)) hookShiftDown = keyDown;
            if (IsAltKey(key)) hookAltDown = keyDown;
            if (activeCommandPalette != null && !activeCommandPalette.IsDisposed)
            {
                if (key == (int)Keys.P && keyUp) commandKeyHeld = false;
                if (key == (int)Keys.Return && keyUp) broadcastKeyHeld = false;
                return NativeMethods.CallNextHookEx(keyboardHook, code, message, data);
            }
            if (key == (int)Keys.P && keyDown && ModifierDownFromHook(Keys.Control | Keys.Shift) && IsAgentDockForeground())
            {
                if (!commandKeyHeld) NativeMethods.PostMessage(Handle, CommandPaletteMessage, IntPtr.Zero, IntPtr.Zero);
                commandKeyHeld = true; return new IntPtr(1);
            }
            if (key == (int)Keys.P && keyUp && commandKeyHeld) { commandKeyHeld = false; return new IntPtr(1); }
            var configuredBroadcastKey = (int)(broadcastShortcut & Keys.KeyCode);
            if (broadcastShortcut != Keys.None && (key == configuredBroadcastKey || (configuredBroadcastKey == (int)Keys.Oemplus && key == (int)Keys.Add)) && keyDown && ModifierDownFromHook(broadcastShortcut) && IsAgentDockForeground())
            {
                if (!broadcastKeyHeld) NativeMethods.PostMessage(Handle, BroadcastMessage, IntPtr.Zero, IntPtr.Zero);
                broadcastKeyHeld = true; return new IntPtr(1);
            }
            if (key == (int)Keys.P && eventType == 0x0101) commandKeyHeld = false;
            if (broadcastShortcut != Keys.None && (key == configuredBroadcastKey || (configuredBroadcastKey == (int)Keys.Oemplus && key == (int)Keys.Add)) && keyUp)
            {
                var wasHeld = broadcastKeyHeld; broadcastKeyHeld = false; if (wasHeld) return new IntPtr(1);
            }
            var configuredKey = (int)(splitShortcut & Keys.KeyCode);
            if (key == configuredKey || (configuredKey == (int)Keys.Oemplus && key == (int)Keys.Add))
            {
                if (keyUp)
                {
                    var wasHeld = splitKeyHeld; splitKeyHeld = false; if (wasHeld) return new IntPtr(1);
                }
                else if (keyDown && ModifierDownFromHook(splitShortcut) && IsAgentDockForeground())
                {
                    if (!splitKeyHeld) NativeMethods.PostMessage(Handle, SplitMessage, IntPtr.Zero, IntPtr.Zero);
                    splitKeyHeld = true; return new IntPtr(1);
                }
            }
            var activeBrowser = selectedWorkspace == null ? null : selectedWorkspace.ActivePane as BrowserTile;
            var gui = new NativeMethods.GuiThreadInfo { Size = Marshal.SizeOf(typeof(NativeMethods.GuiThreadInfo)) };
            var browserHasFocus = activeBrowser != null && NativeMethods.GetGUIThreadInfo(0, ref gui) && activeBrowser.OwnsFocus(gui.Focus);
            if ((browserHasFocus || browserInputArmed) && IsAgentDockForeground() && (eventType == 0x0100 || eventType == 0x0101 || eventType == 0x0104 || eventType == 0x0105))
            {
                activeBrowser.ForwardKeyboardMessage(message, data); return new IntPtr(1);
            }
        }
        return NativeMethods.CallNextHookEx(keyboardHook, code, message, data);
    }
    protected override void WndProc(ref Message message)
    {
        if (message.Msg == SplitMessage) { AddLauncher(null); return; }
        if (message.Msg == CommandPaletteMessage) { ShowCommandPalette(); return; }
        if (message.Msg == BroadcastMessage) { ShowBroadcastDialog(); return; }
        if (message.Msg == 0x0024 && message.LParam != IntPtr.Zero)
        {
            base.WndProc(ref message);
            var screen = Screen.FromHandle(Handle);
            var limits = (NativeMethods.MinMaxInfo)Marshal.PtrToStructure(message.LParam, typeof(NativeMethods.MinMaxInfo));
            limits.MaxPosition.X = screen.WorkingArea.Left - screen.Bounds.Left;
            limits.MaxPosition.Y = screen.WorkingArea.Top - screen.Bounds.Top;
            limits.MaxSize.X = screen.WorkingArea.Width;
            limits.MaxSize.Y = screen.WorkingArea.Height;
            Marshal.StructureToPtr(limits, message.LParam, false);
            message.Result = IntPtr.Zero;
            return;
        }
        if (message.Msg == 0x0084 && WindowState == FormWindowState.Normal)
        {
            var position = message.LParam.ToInt64(); var point = PointToClient(new Point((short)(position & 0xffff), (short)((position >> 16) & 0xffff)));
            var left = point.X < 6; var right = point.X >= ClientSize.Width - 6; var top = point.Y < 4; var bottom = point.Y >= ClientSize.Height - 6;
            var hit = top && left ? 13 : top && right ? 14 : bottom && left ? 16 : bottom && right ? 17 : left ? 10 : right ? 11 : top ? 12 : bottom ? 15 : 0;
            if (hit != 0) { message.Result = new IntPtr(hit); return; }
        }
        base.WndProc(ref message);
    }
    protected override bool ProcessCmdKey(ref Message message, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.Shift | Keys.P)) { ShowCommandPalette(); return true; }
        if (broadcastShortcut != Keys.None && keyData == broadcastShortcut) { ShowBroadcastDialog(); return true; }
        if (keyData == (Keys.Control | Keys.Shift | Keys.T)) { AddWorkspace("工作区 " + (workspaces.Count + 1)); return true; }
        if (keyData == (Keys.Control | Keys.Shift | Keys.D)) { AddLauncher(null); return true; }
        if ((keyData & Keys.KeyCode) == (splitShortcut & Keys.KeyCode) && (keyData & Keys.Modifiers) == (splitShortcut & Keys.Modifiers)) { AddLauncher(null); return true; }
        return base.ProcessCmdKey(ref message, keyData);
    }
    private static bool ModifierDown(Keys shortcut)
    {
        var modifiers = shortcut & Keys.Modifiers;
        if ((modifiers & Keys.Control) != Keys.None && NativeMethods.GetAsyncKeyState((int)Keys.ControlKey) >= 0) return false;
        if ((modifiers & Keys.Shift) != Keys.None && NativeMethods.GetAsyncKeyState((int)Keys.ShiftKey) >= 0) return false;
        if ((modifiers & Keys.Alt) != Keys.None && NativeMethods.GetAsyncKeyState((int)Keys.Menu) >= 0) return false;
        return true;
    }
    private bool ModifierDownFromHook(Keys shortcut)
    {
        var modifiers = shortcut & Keys.Modifiers;
        if ((modifiers & Keys.Control) != Keys.None && !hookControlDown) return false;
        if ((modifiers & Keys.Shift) != Keys.None && !hookShiftDown) return false;
        if ((modifiers & Keys.Alt) != Keys.None && !hookAltDown) return false;
        return true;
    }
    private static bool IsControlKey(int key) { return key == 0x11 || key == 0xA2 || key == 0xA3; }
    private static bool IsShiftKey(int key) { return key == 0x10 || key == 0xA0 || key == 0xA1; }
    private static bool IsAltKey(int key) { return key == 0x12 || key == 0xA4 || key == 0xA5; }
    private bool IsAgentDockForeground()
    {
        var foreground = NativeMethods.GetForegroundWindow();
        return foreground == Handle || NativeMethods.IsChild(Handle, foreground) || NativeMethods.GetAncestor(foreground, 2) == Handle || NativeMethods.GetAncestor(foreground, 3) == Handle;
    }
}

internal static class Program
{
    [STAThread] private static void Main() { Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false); Application.Run(new MainForm()); }
}
