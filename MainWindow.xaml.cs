using Microsoft.UI.Xaml;
using WinUIEx;

namespace WinUIExTransparent;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : WinUIEx.WindowEx
{
    public MainWindow()
    {
        InitializeComponent();

        var hwnd = this.GetWindowHandle();
        // Comment ToggleWindowStyle out and see compositor works again
        HwndExtensions.ToggleWindowStyle(hwnd, false, WindowStyle.TiledWindow);
        HwndExtensions.CenterOnScreen(hwnd, 1024, 768);
    }
}
