using System;
using System.Collections.Generic;
using System.Text;

namespace Sudoku.Engine
{
    public class Cell
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public bool isStartingClue { get; set; }
        private int _value;
        public int Value
        {
            get => _value;
            set
            {
                if (value < 0 || value > 9)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Cell value must be between 0 and 9!");
                }
                _value = value;
            }
        }
    }
}