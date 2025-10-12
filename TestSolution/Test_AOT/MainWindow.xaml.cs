using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

using WinRT;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Test_AOT;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly OverlappedPresenter presenter;
    private readonly GCHandle thisGCHandle;
    private readonly HWND windowHandle;

    private int scaledMinWidth;
    private int scaledMinHeight;

    private const double cMinWidth = 450;
    private const double cMinHeight = 350;
    private const int cSubClassID = 0;

    public MainWindow()
    {
        InitializeComponent();

        // work around for https://github.com/microsoft/CsWinRT/issues/1930
        presenter = AppWindow.Presenter.As<OverlappedPresenter>();

        windowHandle = (HWND)WindowNative.GetWindowHandle(this);
        
        double scaleFactor = PInvoke.GetDpiForWindow(windowHandle) / 96.0;
        scaledMinWidth = Scale(cMinWidth, scaleFactor);
        scaledMinHeight = Scale(cMinHeight, scaleFactor);

        thisGCHandle = GCHandle.Alloc(this);

        unsafe
        {
            bool success = PInvoke.SetWindowSubclass(windowHandle, &NewSubWindowProc, cSubClassID, (nuint)GCHandle.ToIntPtr(thisGCHandle));
            Debug.Assert(success);
        }

        Closed += MainWindow_Closed;
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        unsafe
        {
            PInvoke.RemoveWindowSubclass(windowHandle, &NewSubWindowProc, cSubClassID);
        }

        thisGCHandle.Free();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static LRESULT NewSubWindowProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam, nuint uIdSubclass, nuint dwRefData)
    {
        GCHandle handle = GCHandle.FromIntPtr((nint)dwRefData);

        if (handle.Target is MainWindow window)
        {
            switch (uMsg)
            {
                case PInvoke.WM_GETMINMAXINFO:
                {
                    unsafe
                    {
                        MINMAXINFO* mPtr = (MINMAXINFO*)lParam.Value;
                        mPtr->ptMinTrackSize.X = window.scaledMinWidth;
                        mPtr->ptMinTrackSize.Y = window.scaledMinHeight;
                    }
                    break;
                }

                case PInvoke.WM_DPICHANGED:
                {
                    double scaleFactor = (wParam & 0xFFFF) / 96.0;
                    window.scaledMinWidth = Scale(cMinWidth, scaleFactor);
                    window.scaledMinHeight = Scale(cMinWidth, scaleFactor);
                    break;
                }
            }
        }

        return PInvoke.DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    private static int Scale(double source, double scaleFactor) => (int)Math.FusedMultiplyAdd(source, scaleFactor, 0.5);
}