using Xunit;
using Sudoku.Engine;

namespace Sudoku.Engine.Tests
{
    public class GeneratorTests
    {
        [Fact]
        public void GeneratePuzzle_ShouldCreateValidPuzzle()
        {
            var board = new SudokuBoard();
            int cellsToEmpty = 40; // 40 is a medium difficulty level

            board.GeneratePuzzle(cellsToEmpty);

            //Verify cells with numbers are marked as starting clues
            int startingCluesCount = board.Cells.Count(c => c.isStartingClue);
            int emptyCellsCount = board.Cells.Count(c => c.Value == 0);
            Assert.True(emptyCellsCount > 0, "There should be empty cells in the generated puzzle.");
            Assert.Equal(81 - emptyCellsCount, startingCluesCount);

            //check we have only one solution for this board
            int solutionCount = board.CountSolutions();
            Assert.Equal(1, solutionCount);
        }
    }
}
