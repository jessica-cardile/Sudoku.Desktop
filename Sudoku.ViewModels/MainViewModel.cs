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

    /// <summary>
    /// True exactly when the selected cell currently holds a wrong placement, i.e. a hint
    /// would have something useful to fix.
    /// </summary>
    [ObservableProperty]
    public partial bool IsHintAvailable { get; set; }

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

        //Hint is only available while the newly selected cell holds a wrong placement.
        IsHintAvailable = newValue?.IsError == true;
    }

    /// <summary>
    /// Sets the selected cell when clicked in the UI.       
    /// </summary>
    [RelayCommand]
    public void SelectCell(CellViewModel cell)
    {
        SelectedCell = cell;
    }

    /// <summary>
    /// Returns the engine-side cell paired with the current SelectedCell, or null if there is
    /// no selection or the selection is a fixed starting clue that can't be edited.
    /// </summary>
    private Cell? GetEditableEngineCell()
    {
        if (SelectedCell == null || SelectedCell.IsGiven)
        {
            return null;
        }

        return _engineBoard.GetCell(SelectedCell.Row, SelectedCell.Column);
    }

    /// <summary>
    /// Inputs a number (1-9) into the currently selected cell.
    /// </summary>
    [RelayCommand]
    public void InputNumber(int number)
    {
        var engineCell = GetEditableEngineCell();
        if (engineCell == null)
        {
            return;
        }

        SelectedCell!.Value = number;
        engineCell.Value = number;
        SelectedCell.IsError = number != _engineBoard.GetSolutionValue(SelectedCell.Row, SelectedCell.Column);
        IsHintAvailable = SelectedCell.IsError;
        CheckGameCompletion();
    }

    /// <summary>
    /// Clears the value of the selected cell
    /// </summary>
    [RelayCommand]
    public void ClearSelectedCell()
    {
        var engineCell = GetEditableEngineCell();
        if (engineCell == null)
        {
            return;
        }

        SelectedCell!.Value = 0;
        SelectedCell.IsError = false;
        IsHintAvailable = false;
        engineCell.Value = 0;

        StatusMessage = $"Cleared cell ({SelectedCell.Row + 1}, {SelectedCell.Column + 1})";
    }

    /// <summary>
    /// Reveals the correct value for the selected cell and locks it, like a starting clue.
    /// </summary>
    [RelayCommand]
    public void UseHint()
    {
        var engineCell = GetEditableEngineCell();
        if (engineCell == null)
        {
            StatusMessage = "Select an empty cell to get a hint.";
            return;
        }

        int correctValue = _engineBoard.GetSolutionValue(SelectedCell!.Row, SelectedCell.Column);

        SelectedCell.Value = correctValue;
        SelectedCell.IsError = false;
        SelectedCell.IsGiven = true;
        IsHintAvailable = false;

        engineCell.Value = correctValue;
        engineCell.isStartingClue = true;

        CheckGameCompletion();
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