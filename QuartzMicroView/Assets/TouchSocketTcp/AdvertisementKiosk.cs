using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;

/// <summary>
/// 全屏 置顶 防休眠 多屏幕
/// </summary>
public class AdvertisementKiosk : MonoBehaviour
{
    private const float checkInterval = 3f; // 每3秒检查一次
    private IntPtr hWnd;

#if UNITY_STANDALONE_WIN
    private const int SW_SHOW = 5;
    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll", EntryPoint = "EnumWindows", SetLastError = true)]
    private static extern bool EnumWindows(WNDENUMPROC lpEnumFunc, uint lParam);

    [DllImport("user32.dll", EntryPoint = "GetParent", SetLastError = true)]
    private static extern IntPtr GetParent(IntPtr hWnd);

    [DllImport("user32.dll", EntryPoint = "GetWindowThreadProcessId")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, ref uint lpdwProcessId);

    [DllImport("user32.dll", EntryPoint = "IsWindow")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
    private static extern void SetLastError(uint dwErrCode);

    [DllImport("user32.dll")]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    private delegate bool WNDENUMPROC(IntPtr hwnd, uint lParam);

    private static System.Collections.Hashtable processWnd = new System.Collections.Hashtable();

    private IntPtr GetCurrentWindowHandle()
    {
        IntPtr ptrWnd = IntPtr.Zero;
        uint uiPid = (uint)Process.GetCurrentProcess().Id;  // 当前进程 ID

        // 首先尝试通过窗口类名查找
        ptrWnd = FindWindow("UnityWndClass", null);
        if (ptrWnd != IntPtr.Zero)
        {
            UnityEngine.Debug.Log("找到unity窗口");
            UnityEngine.Debug.Log(ptrWnd);
            return ptrWnd;
        }

        object objWnd = processWnd[uiPid];
        if (objWnd != null)
        {
            ptrWnd = (IntPtr)objWnd;
            if (ptrWnd != IntPtr.Zero && IsWindow(ptrWnd))  // 从缓存中获取句柄
            {
                return ptrWnd;
            }
            else
            {
                ptrWnd = IntPtr.Zero;
            }
        }
        bool bResult = EnumWindows(new WNDENUMPROC(EnumWindowsProc), uiPid);
        // 枚举窗口返回 false 并且没有错误号时表明获取成功
        if (!bResult && Marshal.GetLastWin32Error() == 0)
        {
            objWnd = processWnd[uiPid];
            if (objWnd != null)
            {
                ptrWnd = (IntPtr)objWnd;
            }
        }
        return ptrWnd;
    }

    private static bool EnumWindowsProc(IntPtr hwnd, uint lParam)
    {
        uint uiPid = 0;
        if (GetParent(hwnd) == IntPtr.Zero)
        {
            GetWindowThreadProcessId(hwnd, ref uiPid);
            if (uiPid == lParam)    // 找到进程对应的主窗口句柄
            {
                processWnd[uiPid] = hwnd;   // 把句柄缓存起来
                SetLastError(0);    // 设置无错误
                return false;   // 返回 false 以终止枚举窗口
            }
        }
        return true;
    }

#endif

    private void Start()
    {
        // 设置全屏
        Screen.fullScreen = Settings.ini.Graphics.FullScreen;



        // 设置屏幕常亮
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        if (Settings.ini.Graphics.HideCursor)
        {
            // 隐藏鼠标
#if UNITY_EDITOR
            Cursor.visible = true;
#else
        Cursor.visible = false;
#endif
        }

        Application.runInBackground = true;
        //多屏
        for (int i = 0; i < Display.displays.Length; i++)
        {
            Display.displays[i].Activate();
            //Screen.SetResolution(Display.displays[i].renderingWidth, Display.displays[i].renderingHeight, true);
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (Settings.ini.Graphics.TopMost || File.Exists(Path.Combine(Application.streamingAssetsPath, "置顶.txt")))
        {
            StartCoroutine(GetWindowHandle());

            // 启动定期检查协程
            StartCoroutine(EnsureTopmostAndFullscreen());
        }

#endif
    }

#if UNITY_STANDALONE_WIN

    private IEnumerator GetWindowHandle()
    {
        yield return new WaitForSeconds(0.5f); // 等待短暂时间确保窗口已创建

        hWnd = GetCurrentWindowHandle();
        if (hWnd != IntPtr.Zero)
        {
            UnityEngine.Debug.Log("Got window handle: " + hWnd);
        }
        else
        {
            UnityEngine.Debug.LogError("Failed to get window handle.");
        }
    }

#endif

    private IEnumerator EnsureTopmostAndFullscreen()
    {
        WaitForSeconds wait = new WaitForSeconds(checkInterval);

        while (true)
        {
            SetTopmostAndFullscreen();
            yield return wait;
        }
    }

    private void SetTopmostAndFullscreen()
    {
        if (!Screen.fullScreen)
        {
            Screen.fullScreen = true;
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (hWnd != IntPtr.Zero)
        {
            ShowWindow(hWnd, SW_SHOW);
            SetForegroundWindow(hWnd);
            BringWindowToTop(hWnd);
            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
        }
#endif
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        SetTopmostAndFullscreen();
    }
}