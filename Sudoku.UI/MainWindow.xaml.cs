using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;

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

        private readonly Brush _accentBrush;
        private readonly Brush _accentTextBrush;
        private readonly Brush _cellBorderBrush;

        private readonly DispatcherTimer _gameTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        private TimeSpan _elapsed = TimeSpan.Zero;

        private readonly List<TextBox> _cellTextBoxes = new();
        private TextBox? _selectedCell;

        public MainWindow()
        {
            this.InitializeComponent();

            _accentBrush = (Brush)Application.Current.Resources["AppAccentBrush"];
            _accentTextBrush = (Brush)Application.Current.Resources["AppAccentTextBrush"];
            _cellBorderBrush = (Brush)Application.Current.Resources["AppCellBorderBrush"];

            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
            var workArea = displayArea.WorkArea;

            int width = Math.Min((int)(workArea.Width * 0.7), 1400);
            int height = Math.Min((int)(workArea.Height * 0.8), 1000);

            appWindow.Resize(new SizeInt32(width, height));

            if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
            {
                presenter.PreferredMinimumWidth = 900;
                presenter.PreferredMinimumHeight = 650;
            }

            this.GenerateGridCells();

            ContentArea.SizeChanged += ContentArea_SizeChanged;

            _gameTimer.Tick += GameTimer_Tick;
            _gameTimer.Start();
        }

        private void ContentArea_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double boardSize = Math.Clamp(
                Math.Min(e.NewSize.Width * 0.55, e.NewSize.Height - 40),
                MinBoardSize, MaxBoardSize);

            BoardGrid.Width = boardSize;
            BoardGrid.Height = boardSize;
            UpdateCellFontSize(boardSize);

            double scale = boardSize / MinBoardSize;
            UpdateSidebarScale(scale);
        }

        private void UpdateSidebarScale(double scale)
        {
            double clamped = Math.Clamp(scale, 1.0, 1.6);

            SidebarPanel.Width = Math.Clamp(MinSidebarWidth * clamped, MinSidebarWidth, MaxSidebarWidth);
            TimerText.FontSize = 24 * clamped;

            foreach (var btn in NumberPadGrid.Children.OfType<Button>())
            {
                btn.FontSize = 16 * clamped;
                btn.Height = 48 * clamped;
            }

            EraseButton.FontSize = HintButton.FontSize = 14 * clamped;
            EraseButton.Height = HintButton.Height = 44 * clamped;
        }

        private void UpdateCellFontSize(double boardSize)
        {
            double cellSize = boardSize / 9;
            double fontSize = Math.Clamp(cellSize * 0.5, 14, 28);

            foreach (var textBox in _cellTextBoxes)
            {
                textBox.FontSize = fontSize;
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
                        BorderBrush = (SolidColorBrush)Application.Current.Resources["BoardLineBrush"],
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

                    // Create input box for digits
                    var textBox = new TextBox
                    {
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        FontFamily = new FontFamily("Segoe UI Variable"),
                        FontSize = 20,
                        MaxLength = 1,
                        BorderThickness = new Thickness(0),
                        CornerRadius = new CornerRadius(0),
                        Background = new SolidColorBrush(Colors.Transparent)
                    };

                    textBox.GotFocus += (s, e) =>
                    {
                        _selectedCell = textBox;
                        cellBorder.Background = (SolidColorBrush)Application.Current.Resources["AppAccentBrush"];
                    };

                    textBox.LostFocus += (s, e) =>
                    {
                        cellBorder.Background = new SolidColorBrush(Colors.Transparent);

                        if (ReferenceEquals(_selectedCell, textBox))
                        {
                            _selectedCell = null;
                        }
                    };

                    cellBorder.Child = textBox;
                    _cellTextBoxes.Add(textBox);

                    // Position within the 9x9 Grid
                    Grid.SetRow(cellBorder, row);
                    Grid.SetColumn(cellBorder, col);

                    // Insert behind the thick 3x3 major outlines
                    BoardGrid.Children.Insert(0, cellBorder);
                }
            }
        }

        private void NumberPad_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCell is null)
            {
                return;
            }

            if (sender is Button button && button.Tag is string digit)
            {
                _selectedCell.Text = digit;
            }
        }

        private void EraseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCell is not null)
            {
                _selectedCell.Text = string.Empty;
            }
        }

        private void NewGameButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: generate a new puzzle for the selected difficulty.
        }

        private void HintButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: reveal a hint for the selected cell.
        }

        private void GameTimer_Tick(object? sender, object e)
        {
            _elapsed = _elapsed.Add(TimeSpan.FromSeconds(1));
            TimerText.Text = _elapsed.ToString(@"mm\:ss");
        }
    }
}
