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
        var desktopImageSource = await CaptureDesktopAsync();

        _window = new MainWindow(WINDOW_WIDTH, WINDOW_HEIGHT, desktopImageSource);
        _window.Activate();
    }

    public static async Task<SoftwareBitmapSource> CaptureDesktopAsync()
    {
        // Get the device context of the entire screen
        var hScreenDC = PInvoke.GetDC(HWND.Null);
        var hMemoryDC = PInvoke.CreateCompatibleDC(hScreenDC);

        // Get the width and height of the screen
        int width = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXVIRTUALSCREEN);
        int height = PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYVIRTUALSCREEN);

        // Create a compatible bitmap
        var hBitmap = PInvoke.CreateCompatibleBitmap(hScreenDC, width, height);
        PInvoke.SelectObject(hMemoryDC, hBitmap);

        // Copy the screen into the bitmap
        PInvoke.BitBlt(
            hMemoryDC,
            0, 0, width, height,
            hScreenDC,
            0, 0,
            ROP_CODE.SRCCOPY
        );

        // Create a BitmapImage from the HBITMAP
        var softwareBitmapSource = await ConvertHBitmapToBitmapImage(hBitmap);

        // Cleanup
        PInvoke.DeleteDC(hMemoryDC);
        PInvoke.ReleaseDC(HWND.Null, hScreenDC);
        PInvoke.DeleteObject(hBitmap);

        return softwareBitmapSource;
    }

    public static async Task<SoftwareBitmapSource> ConvertHBitmapToBitmapImage(IntPtr hBitmap)
    {
        // Step 1: Convert HBITMAP to byte array
        var bitmap = Image.FromHbitmap(hBitmap);
        using var memoryStream = new MemoryStream();
        bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Bmp);
        byte[] imageBytes = memoryStream.ToArray();

        // Step 2: Create a SoftwareBitmap
        using var stream = new InMemoryRandomAccessStream();
        await stream.WriteAsync(imageBytes.AsBuffer());
        stream.Seek(0);

        var decoder = await BitmapDecoder.CreateAsync(stream);
        SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied
        );

        // Step 3: Wrap in SoftwareBitmapSource exposable to XAML
        var softwareBitmapSource = new SoftwareBitmapSource();
        await softwareBitmapSource.SetBitmapAsync(softwareBitmap);

        return softwareBitmapSource;
    }
}
