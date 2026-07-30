using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Sudoku.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    //This holds an ObservableCollection<CellViewModel> of all 81 cells,
    //bridging them directly to the SudokuEngine
    //for generating new puzzles and triggering the solver.

    /// <summary>
    /// The 81 cells bound to the UI.
    /// </summary>
    public ObservableCollection<CellViewModel> Board { get; } = new();

    /// <summary>
    /// Tracks the currently active/highlighted cell in the UI.
    /// </summary>
    [ObservableProperty]
    public partial CellViewModel? SelectedCell { get; set; }

    /// <summary>
    /// Status text displayed in the UI.
    /// </summary>
    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Select a cell to begin";

    public MainViewModel()
    {
        InitialiseGrid();
    }

    /// <summary>
    /// Populates the 9x9 board with empty cell view models.
    /// </summary>
    private void InitialiseGrid()
    {
        Board.Clear();
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 9; col++)
            {
                Board.Add(new CellViewModel(row, col));
            }
        }
    }

    /// <summary>
    /// Partial property changes automatically for the SelectedCell property.
    /// </summary>
    partial void OnSelectedCellChanged(CellViewModel? oldValue, CellViewModel? newValue)
    {
        if(oldValue != null)
        {
            oldValue.IsSelected = false;
        }
        if (newValue != null)
        {
            newValue.IsSelected = true;
        }
    }

    /// <summary>
    /// Sets the selected cell when clicked in the UI.       
    /// </summary>
    [RelayCommand]
    private void SelectCell(CellViewModel cell)
    {
        SelectedCell = cell;
    }

    /// <summary>
    /// Inputs a number (1-9) into the currently selected cell.
    /// </summary>
    [RelayCommand]
    public void InputNumber(int number)
    {
        //don't modify fixed starting clues or empty selections
        if (SelectedCell == null || SelectedCell.IsGiven)
        {
            return;
        }

        SelectedCell.Value = number;

        //TODO: Call the Engine to validate move or check if puzzle is solved!
    }

    /// <summary>
    /// Clears the value of the selected cell
    /// </summary>
    [RelayCommand]
    public void ClearSelectedCell()
    {
        if ( (SelectedCell == null || SelectedCell.IsGiven))
        {
            return;
        }

        SelectedCell.Value = 0;
    }

    /// <summary>
    /// Resets the board!
    /// </summary>
    [RelayCommand]
    private void NewGame()
    {
        InitialiseGrid();
        SelectedCell = null;
        StatusMessage = "New Game Started!";
    }
}