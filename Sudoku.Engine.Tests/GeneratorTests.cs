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

        [Fact]
        public void GeneratePuzzle_ShouldRetainFullyValidSolution()
        {
            var board = new SudokuBoard();
            board.GeneratePuzzle(cellsToEmpty: 40);

            // Reconstruct the retained solution into a fresh board and verify it's a
            // genuinely complete, duplicate-free Sudoku grid (not just zeros/garbage).
            var solutionBoard = new SudokuBoard();
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    solutionBoard.GetCell(row, col).Value = board.GetSolutionValue(row, col);
                }
            }

            Assert.True(solutionBoard.Cells.All(c => c.Value != 0));
            Assert.True(solutionBoard.IsBoardValid());

            // Starting clues must match the retained solution exactly.
            foreach (var cell in board.Cells.Where(c => c.isStartingClue))
            {
                Assert.Equal(cell.Value, board.GetSolutionValue(cell.Row, cell.Column));
            }
        }
    }
}
