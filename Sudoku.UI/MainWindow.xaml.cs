using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Text;
using Sudoku.UI.Services;
using Sudoku.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Sudoku.UI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private const double MinBoardSize = 350;
        private const double MaxBoardSize = 700;
        private const double MinSidebarWidth = 200;
        private const double MaxSidebarWidth = 320;
        private const int MaxLimitedHints = 3;

        // Layout measurements (in DIPs) shared by the board sizing and the minimum window width.
        private const double ColumnGap = 48;                  // the gap column between board and sidebar
        private const double BoardChrome = 22;                // board border: 8*2 padding + 3*2 thickness
        private const double RootGridHorizontalPadding = 48;  // RootGrid Padding="24" on each side
        private const int BaseMinimumWindowWidth = 900;
        private const int WindowFrameAllowancePixels = 16;

        private readonly Brush _accentBrush;
        private readonly Brush _accentTextBrush;
        private readonly Brush _cellBorderBrush;
        private readonly Brush _userPlacedTextBrush;
        private readonly Brush _highlightBrush;

        private readonly DispatcherTimer _gameTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        private TimeSpan _elapsed = TimeSpan.Zero;
        private bool _isPaused;
        private bool _isReady;
        private bool _wasPausedBeforeConfirmDialog;
        private bool _wasPausedBeforeSettingsDialog;
        private int _hintsUsed;

        private readonly DispatcherTimer _hintPulseTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
        private double _hintPulsePhase;
        private bool _hintPulsing;
        private bool _erasePulsing;
        private bool _erasePulseSuppressed;

        private const string BugReportEmail = "jessicacardile.dev@outlook.com";
        private string _appVersion = string.Empty;
        private bool _sidebarEnabled = true;

        private readonly List<Button> _cellButtons = new();
        private readonly List<Button> _highlightedCells = new();
        private readonly List<Button> _numberPadButtons = new();
        private readonly List<Button> _actionButtons;

        // The digit (1-9) or eraser (0) currently armed from the sidebar, awaiting a cell click.
        private int? _armedValue;
        private Button? _armedButton;

        private readonly MainViewModel _viewModel = new();

        private Microsoft.UI.Windowing.AppWindow? _appWindow;

        public MainWindow()
        {
            this.InitializeComponent();

            _actionButtons = new List<Button> { EraseButton, HintButton, PauseButton };

            _accentBrush = (Brush)Application.Current.Resources["AppAccentBrush"];
            _accentTextBrush = (Brush)Application.Current.Resources["AppAccentTextBrush"];
            _cellBorderBrush = (Brush)Application.Current.Resources["AppCellBorderBrush"];
            _userPlacedTextBrush = (Brush)Application.Current.Resources["AppUserPlacedTextBrush"];
            _highlightBrush = (Brush)Application.Current.Resources["AppHighlightBrush"];

            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            _appWindow = appWindow;
            appWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "title-icon.ico"));
            ApplyTitleBarColors(appWindow);

            this.Activated += MainWindow_Activated;

            var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
            var workArea = displayArea.WorkArea;

            int width = Math.Min((int)(workArea.Width * 0.7), 1400);
            int height = Math.Min((int)(workArea.Height * 0.8), 1000);

            appWindow.Resize(new SizeInt32(width, height));

            if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
            {
                presenter.PreferredMinimumWidth = BaseMinimumWindowWidth;
                presenter.PreferredMinimumHeight = 650;
            }

            this.GenerateGridCells();
            this.ApplyBoardToUi();

            ContentArea.SizeChanged += ContentArea_SizeChanged;

            _gameTimer.Tick += GameTimer_Tick;
            _gameTimer.Start();

            _numberPadButtons.AddRange(NumberPadGrid.Children.OfType<Button>());

            foreach (var button in _numberPadButtons)
            {
                button.Translation = new System.Numerics.Vector3(0, 0, 16);
            }

            _hintPulseTimer.Tick += HintPulseTimer_Tick;
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            UpdatePulseEffects();

            DisplayTimerToggle.IsOn = AppSettings.DisplayTimer;
            ApplyDisplayTimerVisibility(DisplayTimerToggle.IsOn);

            LimitedHintsToggle.IsOn = AppSettings.LimitedHints;

            var version = Windows.ApplicationModel.Package.Current.Id.Version;
            _appVersion = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
            AppVersionText.Text = $"App Version: {_appVersion}";

            _isReady = true;
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (_appWindow is not null)
            {
                ApplyTitleBarColors(_appWindow);
            }
        }

        private void ApplyTitleBarColors(Microsoft.UI.Windowing.AppWindow appWindow)
        {
            if (!Microsoft.UI.Windowing.AppWindowTitleBar.IsCustomizationSupported())
            {
                return;
            }

            if (IsAccentColorOnTitleBarsEnabled())
            {
                // Use user's custom accent colour on title bars if they have chosen one
                return;
            }

            var darkBackground = Color.FromArgb(255, 0x1B, 0x1B, 0x24);
            var accent = ((SolidColorBrush)_accentBrush).Color;
            var mutedText = ((SolidColorBrush)Application.Current.Resources["AppMutedTextBrush"]).Color;

            var titleBar = appWindow.TitleBar;
            titleBar.BackgroundColor = darkBackground;
            titleBar.InactiveBackgroundColor = darkBackground;
            titleBar.ForegroundColor = Colors.White;
            titleBar.InactiveForegroundColor = mutedText;

            //If the user toggles off the "Show accent colour on title bars and window borders" setting,
            //the system will ignore any custom button colours and use its own defaults instead.
            //To ensure that the buttons remain visible and legible, we explicitly set their colours to match the title bar's background and foreground.
            titleBar.ButtonBackgroundColor = darkBackground;
            titleBar.ButtonInactiveBackgroundColor = darkBackground;
            titleBar.ButtonForegroundColor = Colors.White;
            titleBar.ButtonInactiveForegroundColor = mutedText;
            titleBar.ButtonHoverBackgroundColor = accent;
            titleBar.ButtonHoverForegroundColor = Colors.White;
            titleBar.ButtonPressedBackgroundColor = accent;
            titleBar.ButtonPressedForegroundColor = Colors.White;
        }

        private static bool IsAccentColorOnTitleBarsEnabled()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\DWM");
                return key?.GetValue("ColorPrevalence") is int value && value == 1;
            }
            catch
            {
                return false;
            }
        }

        private void ContentArea_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // The header controls (difficulty, New Game, Settings) set the right column's width, so the
            // board must leave room for them or the row gets clipped on narrower windows.
            double headerWidth = MeasureDesiredWidth(HeaderControlsPanel);
            double roomForBoard = e.NewSize.Width - ColumnGap - headerWidth - BoardChrome;

            double boardSize = Math.Clamp(
                Math.Min(Math.Min(e.NewSize.Width * 0.55, e.NewSize.Height - 40), roomForBoard),
                MinBoardSize, MaxBoardSize);

            BoardGrid.Width = boardSize;
            BoardGrid.Height = boardSize;
            UpdateCellFontSize(boardSize);

            double scale = boardSize / MinBoardSize;
            UpdateSidebarScale(scale);

            UpdateMinimumWindowWidth(headerWidth);
        }

        private static double MeasureDesiredWidth(FrameworkElement element)
        {
            element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return element.DesiredSize.Width;
        }

        // Keeps the window from being sized narrower than the layout needs at the smallest board size
        // (title + gap + header controls), and grows it if it is currently too narrow for that.
        private void UpdateMinimumWindowWidth(double headerWidth)
        {
            if (_appWindow?.Presenter is not Microsoft.UI.Windowing.OverlappedPresenter presenter || Content.XamlRoot is null)
            {
                return;
            }

            double contentWidth = Math.Max(MeasureDesiredWidth(TitlePanel), MinBoardSize + BoardChrome) + ColumnGap + headerWidth;
            double minDips = RootGridHorizontalPadding + contentWidth;
            int minPixels = Math.Max(
                BaseMinimumWindowWidth,
                (int)Math.Ceiling(minDips * Content.XamlRoot.RasterizationScale) + WindowFrameAllowancePixels);

            if (presenter.PreferredMinimumWidth != minPixels)
            {
                presenter.PreferredMinimumWidth = minPixels;
            }

            var workArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(_appWindow.Id, Microsoft.UI.Windowing.DisplayAreaFallback.Nearest).WorkArea;
            int targetWidth = Math.Min(minPixels, workArea.Width);

            if (_appWindow.Size.Width < targetWidth)
            {
                _appWindow.Resize(new SizeInt32(targetWidth, _appWindow.Size.Height));
            }
        }

        private void UpdateSidebarScale(double scale)
        {
            double clamped = Math.Clamp(scale, 1.0, 1.6);

            SidebarPanel.Width = Math.Clamp(MinSidebarWidth * clamped, MinSidebarWidth, MaxSidebarWidth);
            TimerText.FontSize = 24 * clamped;

            foreach (var btn in _numberPadButtons)
            {
                btn.FontSize = 16 * clamped;
                btn.Height = 48 * clamped;
            }

            foreach (var btn in _actionButtons)
            {
                btn.FontSize = 14 * clamped;
                btn.Height = 44 * clamped;
            }
        }

        private void UpdateCellFontSize(double boardSize)
        {
            double cellSize = boardSize / 9;
            double fontSize = Math.Clamp(cellSize * 0.7, 20, 44);

            foreach (var button in _cellButtons)
            {
                button.FontSize = fontSize;
            }
        }

        private void GenerateGridCells()
        {
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    // Create thin borders for individual cells
                    var cellBorder = new Border
                    {
                        BorderBrush = new SolidColorBrush(Colors.White) { Opacity = 0.35 },
                        BorderThickness = new Thickness(
                            left: col == 0 ? 0 : 0.5,
                            top: row == 0 ? 0 : 0.5,
                            right: col == 8 ? 0 : 0.5,
                            bottom: row == 8 ? 0 : 0.5
                        ),
                        CornerRadius = new CornerRadius(0),
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch
                    };

                    // Create the clickable cell that displays a digit
                    var cellButton = new PointerCursorButton
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0),
                        Padding = new Thickness(0),
                        FontFamily = new FontFamily("Curlz MT"),
                        FontSize = 28,
                        BorderThickness = new Thickness(0),
                        CornerRadius = new CornerRadius(0),
                        Background = new SolidColorBrush(Colors.Transparent),
                        Foreground = new SolidColorBrush(Colors.White)
                    };

                    int cellIndex = row * 9 + col;
                    cellButton.Click += (s, e) => OnCellClicked(cellButton, cellIndex);

                    cellBorder.Child = cellButton;
                    _cellButtons.Add(cellButton);

                    // Position within the 9x9 Grid
                    Grid.SetRow(cellBorder, row);
                    Grid.SetColumn(cellBorder, col);

                    // Insert behind the thick 3x3 major outlines
                    BoardGrid.Children.Insert(0, cellBorder);
                }
            }
        }

        private void RootGrid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            // Disarm selection only when the user clicks outside the board and sidebar, on the background itself.
            if (ReferenceEquals(e.OriginalSource, RootGrid))
            {
                DisarmAction();
            }
        }

        private void NumberPad_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string digitText && int.TryParse(digitText, out int digit))
            {
                ArmAction(digit, button);
            }
        }

        private void EraseButton_Click(object sender, RoutedEventArgs e)
        {
            ArmAction(0, EraseButton);
        }

        private void ArmAction(int value, Button sourceButton)
        {
            bool wasAlreadyArmed = _armedButton == sourceButton;

            DisarmAction();

            if (!wasAlreadyArmed)
            {
                _armedValue = value;
                _armedButton = sourceButton;
                sourceButton.Background = _highlightBrush;
                sourceButton.Foreground = _accentTextBrush;

                if (value != 0)
                {
                    HighlightMatchingCells(value);
                }
            }

            UpdateCellCursors();
            UpdatePulseEffects();
        }

        private void DisarmAction()
        {
            if (_armedButton is not null)
            {
                _armedButton.ClearValue(Button.BackgroundProperty);
                _armedButton.Foreground = new SolidColorBrush(Colors.White);
            }

            _armedButton = null;
            _armedValue = null;
            _erasePulseSuppressed = false;

            ClearMatchingHighlights();
            UpdateCellCursors();
            UpdatePulseEffects();
        }

        // While erase mode is armed, editable cells show the eraser cursor; given cells can't be erased so keep the hand.
        private void UpdateCellCursors()
        {
            bool eraserArmed = _armedValue == 0;

            for (int i = 0; i < _cellButtons.Count; i++)
            {
                if (_cellButtons[i] is PointerCursorButton cellButton)
                {
                    cellButton.SetEraserCursor(eraserArmed && !_viewModel.Board[i].IsGiven);
                }
            }
        }

        private void HighlightMatchingCells(int digit)
        {
            string digitText = digit.ToString();

            foreach (var button in _cellButtons)
            {
                if (button.Content as string == digitText)
                {
                    HighlightCell(button);
                }
            }
        }

        private void HighlightCell(Button button)
        {
            // A dedicated (dark) highlight brush, distinct from the button-accent purple, so the
            // cell's own text color (white/violet/red) stays legible without needing an override.
            button.Background = _highlightBrush;
            _highlightedCells.Add(button);
        }

        private void ClearMatchingHighlights()
        {
            foreach (var button in _highlightedCells)
            {
                button.Background = new SolidColorBrush(Colors.Transparent);
            }

            _highlightedCells.Clear();
        }

        private void OnCellClicked(Button cellButton, int cellIndex)
        {
            var cellViewModel = _viewModel.Board[cellIndex];

            if (cellViewModel.IsGiven)
            {
                return;
            }

            if (_armedValue is not int value)
            {
                return;
            }

            _viewModel.SelectCell(cellViewModel);

            if (value == 0)
            {
                _viewModel.ClearSelectedCell();
            }
            else
            {
                _viewModel.InputNumber(value);
            }

            RenderCell(cellButton, cellViewModel);
            UpdateNumberPadAvailability();

            // The armed digit/eraser stays selected so the user can place it again without
            // it's only cleared by picking a different one or tapping outside the board.
            if (value != 0 && cellViewModel.Value == value)
            {
                HighlightCell(cellButton);
            }
        }

        private void NewGameButton_Click(object sender, RoutedEventArgs e)
        {
            StartNewGame((Difficulty)DifficultyComboBox.SelectedIndex);
        }

        private void StartNewGame(Difficulty difficulty)
        {
            _viewModel.NewGame(difficulty);

            DisarmAction();
            ApplyBoardToUi();
            ResetTimerAndPauseState();
            _hintsUsed = 0;
        }

        private void DifficultyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isReady)
            {
                return;
            }

            ShowNewGameConfirmDialog();
        }

        private void ShowNewGameConfirmDialog()
        {
            _wasPausedBeforeConfirmDialog = _isPaused;

            if (!_isPaused)
            {
                _gameTimer.Stop();
            }

            DifficultyComboBox.IsEnabled = false;
            NewGameButton.IsEnabled = false;
            SetSidebarButtonsEnabled(false);

            NewGameConfirmOverlay.Visibility = Visibility.Visible;
        }

        private void HideNewGameConfirmDialogChrome()
        {
            NewGameConfirmOverlay.Visibility = Visibility.Collapsed;

            DifficultyComboBox.IsEnabled = true;
            NewGameButton.IsEnabled = true;
            SetSidebarButtonsEnabled(true);
        }

        private void SetSidebarButtonsEnabled(bool isEnabled)
        {
            _sidebarEnabled = isEnabled;

            // Settings lives in the header now, but is still disabled whenever an overlay/dialog is up.
            SettingsButton.IsEnabled = isEnabled;

            foreach (var button in _numberPadButtons)
            {
                button.IsEnabled = isEnabled;
            }

            foreach (var button in _actionButtons)
            {
                button.IsEnabled = button == HintButton ? isEnabled && CanUseHint() : isEnabled;
            }

            if (isEnabled)
            {
                // Re-applies the greyed-out state of digits that are already fully placed.
                UpdateNumberPadAvailability();
            }
        }

        private void NewGameConfirmYesButton_Click(object sender, RoutedEventArgs e)
        {
            HideNewGameConfirmDialogChrome();
            StartNewGame((Difficulty)DifficultyComboBox.SelectedIndex);
        }

        private void NewGameConfirmNoButton_Click(object sender, RoutedEventArgs e)
        {
            HideNewGameConfirmDialogChrome();

            if (!_wasPausedBeforeConfirmDialog)
            {
                _gameTimer.Start();
            }
        }

        private void ApplyBoardToUi()
        {
            for (int i = 0; i < _cellButtons.Count; i++)
            {
                var cell = _viewModel.Board[i];
                var button = _cellButtons[i];

                RenderCell(button, cell);
            }

            UpdateNumberPadAvailability();
        }

        /// <summary>
        /// Greys out and disables a number pad button once all 9 instances of its digit are
        /// already placed on the board, so the player can see at a glance which digits are
        /// exhausted.
        /// </summary>
        private void UpdateNumberPadAvailability()
        {
            foreach (var button in _numberPadButtons)
            {
                if (button.Tag is not string digitText || !int.TryParse(digitText, out int digit))
                {
                    continue;
                }

                int placedCount = _viewModel.Board.Count(c => c.Value == digit);
                bool isExhausted = placedCount >= 9;

                button.IsEnabled = !isExhausted;
                button.Opacity = isExhausted ? 0.35 : 1;

                if (isExhausted && _armedButton == button)
                {
                    DisarmAction();
                }
            }
        }

        private void RenderCell(Button button, CellViewModel cell)
        {
            button.Content = cell.Value == 0 ? string.Empty : cell.Value.ToString();
            button.FontWeight = cell.IsGiven ? FontWeights.Bold : FontWeights.Normal;
            SetCellForeground(button, cell);
        }

        private void SetCellForeground(Button cellButton, CellViewModel cell)
        {
            Brush brush = cell.IsGiven
                ? new SolidColorBrush(Colors.White)
                : cell.IsError
                    ? new SolidColorBrush(Colors.Red)
                    : _userPlacedTextBrush;

            cellButton.Foreground = brush;

            // The default Button style overrides Foreground on pointer-over/pressed via these
            // theme resource keys, which would otherwise hide the error colour on hover/press.
            cellButton.Resources["ButtonForegroundPointerOver"] = brush;
            cellButton.Resources["ButtonForegroundPressed"] = brush;
        }

        private void ResetTimerAndPauseState()
        {
            _elapsed = TimeSpan.Zero;
            TimerText.Text = "00:00";

            SetPaused(false);
        }

        private void HintButton_Click(object sender, RoutedEventArgs e)
        {
            // IsHintAvailable is only ever true when there's an editable cell to reveal, so this
            // is a genuine use of the hint, not just a no-op click.
            if (_viewModel.IsHintAvailable)
            {
                _hintsUsed++;
            }

            // Using a hint while Erase is armed leaves Erase armed but ends its pulse; it starts
            // pulsing again only if Erase is re-armed. Set before UseHint so the pulse update
            // triggered by IsHintAvailable changing already sees it.
            if (_armedValue == 0)
            {
                _erasePulseSuppressed = true;
            }

            _viewModel.UseHint();

            if (_viewModel.SelectedCell is CellViewModel cell)
            {
                int index = cell.Row * 9 + cell.Column;
                RenderCell(_cellButtons[index], cell);
                UpdateNumberPadAvailability();
                UpdateCellCursors();
                UpdatePulseEffects();
            }
        }

        /// <summary>
        /// Whether a hint can currently be given: the selected cell has to hold a wrong
        /// placement, and - when the Limited Hints setting is on - the per-game cap must not
        /// already be used up.
        /// </summary>
        private bool CanUseHint() =>
            _viewModel.IsHintAvailable && (!AppSettings.LimitedHints || _hintsUsed < MaxLimitedHints);

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.IsHintAvailable))
            {
                UpdatePulseEffects();
            }

            if (e.PropertyName == nameof(MainViewModel.IsSolved) && _viewModel.IsSolved)
            {
                ShowWinOverlay();
            }
        }

        // The Hint button is enabled whenever a hint can be used, but its glow only pulses while Erase mode
        // is off; while Erase is armed the pulse moves to the Erase button instead.
        private void UpdatePulseEffects()
        {
            bool eraseArmed = _armedValue == 0;
            bool canUseHint = CanUseHint();

            HintButton.IsEnabled = canUseHint && _sidebarEnabled;

            _hintPulsing = canUseHint && !eraseArmed;
            _erasePulsing = eraseArmed && !_erasePulseSuppressed;

            if (_hintPulsing || _erasePulsing)
            {
                if (!_hintPulseTimer.IsEnabled)
                {
                    _hintPulsePhase = 0;
                    _hintPulseTimer.Start();
                }
            }
            else
            {
                _hintPulseTimer.Stop();
            }

            if (!_hintPulsing)
            {
                HintGlow.Opacity = 0;
            }

            if (!_erasePulsing)
            {
                EraseGlow.Opacity = 0;
            }
        }

        private void HintPulseTimer_Tick(object? sender, object e)
        {
            _hintPulsePhase += 0.15;
            double opacity = 0.55 + (Math.Sin(_hintPulsePhase) * 0.35); // oscillates ~0.2 -> 0.9

            if (_hintPulsing)
            {
                HintGlow.Opacity = opacity;
            }

            if (_erasePulsing)
            {
                EraseGlow.Opacity = opacity;
            }
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            SetPaused(!_isPaused);
        }

        private void SetPaused(bool paused)
        {
            _isPaused = paused;

            if (_isPaused)
            {
                _gameTimer.Stop();
                PauseButton.Content = "Resume";
                PauseOverlay.Visibility = Visibility.Visible;
            }
            else
            {
                _gameTimer.Start();
                PauseButton.Content = "Pause";
                PauseOverlay.Visibility = Visibility.Collapsed;
            }
        }

        // TODO(debug): remove this handler along with the DebugTriggerWinButton in
        // MainWindow.xaml once the win animation is confirmed working.
        private void DebugTriggerWinButton_Click(object sender, RoutedEventArgs e)
        {
            ShowWinOverlay();
        }

        private void ShowWinOverlay()
        {
            _gameTimer.Stop();
            WinOverlay.Visibility = Visibility.Visible;
            SetSidebarButtonsEnabled(false);
        }

        private void PlayAgainButton_Click(object sender, RoutedEventArgs e)
        {
            WinOverlay.Visibility = Visibility.Collapsed;
            SetSidebarButtonsEnabled(true);
            StartNewGame((Difficulty)DifficultyComboBox.SelectedIndex);
        }

        // Opens the user's default mail app with a pre-filled bug report addressed to the developer.
        private async void ReportBugButton_Click(object sender, RoutedEventArgs e)
        {
            string subject = Uri.EscapeDataString($"Sudoku bug report (v{_appVersion})");
            string body = Uri.EscapeDataString(
                $"App version: {_appVersion}\r\n\r\nWhat happened:\r\n\r\nWhat I expected:\r\n\r\nSteps to reproduce:\r\n");

            await Windows.System.Launcher.LaunchUriAsync(new Uri($"mailto:{BugReportEmail}?subject={subject}&body={body}"));
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            _wasPausedBeforeSettingsDialog = _isPaused;

            if (!_isPaused)
            {
                _gameTimer.Stop();
            }

            SettingsOverlay.Visibility = Visibility.Visible;
            SetSidebarButtonsEnabled(false);
        }

        private void CloseSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsOverlay.Visibility = Visibility.Collapsed;
            SetSidebarButtonsEnabled(true);

            if (!_wasPausedBeforeSettingsDialog)
            {
                _gameTimer.Start();
            }
        }

        private void DisplayTimerToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!_isReady)
            {
                return;
            }

            bool isOn = DisplayTimerToggle.IsOn;
            AppSettings.DisplayTimer = isOn;
            ApplyDisplayTimerVisibility(isOn);
        }

        private void ApplyDisplayTimerVisibility(bool isOn)
        {
            // The Timer card itself stays put; only the ticking value swaps for a
            // placeholder image so the sidebar layout doesn't shift.
            TimerValuePanel.Visibility = isOn ? Visibility.Visible : Visibility.Collapsed;
            TimerHiddenPlaceholder.Visibility = isOn ? Visibility.Collapsed : Visibility.Visible;
        }

        private void LimitedHintsToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!_isReady)
            {
                return;
            }

            AppSettings.LimitedHints = LimitedHintsToggle.IsOn;

            // Re-evaluate immediately: toggling this can enable/disable the Hint button right
            // away even mid-game, without waiting for the selected cell to change.
            UpdatePulseEffects();
        }

        private void GameTimer_Tick(object? sender, object e)
        {
            _elapsed = _elapsed.Add(TimeSpan.FromSeconds(1));
            TimerText.Text = _elapsed.ToString(@"mm\:ss");
        }
    }
}