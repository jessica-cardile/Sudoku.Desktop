using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Sudoku.Engine;

namespace Sudoku.Engine.Tests
{
    public class BoardInitializationTests
    {
        [Fact]
        public void NewBoard_ShouldHave81Cells()
        {
            // Creating the board triggers the constructor
            var board = new SudokuBoard();

            //Assert (Verify the list count is exactly 81)
            Assert.Equal(81, board.Cells.Count);
        }
    }
}
