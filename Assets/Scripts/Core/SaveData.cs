using System;
using System.Collections.Generic;
using UnityEngine;

namespace SudokuGame
{
    [Serializable]
    public class ScoreEntry
    {
        public string nickname;
        public int score;
        public int difficulty;
        public float seconds;
        public int mistakes;
    }

    /// <summary>Nickname + leaderboard, stored as JSON in PlayerPrefs (works on Mac, Windows and Linux).</summary>
    [Serializable]
    public class SaveData
    {
        const string Key = "sudoku_save_v1";
        const int MaxEntries = 100;

        public string nickname = "";
        public List<ScoreEntry> scores = new List<ScoreEntry>();

        static SaveData current;

        public static SaveData Current => current ??= Load();

        static SaveData Load()
        {
            string json = PlayerPrefs.GetString(Key, "");
            if (string.IsNullOrEmpty(json)) return new SaveData();
            try { return JsonUtility.FromJson<SaveData>(json) ?? new SaveData(); }
            catch (Exception) { return new SaveData(); }
        }

        public void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }

        public void AddScore(ScoreEntry entry)
        {
            scores.Add(entry);
            scores.Sort((a, b) => b.score.CompareTo(a.score));
            if (scores.Count > MaxEntries) scores.RemoveRange(MaxEntries, scores.Count - MaxEntries);
            Save();
        }

        public int TotalPointsFor(string name)
        {
            int total = 0;
            foreach (var s in scores) if (s.nickname == name) total += s.score;
            return total;
        }
    }
}
