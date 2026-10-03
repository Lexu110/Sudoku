using System;

namespace SudokuGame
{
    public enum Difficulty { Easy = 0, Medium = 1, Hard = 2 }

    /// <summary>A generated puzzle: Givens are the visible clues, Solution is the full answer. Index = row * 9 + col.</summary>
    public class SudokuPuzzle
    {
        public readonly int[] Givens = new int[81];
        public readonly int[] Solution = new int[81];

        static int CluesFor(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Easy: return 40;
                case Difficulty.Medium: return 32;
                default: return 26;
            }
        }

        public static SudokuPuzzle Generate(Difficulty difficulty)
        {
            var rng = new Random();
            var puzzle = new SudokuPuzzle();

            var grid = new int[81];
            Fill(grid, rng);
            Array.Copy(grid, puzzle.Solution, 81);

            // Remove cells in random order, keeping the puzzle uniquely solvable.
            var order = new int[81];
            for (int i = 0; i < 81; i++) order[i] = i;
            for (int i = 80; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            int clues = 81;
            int target = CluesFor(difficulty);
            foreach (int idx in order)
            {
                if (clues <= target) break;
                int backup = grid[idx];
                grid[idx] = 0;
                if (CountSolutions(grid, 2) != 1) grid[idx] = backup;
                else clues--;
            }

            Array.Copy(grid, puzzle.Givens, 81);
            return puzzle;
        }

        // Bit v (1..9) set means value v is still allowed in that cell.
        static int Candidates(int[] g, int idx)
        {
            int r = idx / 9, c = idx % 9;
            int used = 0;
            for (int i = 0; i < 9; i++)
            {
                used |= 1 << g[r * 9 + i];
                used |= 1 << g[i * 9 + c];
            }
            int br = r / 3 * 3, bc = c / 3 * 3;
            for (int dr = 0; dr < 3; dr++)
                for (int dc = 0; dc < 3; dc++)
                    used |= 1 << g[(br + dr) * 9 + bc + dc];
            return ~used & 0x3FE;
        }

        static int BitCount(int x)
        {
            int n = 0;
            while (x != 0) { x &= x - 1; n++; }
            return n;
        }

        // Picks the empty cell with the fewest candidates. Returns -1 if the grid is full, -2 if a cell has no candidates.
        static int PickCell(int[] g, out int mask)
        {
            int best = -1, bestCount = 10;
            mask = 0;
            for (int i = 0; i < 81; i++)
            {
                if (g[i] != 0) continue;
                int m = Candidates(g, i);
                int n = BitCount(m);
                if (n == 0) return -2;
                if (n < bestCount) { best = i; bestCount = n; mask = m; }
            }
            return best;
        }

        static bool Fill(int[] g, Random rng)
        {
            int cell = PickCell(g, out int mask);
            if (cell == -2) return false;
            if (cell == -1) return true;

            var values = new int[9];
            int count = 0;
            for (int v = 1; v <= 9; v++) if ((mask & (1 << v)) != 0) values[count++] = v;
            for (int i = count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }

            for (int i = 0; i < count; i++)
            {
                g[cell] = values[i];
                if (Fill(g, rng)) return true;
            }
            g[cell] = 0;
            return false;
        }

        static int CountSolutions(int[] g, int limit)
        {
            int cell = PickCell(g, out int mask);
            if (cell == -2) return 0;
            if (cell == -1) return 1;

            int found = 0;
            for (int v = 1; v <= 9 && found < limit; v++)
            {
                if ((mask & (1 << v)) == 0) continue;
                g[cell] = v;
                found += CountSolutions(g, limit - found);
            }
            g[cell] = 0;
            return found;
        }
    }
}
