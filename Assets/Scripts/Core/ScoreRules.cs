using UnityEngine;

namespace SudokuGame
{
    /// <summary>
    /// Score = starting points - time penalty - mistake penalty (never below 0).
    /// Auto-solve forfeits everything, so it never reaches this class.
    /// </summary>
    public static class ScoreRules
    {
        public static int StartingPoints(Difficulty d) => 1000 * ((int)d + 1);
        public static int PointsLostPerSecond(Difficulty d) => (int)d + 1;
        public static int PointsLostPerMistake(Difficulty d) => 50 * ((int)d + 1);

        public static int TimePenalty(Difficulty d, float seconds) => Mathf.FloorToInt(seconds * PointsLostPerSecond(d));
        public static int MistakePenalty(Difficulty d, int mistakes) => mistakes * PointsLostPerMistake(d);

        public static int Calculate(Difficulty d, float seconds, int mistakes)
        {
            return Mathf.Max(0, StartingPoints(d) - TimePenalty(d, seconds) - MistakePenalty(d, mistakes));
        }

        public static string FormatTime(float seconds)
        {
            int total = Mathf.FloorToInt(seconds);
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
