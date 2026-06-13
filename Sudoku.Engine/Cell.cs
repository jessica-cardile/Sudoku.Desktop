using System;
using System.Collections.Generic;
using System.Text;

namespace Sudoku.Engine
{
    public class Cell
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public int Value { get; set; }
    }
}