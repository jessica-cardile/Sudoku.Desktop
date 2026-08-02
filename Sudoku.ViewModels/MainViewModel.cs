using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sudoku.Engine;
using System.Collections.ObjectModel;
using System.Linq;

namespace Sudoku.ViewModels;

public partial class MainViewModel : ObservableObject
{
    //This holds an ObservableCollection<CellViewModel> of all 81 cells,
    //bridging them directly to the SudokuEngine for generating new puzzles and triggering the solver.

    private readonly SudokuBoard _engineBoard = new();
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
        NewGame(Difficulty.Medium);
    }

    /// <summary>
    /// Maps a difficulty level to the number of cells the engine should empty out.
    /// </summary>
    private static int GetCellsToEmpty(Difficulty difficulty) => difficulty switch
    {
        Difficulty.Easy => 36,
        Difficulty.Medium => 40,
        Difficulty.Hard => 46,
        Difficulty.Expert => 52,
        _ => 40
    };

    /// <summary>
    /// Populates the 9x9 board with a freshly generated puzzle for the given difficulty.
    /// </summary>
    private void InitialiseGrid(Difficulty difficulty)
    {
        Board.Clear();
        _engineBoard.GeneratePuzzle(cellsToEmpty: GetCellsToEmpty(difficulty));

        foreach (var engineCell in _engineBoard.Cells)
        {
            var cellViewModel = new CellViewModel(engineCell.Row, engineCell.Column)
            {
                Value = engineCell.Value,
                IsGiven = engineCell.isStartingClue
            };
            Board.Add(cellViewModel);
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
        var engineCell = _engineBoard.GetCell(SelectedCell.Row, SelectedCell.Column);
        engineCell.Value = number;
        int boxIndex = (SelectedCell.Row / 3) * 3 + (SelectedCell.Column / 3);
        bool isValid = _engineBoard.isRowValid(SelectedCell.Row) &&
                       _engineBoard.isColumnValid(SelectedCell.Column) &&
                       _engineBoard.isBoxValid(boxIndex);
        SelectedCell.IsError = !isValid;
        CheckGameCompletion();
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
        SelectedCell.IsError = false;

        var engineCell = _engineBoard.GetCell(SelectedCell.Row, SelectedCell.Column);
        engineCell.Value = 0;

        StatusMessage = $"Cleared cell ({SelectedCell.Row + 1}, {SelectedCell.Column + 1})";
    }

    /// <summary>
    /// Resets the board with a new puzzle for the given difficulty.
    /// </summary>
    [RelayCommand]
    public void NewGame(Difficulty difficulty = Difficulty.Medium)
    {
        InitialiseGrid(difficulty);
        SelectedCell = null;
        StatusMessage = "New Game Started!";
    }

    /// <summary>
    /// Evaluates whether all cells are populated and valid.
    /// </summary>
    private void CheckGameCompletion()
    {
        bool isFull = _engineBoard.Cells.All(c => c.Value != 0);

        if (isFull && _engineBoard.IsBoardValid())
        {
            StatusMessage = "Congratulations! You solved the puzzle!";
        }
        else
        {
            StatusMessage = SelectedCell?.IsError == true
                ? $"Conflict detected at ({SelectedCell.Row + 1}, {SelectedCell.Column + 1})!"
                : $"Placed {SelectedCell?.Value} at ({SelectedCell!.Row + 1}, {SelectedCell.Column + 1})";
        }
    }
}