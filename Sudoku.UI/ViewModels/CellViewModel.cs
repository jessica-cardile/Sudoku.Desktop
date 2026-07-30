using System;
using System.Collections.Generic;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Sudoku.UI.ViewModels;

public partial class CellViewModel : ObservableObject
{
    //This wraps an individual cell location,
    //storing display state such as its value,
    //whether it was part of the original puzzle clues,
    //whether it's currently selected, and whether it has a validation error.

    public int Row { get; }
    public int Column { get; }

    [ObservableProperty]
    public partial int Value { get; set; }

    [ObservableProperty]
    public partial bool IsGiven { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    public partial bool IsError { get; set; }

    public CellViewModel(int row, int column, int  value = 0, bool isGiven = false)
    {
        Row = row;
        Column = column;
        Value = value;
        IsGiven = isGiven;
        IsSelected = false;
        IsError = false;
    }
}