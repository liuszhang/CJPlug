using System.Runtime.InteropServices;
using Serilog;

namespace CJ.Plug.StationApiServer.Services.Rfb
{
    /// <summary>
    /// RFB 输入注入器：把 RFB PointerEvent/KeyEvent 转换为 Windows SendInput 注入。
    /// 坐标映射：画布坐标 → 屏幕坐标（由 WindowCompositor.MapToScreen 提供）。
    /// </summary>
    public class InputInjector
    {
        #region Win32 P/Invoke

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private const uint INPUT_MOUSE = 0;
        private const uint INPUT_KEYBOARD = 1;
        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;
        private const uint MOUSEEVENTF_XDOWN = 0x0080;
        private const uint MOUSEEVENTF_XUP = 0x0100;
        private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_SCANCODE = 0x0008;
        private const uint XBUTTON1 = 0x0001;
        private const uint XBUTTON2 = 0x0002;

        [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(POINT point);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }

        #endregion

        private readonly WindowCompositor _compositor;

        public InputInjector(WindowCompositor compositor)
        {
            _compositor = compositor;
        }

        /// <summary>
        /// 注入鼠标事件（RFB PointerEvent）。
        /// </summary>
        /// <param name="buttonMask">RFB buttonMask：1=左, 2=中, 4=右, 8=滚轮上, 16=滚轮下, 32=X1, 64=X2</param>
        /// <param name="canvasX">画布坐标</param>
        /// <param name="canvasY">画布坐标</param>
        public void InjectPointer(int buttonMask, int canvasX, int canvasY)
        {
            try
            {
                var mapped = _compositor.MapToScreen(canvasX, canvasY);
                if (mapped == null) return;

                // 激活目标窗口（保证点击/键盘落在正确前台）
                var hwnd = WindowFromPoint(new POINT { X = mapped.Value.X, Y = mapped.Value.Y });
                if (hwnd != IntPtr.Zero && GetForegroundWindow() != hwnd)
                    SetForegroundWindow(hwnd);

                SetCursorPos(mapped.Value.X, mapped.Value.Y);

                // 发送按钮状态变化（差分：对比上次 mask）
                uint changed = (uint)buttonMask ^ _lastButtonMask;
                if (changed != 0)
                {
                    var events = new List<INPUT>();
                    for (int bit = 0; bit < 7; bit++)
                    {
                        uint mask = 1u << bit;
                        if ((changed & mask) == 0) continue;
                        bool down = (buttonMask & mask) != 0;
                        uint flag = mask switch
                        {
                            0x01 => down ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_LEFTUP,
                            0x02 => down ? MOUSEEVENTF_MIDDLEDOWN : MOUSEEVENTF_MIDDLEUP,
                            0x04 => down ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_RIGHTUP,
                            0x20 => down ? MOUSEEVENTF_XDOWN | XBUTTON1 : MOUSEEVENTF_XUP | XBUTTON1,
                            0x40 => down ? MOUSEEVENTF_XDOWN | XBUTTON2 : MOUSEEVENTF_XUP | XBUTTON2,
                            _ => 0
                        };
                        if (flag != 0)
                            events.Add(MakeMouseInput(flag, 0));
                    }
                    if (events.Count > 0)
                        SendInput((uint)events.Count, events.ToArray(), Marshal.SizeOf<INPUT>());
                }

                // 滚轮（8=上, 16=下，无按下状态，直接发事件）
                if ((buttonMask & 0x08) != 0 && (_lastButtonMask & 0x08) == 0)
                    SendWheel(120);
                if ((buttonMask & 0x10) != 0 && (_lastButtonMask & 0x10) == 0)
                    SendWheel(-120);

                _lastButtonMask = (uint)buttonMask & ~0x18u; // 滚轮位不计入状态
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "注入鼠标事件失败");
            }
        }

        private uint _lastButtonMask;

        private void SendWheel(int delta)
        {
            SendInput(1, [MakeMouseInput(MOUSEEVENTF_WHEEL, (uint)delta)], Marshal.SizeOf<INPUT>());
        }

