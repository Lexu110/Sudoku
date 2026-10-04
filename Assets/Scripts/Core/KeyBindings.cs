using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SudokuGame
{
    public enum GameAction { MoveUp, MoveDown, MoveLeft, MoveRight, Erase, Leave }

    /// <summary>
    /// Rebindable keys: every action has two slots (primary and alternate). Number keys 1-9 are fixed.
    /// Saved in PlayerPrefs as key names.
    /// </summary>
    public static class KeyBindings
    {
        public const int Slots = 2;
        public static readonly int ActionCount = Enum.GetValues(typeof(GameAction)).Length;

        const string PrefsKey = "sudoku_keys_v1";

        // Order matches GameAction, two slots each.
        static readonly Key[] Defaults =
        {
            Key.UpArrow, Key.W,
            Key.DownArrow, Key.S,
            Key.LeftArrow, Key.A,
            Key.RightArrow, Key.D,
            Key.Backspace, Key.Delete,
            Key.Escape, Key.None
        };

        [Serializable]
        class Store { public string[] keys; }

        static Key[] keys;
        static Key[] Keys => keys ??= Load();

        static int Index(GameAction action, int slot) => (int)action * Slots + slot;

        static Key[] Load()
        {
            var result = (Key[])Defaults.Clone();
            string json = PlayerPrefs.GetString(PrefsKey, "");
            if (string.IsNullOrEmpty(json)) return result;
            try
            {
                var store = JsonUtility.FromJson<Store>(json);
                if (store?.keys == null || store.keys.Length != result.Length) return result;
                for (int i = 0; i < result.Length; i++)
                    result[i] = Enum.TryParse(store.keys[i], out Key k) ? k : Key.None;
            }
            catch (Exception) { return (Key[])Defaults.Clone(); }
            return result;
        }

        static void Save()
        {
            var store = new Store { keys = new string[Keys.Length] };
            for (int i = 0; i < Keys.Length; i++) store.keys[i] = Keys[i].ToString();
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(store));
            PlayerPrefs.Save();
        }

        public static Key Get(GameAction action, int slot) => Keys[Index(action, slot)];

        /// <summary>Binds a key. If another slot already used it, that slot becomes empty.</summary>
        public static void Set(GameAction action, int slot, Key key)
        {
            if (key != Key.None)
                for (int i = 0; i < Keys.Length; i++)
                    if (Keys[i] == key) Keys[i] = Key.None;
            Keys[Index(action, slot)] = key;
            Save();
        }

        public static void ResetToDefaults()
        {
            keys = (Key[])Defaults.Clone();
            Save();
        }

        /// <summary>Number keys are used for entering digits and cannot be rebound.</summary>
        public static bool IsReserved(Key k)
        {
            return (k >= Key.Digit1 && k <= Key.Digit0) || (k >= Key.Numpad0 && k <= Key.Numpad9);
        }

        public static bool WasPressed(GameAction action)
        {
            var kb = Keyboard.current;
            if (kb == null) return false;
            for (int s = 0; s < Slots; s++)
            {
                var k = Keys[Index(action, s)];
                if (k != Key.None && kb[k].wasPressedThisFrame) return true;
            }
            return false;
        }

        public static string KeyName(Key k)
        {
            if (k == Key.None) return "-";
            var kb = Keyboard.current;
            return kb != null ? kb[k].displayName : k.ToString();
        }

        public static string ActionName(GameAction a)
        {
            switch (a)
            {
                case GameAction.MoveUp: return "Move Up";
                case GameAction.MoveDown: return "Move Down";
                case GameAction.MoveLeft: return "Move Left";
                case GameAction.MoveRight: return "Move Right";
                case GameAction.Erase: return "Erase";
                default: return "Leave Game";
            }
        }
    }
}
