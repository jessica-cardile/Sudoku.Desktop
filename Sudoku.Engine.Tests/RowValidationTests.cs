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

        [Fact]
        public void RowWithDuplicateNumbers_ShouldBeInvalid()
        {
            // Arrange
            var board = new SudokuBoard();

            board.GetCell(0, 0).Value = 5;
            board.GetCell(0, 1).Value = 3;
            board.GetCell(0, 2).Value = 5; // Duplicate number

            //Act:
            bool isvalid = board.isRowValid(0);

            // Assert
            Assert.False(isvalid);
        }
    }
}
