using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Sudoku.Engine;

namespace Sudoku.Engine.Tests
{
    public class BoardLookupTests
    {
        [Fact]
        public void GetCell_ShouldReturnCorrectCell()
        {
            //Arrange: set up new board to test
            var board = new SudokuBoard();
            int expectedRow = 4;
            int expectedColumn = 7;

            //Act: find the specifi cell
            Cell resultCell = board.GetCell(expectedRow, expectedColumn);

            //Assert: verify the cell returned is the correct one
            Assert.NotNull(resultCell);
            Assert.Equal(expectedRow, resultCell.Row);
            Assert.Equal(expectedColumn, resultCell.Column);
        }
    }
}
