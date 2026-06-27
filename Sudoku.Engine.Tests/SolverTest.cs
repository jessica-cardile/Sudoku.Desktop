using Xunit;
using Sudoku.Engine;

namespace Sudoku.Engine.Tests
{
    public class SolverTest
    {
        [Fact]
        public void Solve_WithValidIncompleteBoard_ShouldReturnTrueAndFillBoard()
        {
            // Arrange
            var board = new SudokuBoard();
       
            board.GetCell(0, 0).Value = 5;
            board.GetCell(0, 1).Value = 3;
            board.GetCell(1, 0).Value = 6;
            board.GetCell(2, 1).Value = 9;
            board.GetCell(2, 2).Value = 8;

            // Act
            bool success = board.SolvePuzzle();

            // Assert
            Assert.True(success); // The solver should report success

            // We should not have any empty cells left
            foreach (var cell in board.Cells)
            {
                Assert.True(cell.Value > 0, $"Cell at Row {cell.Row}, Col {cell.Column} was left empty!");
            }

            Assert.True(board.IsBoardValid());
        }

        [Fact]
        public void Solve_WithImpossibleBoard_ShouldReturnFalse()
        {
            // Arrange
            var board = new SudokuBoard();

            board.GetCell(0, 0).Value = 1;
            board.GetCell(0, 1).Value = 2;
            // (0,2) is empty
            board.GetCell(0, 3).Value = 4;
            board.GetCell(0, 4).Value = 5;
            board.GetCell(0, 5).Value = 6;
            board.GetCell(0, 6).Value = 7;
            board.GetCell(0, 7).Value = 8;
            board.GetCell(0, 8).Value = 9;

            //number 3 blocked in row 0, column 2 by number 3 in column 2, row 1
            board.GetCell(1, 2).Value = 3;

            // Row 0 is now missing a 3 at index 2, but Column 2 already has a 3.
            // Cell (0,2) has zero options left!

            // Act
            bool success = board.SolvePuzzle();

            // Assert
            Assert.False(success);
        }

        [Fact]
        public void SolveWithRandomisation_ShouldGenerateUniqueBoardsEachTime()
        {
            var board1 = new SudokuBoard();
            var board2 = new SudokuBoard();

            board1.SolvePuzzle(useRandomisation: true);
            board2.SolvePuzzle(useRandomisation: true);

            int identicalCellsCounter = 0;
            for(int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    if (board1.GetCell(i, j).Value == board2.GetCell(i, j).Value)
                    {
                        identicalCellsCounter++;
                    }
                }
            }
            Assert.True(identicalCellsCounter < 81, "Two identical boards generated despite the randomisation");
        }
    }
}
