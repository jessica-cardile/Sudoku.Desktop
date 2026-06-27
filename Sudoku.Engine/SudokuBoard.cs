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

        public bool SolvePuzzle(bool useRandomisation = false)
        {
            //Find the first empty cell available
            Cell? nextEmptyCell = Cells.FirstOrDefault(c => c.Value == 0);

            //Base case: no empty cells, board solved!
            if (nextEmptyCell == null)
            {
                return true;
            }

            IEnumerable<int> numberSequence = Enumerable.Range(1, 9);

            // Update this block inside SolvePuzzle:
            if (useRandomisation)
            {
                numberSequence = numberSequence.OrderBy(x => Random.Shared.Next());
            }

            //empty cell found, try numbers 1 to 9
            foreach (int num in numberSequence)
            {
                //find where cell is in the board
                int boxIndex = (nextEmptyCell.Row / 3) * 3 + (nextEmptyCell.Column / 3);

                //try to place number
                nextEmptyCell.Value = num;

                //check if it breaks the board rules
                if (isRowValid(nextEmptyCell.Row) && isColumnValid(nextEmptyCell.Column) && isBoxValid(boxIndex))
                {
                    //recursively call again to try and solve the rest of the board
                    bool isPuzzleSolved = SolvePuzzle(useRandomisation);

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

        public int CountSolutions(int maxCount = 2)
        {
            Cell? nextEmptyCell = Cells.FirstOrDefault(c => c.Value == 0);

            // base case no empty cells left, we have a solution
            if (nextEmptyCell == null)
            {
                return 1;
            }

            int totalSolutionsFound = 0;

            for (int num = 1; num <= 9; num++)
            {
                nextEmptyCell.Value = num;

                int boxIndex = (nextEmptyCell.Row / 3) * 3 + (nextEmptyCell.Column / 3);

                if (isRowValid(nextEmptyCell.Row) &&
                    isColumnValid(nextEmptyCell.Column) &&
                    isBoxValid(boxIndex))
                {
                    // Recursive case: we have a solution,we want to count if there are more than 1
                    totalSolutionsFound += CountSolutions(maxCount);

                    // solution is not unique, puzzle is broken!
                    if (totalSolutionsFound >= maxCount)
                    {
                        nextEmptyCell.Value = 0; //important! Clean up the cell before returning otherwise we carry stale data into the next recursive call
                        return totalSolutionsFound;
                    }
                }
                nextEmptyCell.Value = 0;
            }
            return totalSolutionsFound;
        }

        public void GeneratePuzzle(int cellsToEmpty)
        {
            //complete cleanup of the board
            foreach (var cell in Cells)
            {
                cell.Value = 0;
                cell.isStartingClue = false;
            }

            SolvePuzzle(useRandomisation: true);
            
            //creates a shuffled list of all cells in the board
            var cellSequence = Enumerable.Range(0, 81).OrderBy(x => Random.Shared.Next()).ToList();

            int cellsEmptiedCounter = 0;

            //create empty cells in the board
            foreach (int cellIndex in cellSequence)
            {
                if(cellsEmptiedCounter >= cellsToEmpty)
                {
                    break;
                }

                Cell targetCell = Cells[cellIndex];
                int originalCellValue = targetCell.Value;
                //temporarily empty the cell
                targetCell.Value = 0;

                //check if the board has still a unique solution
                if (CountSolutions() == 1)
                {
                    cellsEmptiedCounter++;
                }
                else
                {
                    //put original value back in cause we broken the board rules
                    targetCell.Value = originalCellValue;
                }
            }

            //marks all remaining numbers in the board as permanent starting clues
            foreach (var cell in Cells)
            {
                if (cell.Value != 0)
                {
                    cell.isStartingClue = true;
                }
            }
        }
    }
}  
