using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Sudoku.UI
{
    // ProtectedCursor is only settable from a class that inherits the control
    // this WinAppSDK version has no public UIElement.ChangeCursor API to do it from outside a control's own code.
    public sealed class PointerCursorButton : Button
    {
        public PointerCursorButton()
        {
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
        }

        public void SetEraserCursor(bool useEraser)
        {
            ProtectedCursor = (useEraser ? EraserCursor.Get() : null)
                ?? InputSystemCursor.Create(InputSystemCursorShape.Hand);
        }
    }

    public sealed class PointerCursorComboBox : ComboBox
    {
        public PointerCursorComboBox()
        {
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
        }
    }

    // WinAppSDK has no built-in eraser cursor, so the .cur is loaded through Win32 and wrapped
    // with the InputCursor interop interface. Returns null on any failure so callers fall back to the hand cursor.
    internal static class EraserCursor
    {
        private static InputCursor? s_cursor;
        private static bool s_loadAttempted;

        public static InputCursor? Get()
        {
            if (!s_loadAttempted)
            {
                s_loadAttempted = true;
                s_cursor = TryLoad();
            }

            return s_cursor;
        }

        private static InputCursor? TryLoad()
        {
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, "Assets", "eraser.cur");

                IntPtr hCursor = LoadCursorFromFileW(path);
                if (hCursor == IntPtr.Zero)
                {
                    Debug.WriteLine($"EraserCursor: LoadCursorFromFileW failed for '{path}' (Win32 error {Marshal.GetLastWin32Error()}).");
                    return null;
                }

                const string className = "Microsoft.UI.Input.InputCursor";
                Marshal.ThrowExceptionForHR(WindowsCreateString(className, className.Length, out IntPtr hClassName));

                try
                {
                    Guid iid = typeof(IInputCursorStaticsInterop).GUID;
                    Marshal.ThrowExceptionForHR(RoGetActivationFactory(hClassName, ref iid, out IInputCursorStaticsInterop statics));

                    Marshal.ThrowExceptionForHR(statics.CreateFromHCursor(hCursor, out IntPtr cursorAbi));

                    try
                    {
                        return WinRT.MarshalInspectable<InputCursor>.FromAbi(cursorAbi);
                    }
                    finally
                    {
                        WinRT.MarshalInspectable<InputCursor>.DisposeAbi(cursorAbi);
                    }
                }
                finally
                {
                    WindowsDeleteString(hClassName);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EraserCursor: could not create the eraser cursor, falling back to the hand cursor. {ex}");
                return null;
            }
        }

        [ComImport, Guid("ac6f5065-90c4-46ce-beb7-05e138e54117"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IInputCursorStaticsInterop
        {
            // IInspectable slots, which precede CreateFromHCursor in the vtable.
            void GetIids();
            void GetRuntimeClassName();
            void GetTrustLevel();

            [PreserveSig]
            int CreateFromHCursor(IntPtr hCursor, out IntPtr inputCursor);
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadCursorFromFileW(string name);

        [DllImport("api-ms-win-core-winrt-l1-1-0.dll")]
        private static extern int RoGetActivationFactory(IntPtr activatableClassId, ref Guid iid, out IInputCursorStaticsInterop factory);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll", CharSet = CharSet.Unicode)]
        private static extern int WindowsCreateString(string sourceString, int length, out IntPtr hstring);

        [DllImport("api-ms-win-core-winrt-string-l1-1-0.dll")]
        private static extern int WindowsDeleteString(IntPtr hstring);
    }
}
