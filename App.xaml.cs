using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace WinUIExTransparent;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    // Size of window masking, XAML panel/effects needs to be contained within this area
    private static int WINDOW_WIDTH = 1024;
    private static int WINDOW_HEIGHT = 256;

    private Window? _window;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        // TODO: Not sure how DPI factors in here...
        // Get width of primary display and calculate our window x coordinate
        var xpos = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXSCREEN) / 2 - WINDOW_WIDTH / 2;
        var ypos = 0;

        // Capture slice of the desktop background
        var desktopImageSource = await CaptureDesktopAsync(xpos, ypos, WINDOW_WIDTH, WINDOW_HEIGHT);

        _window = new MainWindow(xpos, ypos, WINDOW_WIDTH, WINDOW_HEIGHT, desktopImageSource);
        _window.Activate();
    }

    private static async Task<SoftwareBitmapSource> CaptureDesktopAsync(int xpos, int ypos, int width, int height)
    {
        // Get the device context of the entire screen
        var hScreenDC = PInvoke.GetDC(HWND.Null);
        var hMemoryDC = PInvoke.CreateCompatibleDC(hScreenDC);

        // Create a compatible bitmap of the requested size (not the full screen)
        var hBitmap = PInvoke.CreateCompatibleBitmap(hScreenDC, width, height);
        var oldBitmap = PInvoke.SelectObject(hMemoryDC, hBitmap);

        // Copy the specified region of the screen into the bitmap
        PInvoke.BitBlt(
            hMemoryDC,
            0, 0, width, height, // destination pos and size (buffer)
            hScreenDC,
            xpos, ypos,          // source position (on screen)
            ROP_CODE.SRCCOPY
        );

        // Create a BitmapImage from the HBITMAP
        var softwareBitmapSource = await ConvertHBitmapToBitmapImage(hBitmap);

        // Cleanup
        PInvoke.SelectObject(hMemoryDC, oldBitmap);
        PInvoke.DeleteDC(hMemoryDC);
        PInvoke.ReleaseDC(HWND.Null, hScreenDC);
        PInvoke.DeleteObject(hBitmap);

        return softwareBitmapSource;
    }

    private static async Task<SoftwareBitmapSource> ConvertHBitmapToBitmapImage(HBITMAP hBitmap)
    {
        // Convert HBITMAP to byte array
        var bitmap = Image.FromHbitmap(hBitmap);
        using var memoryStream = new MemoryStream();
        bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Bmp);
        byte[] imageBytes = memoryStream.ToArray();

        // Create a SoftwareBitmap
        using var stream = new InMemoryRandomAccessStream();
        await stream.WriteAsync(imageBytes.AsBuffer());
        stream.Seek(0);

        // Ensure SoftwareBitmap is in the acceptable format for SoftwareBitmapSource
        var decoder = await BitmapDecoder.CreateAsync(stream);
        SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied
        );

        // Wrap in SoftwareBitmapSource exposable to XAML
        var softwareBitmapSource = new SoftwareBitmapSource();
        await softwareBitmapSource.SetBitmapAsync(softwareBitmap);

        return softwareBitmapSource;
    }
}
