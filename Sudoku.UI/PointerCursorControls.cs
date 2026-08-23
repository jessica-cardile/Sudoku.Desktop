using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

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
    }

    public sealed class PointerCursorComboBox : ComboBox
    {
        public PointerCursorComboBox()
        {
            ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Hand);
        }
    }
}