using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Sudoku.Engine;

namespace Sudoku.Engine.Tests
{
    public class ColumnValidationTests
    {
        [Fact]
        public void ColumnWithNoDuplicateNumbers_ShouldBevalid()
        {
            //arrange
            var board = new SudokuBoard();

            board.GetCell(0, 3).Value = 1;
            board.GetCell(1, 3).Value = 4;
            board.GetCell(7, 3).Value = 8;

            //Act:
            bool isValid = board.isColumnValid(3);

            //Assert
            Assert.True(isValid);
        }

        [Fact]
        public void ColumnWithDuplicateNumbers_ShouldBeInvalid()
        {
            //arrange
            var board = new SudokuBoard();

            board.GetCell(0, 3).Value = 1;
            board.GetCell(1, 3).Value = 4;
            board.GetCell(7, 3).Value = 1; // Duplicate number

            //Act:
            bool isValid = board.isColumnValid(3);

            //Assert
            Assert.False(isValid);
        }
    }
}
