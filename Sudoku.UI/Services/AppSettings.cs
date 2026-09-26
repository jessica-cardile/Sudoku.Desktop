using Windows.Storage;

namespace Sudoku.UI.Services
{
    /// <summary>
    /// Thin wrapper around Windows.Storage.ApplicationData.Current.LocalSettings for simple
    /// app settings. Add one property per setting; each get/set reads and writes straight
    /// through, so values persist immediately and survive app restarts.
    /// </summary>
    public static class AppSettings
    {
        private const string DisplayTimerKey = "DisplayTimer";
        private const bool DisplayTimerDefault = true;

        private const string LimitedHintsKey = "LimitedHints";
        private const bool LimitedHintsDefault = false;

        private static Windows.Foundation.Collections.IPropertySet Values =>
            ApplicationData.Current.LocalSettings.Values;

        public static bool DisplayTimer
        {
            get => Values.TryGetValue(DisplayTimerKey, out var value) && value is bool stored
                ? stored
                : DisplayTimerDefault;
            set => Values[DisplayTimerKey] = value;
        }

        /// <summary>
        /// When true, hints are capped at MainWindow.MaxLimitedHints per game instead of being
        /// unlimited.
        /// </summary>
        public static bool LimitedHints
        {
            get => Values.TryGetValue(LimitedHintsKey, out var value) && value is bool stored
                ? stored
                : LimitedHintsDefault;
            set => Values[LimitedHintsKey] = value;
        }
    }
}
