using System;
using System.Collections.Generic;
using System.Text;

namespace Sudoku.Engine.Tests
{
    public class RowValidationTests
    {
        [Fact]
        public void RowWithNoDuplicateNumbers_ShouldBeValid()
        {
            // Arrange
            var board = new SudokuBoard();

            board.GetCell(0, 0).Value = 5;
            board.GetCell(0, 1).Value = 3;
            board.GetCell(0, 2).Value = 9;

            //Act:
            bool isvalid = board.isRowValid(0);

            // Assert
            Assert.True(isvalid);
        }

        [Theory]
        [InlineData(5, 0, 1)]
        [InlineData(9, 0, 8)]
        [InlineData(3, 4, 5)]
        [InlineData(1, 2, 6)]
        public void RowWithVariousDuplicateNumbers_ShouldAlwaysBeInvalid(int duplicateValue, int col1, int col2)
        {
            // Arrange
            var board = new SudokuBoard();

            board.GetCell(0, col1).Value = duplicateValue;
            board.GetCell(0, col2).Value = duplicateValue;

            // Act
            bool isvalid = board.isRowValid(0);

            // Assert
            Assert.False(isvalid);
        }
    }
}
