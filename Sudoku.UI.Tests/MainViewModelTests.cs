using System.Linq;
using Xunit;
using Sudoku.UI.ViewModels;

namespace Sudoku.UI.Tests;

public class MainViewModelTests
{
    [Fact]
    public void Constructor_ShouldInitialiseBoardWith81Cells()
    {
        var viewModel = new MainViewModel();
        Assert.NotNull(viewModel.Board);
        Assert.Equal(81, viewModel.Board.Count);
    }
    
}
