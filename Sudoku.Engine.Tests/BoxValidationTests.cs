using Sudoku.Engine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Numerics;
using System.Text;
using Xunit;

namespace Sudoku.Engine.Tests
{
    public class BoxValidationTests
    {
        [Fact]
        public void BoxWithNoDuplicateNumbers_ShouldBeValid()
        {
            // Arrange
            var board = new SudokuBoard();

            //Box 4 covers Rows 3,4,5 and Columns 3,4,5
            board.GetCell(3, 3).Value = 9;
            board.GetCell(4, 4).Value = 4;
            board.GetCell(5, 5).Value = 2;

            // Act
            bool isValid = board.isBoxValid(4);

            // Assert
            Assert.True(isValid);
        }

        [Fact]
        public void BoxWithDuplicateNumbers_ShouldBeInvalid()
        {
            // Arrange
            var board = new SudokuBoard();

            // Intentionally place a duplicate '9' inside the boundaries of Box 4
            board.GetCell(3, 3).Value = 9;
            board.GetCell(5, 4).Value = 9; // Duplicate inside Box 4!

            // Act
            bool isValid = board.isBoxValid(4);
            // Assert
            Assert.False(isValid);
        }
    }
}
