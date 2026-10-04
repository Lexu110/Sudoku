using UnityEngine;

namespace SudokuGame
{
    public static class TimeFormat
    {
        public static string Format(float seconds)
        {
            int total = Mathf.FloorToInt(seconds);
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
