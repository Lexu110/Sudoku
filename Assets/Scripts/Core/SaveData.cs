using System;
using UnityEngine;

namespace SudokuGame
{
    /// <summary>The logged-in player's username and JWT, stored in PlayerPrefs. The password is never stored.</summary>
    [Serializable]
    public class SaveData
    {
        const string Key = "sudoku_save_v2";
        const string OldKey = "sudoku_save_v1";

        public string username = "";
        public string token = "";

        static SaveData current;

        public static SaveData Current => current ??= Load();

        public bool HasSession => !string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(username);

        static SaveData Load()
        {
            // The old version kept a local-only nickname and leaderboard.
            if (PlayerPrefs.HasKey(OldKey)) PlayerPrefs.DeleteKey(OldKey);

            string json = PlayerPrefs.GetString(Key, "");
            if (string.IsNullOrEmpty(json)) return new SaveData();
            try { return JsonUtility.FromJson<SaveData>(json) ?? new SaveData(); }
            catch (Exception) { return new SaveData(); }
        }

        void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(this));
            PlayerPrefs.Save();
        }

        public void SetSession(string user, string accessToken)
        {
            username = user;
            token = accessToken;
            Save();
        }

        public void ClearSession()
        {
            username = "";
            token = "";
            Save();
        }
    }
}