        /// <summary>
        /// 注入键盘事件（RFB KeyEvent）。
        /// </summary>
        /// <param name="keysym">X11 keysym</param>
        /// <param name="down">按下/抬起</param>
        public void InjectKey(uint keysym, bool down)
        {
            try
            {
                var vk = KeysymToVk(keysym);
                if (vk == 0) return;

                var inputs = new List<INPUT>();
                // 扩展键（方向键、Insert/Delete 等）需加 KEYEVENTF_EXTENDEDKEY
                bool extended = (vk & 0x100) != 0;
                ushort vkCode = (ushort)(vk & 0xFF);

                uint flags = down ? 0u : KEYEVENTF_KEYUP;
                if (extended) flags |= KEYEVENTF_SCANCODE; // 用扫描码注入扩展键更可靠
                inputs.Add(new INPUT
                {
                    type = INPUT_KEYBOARD,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = vkCode,
                            wScan = (ushort)MapVirtualKey(vkCode, 0),
                            dwFlags = flags,
                            time = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                });

                if (inputs.Count > 0)
                    SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "注入键盘事件失败");
            }
        }

        [DllImport("user32.dll")] private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        private static INPUT MakeMouseInput(uint flags, uint mouseData)
        {
            return new INPUT
            {
                type = INPUT_MOUSE,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dx = 0, dy = 0,
                        mouseData = mouseData,
                        dwFlags = flags,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
        }

        /// <summary>
        /// X11 keysym → Windows 虚拟键码（含扩展键高位标记 0x100）。
        /// 覆盖常用 ASCII、功能键、方向键、编辑键。未知返回 0。
        /// </summary>
        private static uint KeysymToVk(uint keysym)
        {
            // ASCII 字母/数字/符号（keysym 0x20~0x7E 与 ASCII 一致，VK 大写映射）
            if (keysym >= 'a' && keysym <= 'z')
                return (uint)char.ToUpperInvariant((char)keysym);
            if (keysym >= '0' && keysym <= '9')
                return keysym;
            if (keysym >= 0x20 && keysym <= 0x7E)
            {
                return keysym switch
                {
                    ' ' => 0x20,
                    '-' => 0xBD, '=' => 0xBB, '[' => 0xDB, ']' => 0xDD, '\\' => 0xDC,
                    ';' => 0xBA, '\'' => 0xDE, ',' => 0xBC, '.' => 0xBE, '/' => 0xBF,
                    '`' => 0xC0,
                    _ => 0 // 需要 Shift 的符号键（!@#...）交由上层处理，此处不映射
                };
            }

            return keysym switch
            {
                // 功能键
                0xFFBE => 0x70, // F1
                0xFFBF => 0x71, // F2
                0xFFC0 => 0x72, // F3
                0xFFC1 => 0x73, // F4
                0xFFC2 => 0x74, // F5
                0xFFC3 => 0x75, // F6
                0xFFC4 => 0x76, // F7
                0xFFC5 => 0x77, // F8
                0xFFC6 => 0x78, // F9
                0xFFC7 => 0x79, // F10
                0xFFC8 => 0x7A, // F11
                0xFFC9 => 0x7B, // F12
                // 方向键（扩展）
                0xFF51 => 0x100 | 0x25, // Left
                0xFF52 => 0x100 | 0x26, // Up
                0xFF53 => 0x100 | 0x27, // Right
                0xFF54 => 0x100 | 0x28, // Down
                // 编辑键
                0xFF08 => 0x08, // BackSpace
                0xFF09 => 0x09, // Tab
                0xFF0D => 0x0D, // Return/Enter
                0xFF1B => 0x1B, // Escape
                0xFF63 => 0x2D, // Insert（扩展）
                0xFFFF => 0x2E, // Delete（扩展）
                0xFF50 => 0x24, // Home
                0xFF57 => 0x23, // End
                0xFF55 => 0x21, // PageUp
                0xFF56 => 0x22, // PageDown
                0xFFE1 => 0x10, // Shift_L
                0xFFE2 => 0x10, // Shift_R
                0xFFE3 => 0x11, // Control_L
                0xFFE4 => 0x11, // Control_R
                0xFFE7 => 0x12, // Meta_L (Alt)
                0xFFE8 => 0x12, // Meta_R (Alt)
                0xFFE9 => 0x12, // Alt_L
                0xFFEA => 0x12, // Alt_R
                0xFFEB => 0x5B, // Super_L (Win)
                0xFFEC => 0x5B, // Super_R (Win)
                0xFFE5 => 0x14, // CapsLock
                0xFFE6 => 0x14, // CapsLock (Caps)
                // 数字小键盘
                0xFFB0 => 0x60, // KP_0
                0xFFB1 => 0x61, // KP_1
                0xFFB2 => 0x62, // KP_2
                0xFFB3 => 0x63, // KP_3
                0xFFB4 => 0x64, // KP_4
                0xFFB5 => 0x65, // KP_5
                0xFFB6 => 0x66, // KP_6
                0xFFB7 => 0x67, // KP_7
                0xFFB8 => 0x68, // KP_8
                0xFFB9 => 0x69, // KP_9
                0xFFBD => 0x6E, // KP_Add
                0xFFAB => 0x6D, // KP_Subtract
                0xFFAA => 0x6A, // KP_Multiply
                0xFFAF => 0x6F, // KP_Divide
                0xFF8D => 0x6C, // KP_Enter
                0xFF9E => 0x2E, // KP_Decimal
                0xFF8C => 0x90, // KP_NumLock
                _ => 0
            };
        }
    }
}
