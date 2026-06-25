using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sudoku.Engine
{
    public class SudokuBoard
    {
        public List<Cell> Cells { get; set; }

        public SudokuBoard()
        {
            Cells = new List<Cell>();

            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    //create a single, unique cell
                    var newCell = new Cell
                    {
                        Row = r,
                        Column = c,
                        Value = 0 //where 0 is an empty cell
                    };

                    Cells.Add(newCell);
                }
            }
        }

        public Cell GetCell(int targetRow, int targetColumn)
        {
            foreach(var cell in Cells)
            {
                //Find the cell where coordinates match the target row and column
                if(cell.Row == targetRow && cell.Column == targetColumn)
                {
                    return cell;
                }
            }
            throw new InvalidOperationException($"Cell at Row {targetRow}, Column {targetColumn} does not exist inside the board collection.");
        }

        public bool isRowValid(int rowIndex)
        {
            if(rowIndex < 0 || rowIndex > 8)
            {
                throw new ArgumentOutOfRangeException(nameof(rowIndex), "Row index must be between 0 and 8.");
            }

            //Get all the 9 cells belonging to the specified row 
            var rowCells = Cells.Where(c => c.Row == rowIndex);

            //create a HashSet to track the numbers we have in this row
            var existingNumbers = new HashSet<int>();

            foreach (var cell in rowCells)
            {
                //skip if cell is empty
                if(cell.Value == 0)
                {
                    continue;
                }

                //Try adding the number to the HashSet, will return false if the number already exists
                bool isUnique = existingNumbers.Add(cell.Value);

                if(!isUnique)
                { 
                    return false; //we have a duplicate!
                }
            }
            return true; //no duplicates found, row is valid
        }

        public bool isColumnValid(int columnIndex)
        {
            if(columnIndex < 0 || columnIndex > 8)
            {
                throw new ArgumentOutOfRangeException(nameof(columnIndex), "Column index must be between 0 and 8.");
            }
            var  columnsCells = Cells.Where(c => c.Column == columnIndex);

            var existingNumbers = new HashSet<int>();

            foreach(var cell in columnsCells)
            {
                if(cell.Value == 0)
                {
                    continue;
                }

                bool isUnique = existingNumbers.Add(cell.Value);

                if(!isUnique)
                {
                    return false; //we have a duplicate!
                }
            }
            return true; //no duplicates found, column is valid
        }

        public bool isBoxValid(int boxIndex)
        {
            if(boxIndex < 0 || boxIndex > 8)
            {
                throw new ArgumentOutOfRangeException(nameof(boxIndex), "Box index must be between 0 and 8.");
            }

            //calculate boundaries(3*3 board)
            int startRow = (boxIndex / 3) * 3;
            int startColumn = (boxIndex % 3) * 3;

            //Find the 9 cells inside a specific box boundary
            var boxCells = Cells.Where
            (c => c.Row >= startRow && c.Row < startRow + 3 
            && c.Column >= startColumn && c.Column < startColumn + 3);

            var existingNumbers = new HashSet<int>();

            foreach(var cell in boxCells)
            {
                if(cell.Value == 0)
                {
                    continue;
                }

                bool isUnique = existingNumbers.Add(cell.Value);

                if (!isUnique)
                {
                    return false;
                }
            }
            return true;
        }

        public bool IsBoardValid()
        {
            for (int i = 0; i < 9; i++)
            {
                if (!isRowValid(i) || !isColumnValid(i) || !isBoxValid(i))
                {
                    return false;
                }
            }
            return true;
        }

        public bool SolvePuzzle()
        {
            //Find the first empty cell available
            Cell? nextEmptyCell = Cells.FirstOrDefault(c => c.Value == 0);

            //Base case: no empty cells, board solved!
            if (nextEmptyCell == null)
            {
                return true;
            }

            //empty cell found, try numbers 1 to 9
            for (int num = 1; num <= 9; num++)
            {
                //find where cell is in the board
                int boxIndex = (nextEmptyCell.Row / 3) * 3 + (nextEmptyCell.Column / 3);

                //try to place number
                nextEmptyCell.Value = num;

                //check if it breaks the board rules
                if (isRowValid(nextEmptyCell.Row) && isColumnValid(nextEmptyCell.Column) && isBoxValid(boxIndex))
                {
                    //recursively call again to try and solve the rest of the board
                    bool isPuzzleSolved = SolvePuzzle();

                    if (isPuzzleSolved)
                    {
                        return true;
                    }
                }

                //if we got here we need to backtrack the last number was wrong
                nextEmptyCell.Value = 0;
            }
            //if we got here, we tried all numbers and we need to backtrack further
            return false;
        }
    }
}
