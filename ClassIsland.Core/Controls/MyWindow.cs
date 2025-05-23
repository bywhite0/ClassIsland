using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using ClassIsland.Shared;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Models.Theming;
using Adorner = System.Windows.Forms.Design.Behavior.Adorner;

namespace ClassIsland.Core.Controls;

/// <summary>
/// 通用窗口基类
/// </summary>
public class MyWindow : Window
{
    private bool _isAdornerAdded;

    /// <summary>
    /// 是否显示开源警告水印
    /// </summary>
    public static bool ShowOssWatermark { get; internal set; } = false;

    private IThemeService? ThemeService { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public MyWindow()
    {
        try
        {
            ThemeService = IAppHost.GetService<IThemeService>();
            ThemeService.ThemeUpdated += ThemeServiceOnThemeUpdated;
            IAppHost.GetService<IHangService>().AssumeHang();
        }
        catch
        {
            // ignored
        }

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateImmersiveDarkMode(ThemeService?.CurrentRealThemeMode ?? 1);

        if ((AppBase.Current.IsDevelopmentBuild || ShowOssWatermark) && Content is UIElement element &&
            !_isAdornerAdded)
        {
            var layer = AdornerLayer.GetAdornerLayer(element);
            layer?.Add(new DevelopmentBuildAdorner(element, AppBase.Current.IsDevelopmentBuild, ShowOssWatermark));
            _isAdornerAdded = true;
        }
    }

    private void ThemeServiceOnThemeUpdated(object? sender, ThemeUpdatedEventArgs e)
    {
        UpdateImmersiveDarkMode(e.RealThemeMode);
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        UpdateImmersiveDarkMode(ThemeService?.CurrentRealThemeMode ?? 1);
        Debug.WriteLine("rendered.");
    }

    private unsafe void UpdateImmersiveDarkMode(int mode)
    {
        var trueVal = 0x01;
        var falseVal = 0x00;
        var hWnd = (HWND)new WindowInteropHelper(this).Handle;
        var build = Environment.OSVersion.Version.Build;
        if (build < 17763) return;
        //Debug.WriteLine(build);

        if (mode == 0)
            DwmSetWindowAttribute(hWnd,
                DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
                &falseVal,
                (uint)Marshal.SizeOf(typeof(int)));
        else
            DwmSetWindowAttribute(hWnd,
                DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
                &trueVal,
                (uint)Marshal.SizeOf(typeof(int)));

        // 在Windows10系统上强制刷新标题栏
        if (build < 22000)
        {
            uint WM_NCACTIVATE = 0x0086;
            SendMessage(hWnd, WM_NCACTIVATE, new WPARAM((nuint)(!IsActive ? 1 : 0)), 0);
            SendMessage(hWnd, WM_NCACTIVATE, new WPARAM((nuint)(IsActive ? 1 : 0)), 0);
        }
    }
}