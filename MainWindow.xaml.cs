using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using WinUIEx;

namespace WinUIExTransparent;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : WinUIEx.WindowEx
{
    public MainWindow(int xpos, int ypos, int width, int height, SoftwareBitmapSource desktopImageSource)
    {
        InitializeComponent();

        var hwnd = this.GetWindowHandle();
        // Comment ToggleWindowStyle out and see compositor works again
        HwndExtensions.ToggleWindowStyle(hwnd, false, WindowStyle.TiledWindow);

        // Move and resize window to specified location and size
        AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(xpos, ypos, width, height), Microsoft.UI.Windowing.DisplayArea.Primary);

        // Set Background of Window to Bitmap of Desktop to faux transparency
        RootGrid.Background = new ImageBrush()
        {
            ImageSource = desktopImageSource,
        };
    }
}
