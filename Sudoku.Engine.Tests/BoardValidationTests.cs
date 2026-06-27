using Xunit;
using Sudoku.Engine;

namespace Sudoku.Engine.Tests
{
    public class BoardValidationTests
    {
        [Fact]
        public void NewBlankBoard_ShouldBeValid()
        { 
            //Arrange
            var board = new SudokuBoard();

            //Act
            bool isValid = board.IsBoardValid();

            //Assert
            Assert.True(isValid);
        }

        [Fact]
        public void BoardWithConflict_ShouldBeInvalid()
        {
            //Arrange
            var board = new SudokuBoard();

            board.GetCell(0, 0).Value = 5;
            board.GetCell(0, 8).Value = 5;

            //Act
            bool isValid = board.IsBoardValid();

            //Assert
            Assert.False(isValid);
        }

        [Fact]
        public void SettingCellToInvalidValue_ShouldThrowException()
        {
            //Arrange
            var board = new SudokuBoard();
            var cell = board.GetCell(0, 0);

            Assert.Throws<ArgumentOutOfRangeException>(() => cell.Value = 10);
        }
    }
}
