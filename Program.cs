using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace NapWindower
{
    class Program
    {
        // Window Show States
        const int SW_HIDE = 0;
        const int SW_SHOWNORMAL = 1;
        const int SW_MINIMIZE = 2;
        const int SW_MAXIMIZE = 3;
        const int SW_RESTORE = 9;

        // Window Style Constants
        const int GWL_STYLE = -16;
        const int GWL_EXSTYLE = -20;
        
        // Window Styles - More comprehensive
        const uint WS_OVERLAPPED = 0x00000000;
        const uint WS_POPUP = 0x80000000;
        const uint WS_CHILD = 0x40000000;
        const uint WS_MINIMIZE = 0x20000000;
        const uint WS_VISIBLE = 0x10000000;
        const uint WS_DISABLED = 0x08000000;
        const uint WS_CLIPSIBLINGS = 0x04000000;
        const uint WS_CLIPCHILDREN = 0x02000000;
        const uint WS_MAXIMIZE = 0x01000000;
        const uint WS_CAPTION = 0x00C00000;
        const uint WS_BORDER = 0x00800000;
        const uint WS_DLGFRAME = 0x00400000;
        const uint WS_VSCROLL = 0x00200000;
        const uint WS_HSCROLL = 0x00100000;
        const uint WS_SYSMENU = 0x00080000;
        const uint WS_THICKFRAME = 0x00040000;
        const uint WS_GROUP = 0x00020000;
        const uint WS_TABSTOP = 0x00010000;
        const uint WS_MINIMIZEBOX = 0x00020000;
        const uint WS_MAXIMIZEBOX = 0x00010000;
        
        // Combined styles for a resizable window
        const uint WS_OVERLAPPEDWINDOW = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;
        const uint WS_SIZEBOX = WS_THICKFRAME; // For resizing
        
        // Extended Window Styles
        const uint WS_EX_TOPMOST = 0x00000008;
        const uint WS_EX_TOOLWINDOW = 0x00000080;
        const uint WS_EX_APPWINDOW = 0x00040000;

        // SetWindowPos flags
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOZORDER = 0x0004;
        const uint SWP_NOREDRAW = 0x0008;
        const uint SWP_NOACTIVATE = 0x0010;
        const uint SWP_FRAMECHANGED = 0x0020;
        const uint SWP_SHOWWINDOW = 0x0040;
        const uint SWP_HIDEWINDOW = 0x0080;
        const uint SWP_NOCOPYBITS = 0x0100;
        const uint SWP_NOOWNERZORDER = 0x0200;
        const uint SWP_NOSENDCHANGING = 0x0400;

        // Windows API Imports
        [DllImport("user32.dll")]
        static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

        [DllImport("user32.dll")]
        static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter, string? lpszClass, string? lpszWindow);

        [DllImport("user32.dll")]
        static extern uint SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        [DllImport("user32.dll")]
        static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private static volatile bool _keepMonitoring = true;
        private static volatile bool _windowingToggle = false; // Toggle state for windowing
        private static IntPtr _targetWindow = IntPtr.Zero;
        private static readonly object _lockObject = new object();

        // Configuration values with defaults
        private static int _bootDelayMs = 10000;        // 10 seconds default
        private static int _monitorIntervalMs = 250;    // 250ms default
        private static int _hotkeyIntervalMs = 50;      // 50ms default

        /// <summary>
        /// Loads configuration from NapWindower.conf file
        /// </summary>
        static void LoadConfiguration()
        {
            string configPath = "NapWindower.conf";
            
            try
            {
                if (File.Exists(configPath))
                {
                    Console.WriteLine($"Loading configuration from {configPath}...");
                    
                    string[] lines = File.ReadAllLines(configPath);
                    foreach (string line in lines)
                    {
                        // Skip comments and empty lines
                        string trimmed = line.Trim();
                        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                            continue;
                            
                        // Parse key=value pairs
                        int equalsIndex = trimmed.IndexOf('=');
                        if (equalsIndex > 0)
                        {
                            string key = trimmed.Substring(0, equalsIndex).Trim();
                            string value = trimmed.Substring(equalsIndex + 1).Trim();
                            
                            switch (key.ToLowerInvariant())
                            {
                                case "bootdelayms":
                                    if (int.TryParse(value, out int bootDelay) && bootDelay >= 0)
                                    {
                                        _bootDelayMs = bootDelay;
                                        Console.WriteLine($"  Boot delay: {_bootDelayMs}ms ({_bootDelayMs / 1000.0:F1}s)");
                                    }
                                    break;
                                    
                                case "monitorintervalms":
                                    if (int.TryParse(value, out int monitorInterval) && monitorInterval >= 50)
                                    {
                                        _monitorIntervalMs = monitorInterval;
                                        Console.WriteLine($"  Monitor interval: {_monitorIntervalMs}ms");
                                    }
                                    break;
                                    
                                case "hotkeyintervalms":
                                    if (int.TryParse(value, out int hotkeyInterval) && hotkeyInterval >= 10)
                                    {
                                        _hotkeyIntervalMs = hotkeyInterval;
                                        Console.WriteLine($"  Hotkey interval: {_hotkeyIntervalMs}ms");
                                    }
                                    break;
                            }
                        }
                    }
                    Console.WriteLine("Configuration loaded successfully");
                }
                else
                {
                    Console.WriteLine($"No config file found, creating {configPath} with defaults...");
                    CreateDefaultConfigFile(configPath);
                    Console.WriteLine("Default configuration created");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration: {ex.Message}");
                Console.WriteLine("Using default values...");
            }
            
            Console.WriteLine();
        }

        /// <summary>
        /// Creates a default configuration file
        /// </summary>
        static void CreateDefaultConfigFile(string configPath)
        {
            string defaultConfig = @"# NAP Windower Configuration File
            
BootDelayMs=10000

MonitorIntervalMs=250

HotkeyIntervalMs=50";

            File.WriteAllText(configPath, defaultConfig);
        }

        /// <summary>
        /// Checks if the window is currently in fullscreen/kiosk mode
        /// </summary>
        static bool IsWindowFullscreenOrKiosk(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd))
                return false;

            try
            {
                uint currentStyle = GetWindowLong(hwnd, GWL_STYLE);
                
                // Check for fullscreen/kiosk indicators
                bool hasCaption = (currentStyle & WS_CAPTION) == WS_CAPTION;
                bool hasThickFrame = (currentStyle & WS_THICKFRAME) == WS_THICKFRAME;
                bool hasSysMenu = (currentStyle & WS_SYSMENU) == WS_SYSMENU;
                bool isPopup = (currentStyle & WS_POPUP) == WS_POPUP;

                // If it's missing key windowed features, it's likely fullscreen/kiosk
                return !hasCaption || !hasThickFrame || !hasSysMenu || isPopup;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Applies windowed style to the target window
        /// </summary>
        static bool ApplyWindowedStyle(IntPtr hwnd, bool verbose = false)
        {
            if (hwnd == IntPtr.Zero || !IsWindow(hwnd))
                return false;

            try
            {
                // Get current styles
                uint currentStyle = GetWindowLong(hwnd, GWL_STYLE);
                uint currentExStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

                if (verbose)
                {
                    Console.WriteLine($"Applying windowed style (was 0x{currentStyle:X8})");
                }

                // Step 1: Fix extended style
                uint newExStyle = currentExStyle & ~WS_EX_TOPMOST & ~WS_EX_TOOLWINDOW;
                newExStyle |= WS_EX_APPWINDOW;
                SetWindowLong(hwnd, GWL_EXSTYLE, newExStyle);

                // Step 2: Apply comprehensive windowed style
                uint newStyle = WS_OVERLAPPEDWINDOW | WS_VISIBLE;
                newStyle &= ~WS_MINIMIZE & ~WS_MAXIMIZE;
                SetWindowLong(hwnd, GWL_STYLE, newStyle);

                // Step 3: Force frame recalculation and show
                SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, 
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED | SWP_SHOWWINDOW);

                // Step 4: Ensure it's in normal state
                ShowWindow(hwnd, SW_RESTORE);
                ShowWindow(hwnd, SW_SHOWNORMAL);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Background task that continuously monitors and enforces windowed mode based on toggle
        /// </summary>
        static async Task MonitorWindowAsync(IntPtr hwnd)
        {
            Console.WriteLine("Starting window monitoring...");
            int reapplyCount = 0;
            bool wasToggleOn = false;
            
            while (_keepMonitoring)
            {
                try
                {
                    lock (_lockObject)
                    {
                        if (!IsWindow(hwnd))
                        {
                            Console.WriteLine("target window is not there");
                            break;
                        }

                        // Check if toggle state changed
                        if (_windowingToggle != wasToggleOn)
                        {
                            wasToggleOn = _windowingToggle;
                            if (_windowingToggle)
                            {
                                Console.WriteLine("windowing toggle ENABLED");
                                ApplyWindowedStyle(hwnd, true);
                                reapplyCount++;
                            }
                            else
                            {
                                Console.WriteLine("windowing toggle DISABLED");
                            }
                        }

                        // Only check and enforce if toggle is ON
                        if (_windowingToggle && IsWindowVisible(hwnd))
                        {
                            // Check if window has been switched back to fullscreen/kiosk
                            if (IsWindowFullscreenOrKiosk(hwnd))
                            {
                                Console.WriteLine("detected fullscreen/kiosk mode - Re-applying windowed style...");
                                ApplyWindowedStyle(hwnd, false);
                                reapplyCount++;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Monitor error: {ex.Message}");
                }

                // Check every configurable interval for faster response
                await Task.Delay(_monitorIntervalMs);
            }
            
            Console.WriteLine($"Monitoring stopped. Total windowing applications: {reapplyCount}");
        }

        /// <summary>
        /// Monitors for Alt+Q hotkey to toggle windowing mode
        /// </summary>
        static async Task HotkeyMonitorAsync()
        {
            Console.WriteLine("Starting hotkey monitor...");
            bool altPressed = false;
            bool qPressed = false;

            while (_keepMonitoring)
            {
                try
                {
                    // Check Alt key state
                    bool currentAltPressed = (GetAsyncKeyState(0x12) & 0x8000) != 0; // VK_MENU (Alt)
                    bool currentQPressed = (GetAsyncKeyState(0x51) & 0x8000) != 0;   // VK_Q

                    // Detect Alt+Q press (both keys pressed and at least one is newly pressed)
                    if (currentAltPressed && currentQPressed && (!altPressed || !qPressed))
                    {
                        // Toggle the windowing state
                        _windowingToggle = !_windowingToggle;
                        
                        string status = _windowingToggle ? "ENABLED" : "DISABLED";
                        Console.WriteLine($"[Alt+Q] Windowing toggle: {status}");
                        
                        // Wait for keys to be released to avoid rapid toggling
                        while ((GetAsyncKeyState(0x12) & 0x8000) != 0 || 
                               (GetAsyncKeyState(0x51) & 0x8000) != 0)
                        {
                            await Task.Delay(50);
                        }
                    }

                    altPressed = currentAltPressed;
                    qPressed = currentQPressed;
                    
                    await Task.Delay(_hotkeyIntervalMs); // Use configurable interval for responsive hotkeys
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Hotkey monitor error: {ex.Message}");
                    await Task.Delay(1000);
                }
            }
        }

        /// <summary>
        /// Gets the window title for debugging
        /// </summary>
        static string GetWindowTitle(IntPtr hwnd)
        {
            try
            {
                int length = GetWindowTextLength(hwnd);
                if (length == 0) return "";
                
                var builder = new System.Text.StringBuilder(length + 1);
                GetWindowText(hwnd, builder, builder.Capacity);
                return builder.ToString();
            }
            catch
            {
                return "";
            }
        }

        static async Task Main(string[] args)
        {
            Console.WriteLine("NAP Locked Down Browser Windower");
            Console.WriteLine("=================================");
            Console.WriteLine();

            // Load configuration first
            LoadConfiguration();

            // Launch the NAP Locked Down Browser
            Console.WriteLine("rubbing the guacamole all over NAP Locked Down Browser...");
            Process? napProcess = null;
            try
            {
                napProcess = Process.Start("NAP Locked down browser.exe");
                Console.WriteLine("application launched successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to launch application: {ex.Message}");
                Console.WriteLine("put this in the same directory as 'NAP Locked down browser.exe'");
                Console.WriteLine("\nPress any key to exit right meow...");
                Console.ReadKey();
                return;
            }

            // Wait for NAP to fully boot up (configurable delay)
            if (_bootDelayMs > 0)
            {
                Console.WriteLine($"waiting for NAP to fully boot up ({_bootDelayMs / 1000.0:F1} seconds)...");
                int remainingMs = _bootDelayMs;
                while (remainingMs > 0)
                {
                    int secondsRemaining = (remainingMs + 999) / 1000; // Round up
                    Console.Write($"\rbooting... {secondsRemaining} seconds remaining");
                    
                    int delayThisLoop = Math.Min(1000, remainingMs);
                    await Task.Delay(delayThisLoop);
                    remainingMs -= delayThisLoop;
                }
                Console.WriteLine("\rBoot wait complete                    ");
            }
            else
            {
                Console.WriteLine("Boot delay disabled - proceeding immediately");
            }

            // Try multiple window finding strategies
            IntPtr hwnd = IntPtr.Zero;
            string[] possibleTitles = {
                "NAP Locked Down Browser",
                "NAP Browser", 
                "Locked Down Browser",
                "NAP"
            };

            Console.WriteLine("Searching for NAP window...");
            foreach (string title in possibleTitles)
            {
                hwnd = FindWindow(null, title);
                if (hwnd != IntPtr.Zero)
                {
                    Console.WriteLine($"Found window with title: '{title}'");
                    break;
                }
                Console.WriteLine($"  - Trying title: '{title}' - Not found");
            }

            // If still not found, try finding by process
            if (hwnd == IntPtr.Zero && napProcess != null)
            {
                Console.WriteLine("Trying to find window by process...");
                napProcess.Refresh();
                if (napProcess.MainWindowHandle != IntPtr.Zero)
                {
                    hwnd = napProcess.MainWindowHandle;
                    Console.WriteLine("Found window via process MainWindowHandle");
                }
            }

            if (hwnd != IntPtr.Zero && IsWindow(hwnd))
            {
                _targetWindow = hwnd;
                string windowTitle = GetWindowTitle(hwnd);
                Console.WriteLine($"Target window found: '{windowTitle}'");
                
                // Get current window state for debugging
                uint currentStyle = GetWindowLong(hwnd, GWL_STYLE);
                Console.WriteLine($"Initial window style: 0x{currentStyle:X8}");
                
                // Check initial state and apply windowing immediately
                if (IsWindowFullscreenOrKiosk(hwnd))
                {
                    Console.WriteLine("mom the window is in fullscreen/kiosk mode again");
                    ApplyWindowedStyle(hwnd, true);
                    
                    // Set a reasonable size and position
                    GetWindowRect(hwnd, out RECT rect);
                    int width = Math.Max(800, rect.Right - rect.Left);
                    int height = Math.Max(600, rect.Bottom - rect.Top);
                    int screenWidth = System.Windows.Forms.Screen.PrimaryScreen?.Bounds.Width ?? 1920;
                    int screenHeight = System.Windows.Forms.Screen.PrimaryScreen?.Bounds.Height ?? 1080;
                    int x = (screenWidth - width) / 2;
                    int y = (screenHeight - height) / 2;

                    SetWindowPos(hwnd, IntPtr.Zero, x, y, width, height, 
                        SWP_NOZORDER | SWP_SHOWWINDOW);
                    Console.WriteLine($"Window repositioned to {x},{y} with size {width}x{height}");

                    // Bring to foreground
                    SetForegroundWindow(hwnd);
                    BringWindowToTop(hwnd);
                    
                    Console.WriteLine("Initial windowing complete!");
                }
                else
                {
                    Console.WriteLine("Window is already in windowed mode");
                }

                // Enable the toggle by default since we just windowed it
                _windowingToggle = true;

                Console.WriteLine();
                Console.WriteLine("the toggles or whatever");
                Console.WriteLine("=========================");
                Console.WriteLine("• Press Alt+Q to toggle windowing enforcement");
                Console.WriteLine("• current state: ENABLED (windowing active)");
                Console.WriteLine("• press 'Q' in this console to quit");
                Console.WriteLine("• edit NapWindower.conf to change variables");
                Console.WriteLine();

                // Start monitoring tasks
                var windowMonitorTask = MonitorWindowAsync(hwnd);
                var hotkeyMonitorTask = HotkeyMonitorAsync();

                Console.WriteLine("Monitoring started - NAP is now windowed and protected!");
                Console.WriteLine("   Press Alt+Q to toggle windowing on/off");
                Console.WriteLine("   Press 'Q' in this console to quit");
                Console.WriteLine();

                // Wait for user to quit
                while (_keepMonitoring)
                {
                    var key = Console.ReadKey(true);
                    if (key.KeyChar == 'q' || key.KeyChar == 'Q')
                    {
                        Console.WriteLine("shutting down like a portal thingy");
                        _keepMonitoring = false;
                        break;
                    }
                }

                // Wait for tasks to complete
                await Task.WhenAll(windowMonitorTask, hotkeyMonitorTask);
            }
            else
            {
                Console.WriteLine("Window not found!");
                Console.WriteLine("NAP might not be fully loaded yet or has a different window title.");
                
                if (napProcess != null && !napProcess.HasExited)
                {
                    Console.WriteLine($"process is running (PID: {napProcess.Id}) but window not accessible.");
                    Console.WriteLine("you can try running this tool again once NAP is fully loaded.");
                }

                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
        }
    }
}
