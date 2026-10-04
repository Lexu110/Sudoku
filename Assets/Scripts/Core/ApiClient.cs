using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SudokuGame
{
    [Serializable] public class ScoreOut { public int id; public string difficulty; public int score; public int time_seconds; public int mistakes; public string created_at; }
    [Serializable] public class LeaderboardEntry { public int rank; public string username; public int score; }
    [Serializable] class LeaderboardList { public LeaderboardEntry[] items; }
    [Serializable] class Credentials { public string username; public string password; }
    [Serializable] class TokenResponse { public string access_token; public string token_type; }
    [Serializable] class UserOut { public int id; public string username; }
    [Serializable] class ScoreSubmit { public string difficulty; public int time_seconds; public int mistakes; }

    public enum SessionState { Valid, Expired, Offline }

    public class ApiResult<T>
    {
        public T data;
        public string error;
        public bool sessionExpired;
        public bool Ok => error == null;
    }

    struct ApiResponse
    {
        public bool ok;
        public long status;
        public string text;
        public string error;
        public bool sessionExpired;
    }

    /// <summary>Talks to the Sudoku backend (see .github/skills/sudoku-backend/SKILL.md). Every call reports back through a callback.</summary>
    public class ApiClient : MonoBehaviour
    {
        public const string BaseUrl = "https://sudoku.lcorream.com/api/v1";

        // The server rejects anything faster than this.
        const int MinSeconds = 20;
        const int MaxSeconds = 86400;

        public static ApiClient Instance { get; private set; }

        void Awake() { Instance = this; }

        // ---------- Public calls ----------

        /// <summary>Creates the account, then logs in. done receives null on success or an error message.</summary>
        public void Register(string username, string password, Action<string> done)
        {
            var body = JsonUtility.ToJson(new Credentials { username = username, password = password });
            StartCoroutine(Send("POST", "/auth/register", body, false, r =>
            {
                if (!r.ok) done(r.error);
                else Login(username, password, done);
            }));
        }

        public void Login(string username, string password, Action<string> done)
        {
            var body = JsonUtility.ToJson(new Credentials { username = username, password = password });
            StartCoroutine(Send("POST", "/auth/login", body, false, r =>
            {
                if (!r.ok) { done(r.error); return; }
                var token = Parse<TokenResponse>(r.text);
                if (token == null || string.IsNullOrEmpty(token.access_token)) { done("Unexpected response from the server."); return; }
                SaveData.Current.SetSession(username, token.access_token);
                done(null);
            }));
        }

        /// <summary>Checks the stored token. Offline means the server could not be reached, so the player can still continue.</summary>
        public void CheckSession(Action<SessionState> done)
        {
            StartCoroutine(Send("GET", "/auth/me", null, true, r =>
            {
                if (r.ok)
                {
                    var user = Parse<UserOut>(r.text);
                    if (user != null && !string.IsNullOrEmpty(user.username))
                        SaveData.Current.SetSession(user.username, SaveData.Current.token);
                    done(SessionState.Valid);
                }
                else done(r.sessionExpired ? SessionState.Expired : SessionState.Offline);
            }));
        }

        /// <summary>The server calculates the score; the game only reports difficulty, time and mistakes.</summary>
        public void SubmitScore(Difficulty difficulty, float seconds, int mistakes, Action<ApiResult<ScoreOut>> done)
        {
            var body = JsonUtility.ToJson(new ScoreSubmit
            {
                difficulty = difficulty.ToString().ToLowerInvariant(),
                time_seconds = Mathf.Clamp(Mathf.RoundToInt(seconds), MinSeconds, MaxSeconds),
                mistakes = Mathf.Clamp(mistakes, 0, 1000)
            });
            StartCoroutine(Send("POST", "/scores", body, true, r => done(ToResult(r, Parse<ScoreOut>))));
        }

        /// <param name="difficulty">null ranks each player's best score across all difficulties.</param>
        public void GetLeaderboard(Difficulty? difficulty, int limit, Action<ApiResult<LeaderboardEntry[]>> done)
        {
            string path = $"/leaderboard?limit={limit}" + DifficultyQuery(difficulty, "&");
            StartCoroutine(Send("GET", path, null, false, r => done(ToResult(r, text =>
                JsonUtility.FromJson<LeaderboardList>("{\"items\":" + text + "}").items))));
        }

        /// <summary>A successful result with null data means the player has no score yet.</summary>
        public void GetMyRank(Difficulty? difficulty, Action<ApiResult<LeaderboardEntry>> done)
        {
            string path = "/leaderboard/me" + DifficultyQuery(difficulty, "?");
            StartCoroutine(Send("GET", path, null, true, r =>
            {
                if (r.status == 404) done(new ApiResult<LeaderboardEntry>());
                else done(ToResult(r, Parse<LeaderboardEntry>));
            }));
        }

        // ---------- Internals ----------

        static string DifficultyQuery(Difficulty? d, string prefix)
        {
            return d.HasValue ? prefix + "difficulty=" + d.Value.ToString().ToLowerInvariant() : "";
        }

        // Returns null for a body that is not valid JSON, so callers can treat it as an unexpected response.
        static T Parse<T>(string text) where T : class
        {
            try { return JsonUtility.FromJson<T>(text); }
            catch (Exception) { return null; }
        }

        static ApiResult<T> ToResult<T>(ApiResponse r, Func<string, T> parse)
        {
            if (!r.ok) return new ApiResult<T> { error = r.error, sessionExpired = r.sessionExpired };
            try
            {
                var data = parse(r.text);
                if (data == null) return new ApiResult<T> { error = "Unexpected response from the server." };
                return new ApiResult<T> { data = data };
            }
            catch (Exception)
            {
                return new ApiResult<T> { error = "Unexpected response from the server." };
            }
        }

        IEnumerator Send(string method, string path, string jsonBody, bool authenticated, Action<ApiResponse> done)
        {
            using var request = new UnityWebRequest(BaseUrl + path, method);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 10;
            if (jsonBody != null)
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
                request.SetRequestHeader("Content-Type", "application/json");
            }
            if (authenticated && SaveData.Current.HasSession)
                request.SetRequestHeader("Authorization", "Bearer " + SaveData.Current.token);

            yield return request.SendWebRequest();

            var response = new ApiResponse
            {
                ok = request.result == UnityWebRequest.Result.Success,
                status = request.responseCode,
                text = request.downloadHandler.text
            };
            if (!response.ok) response.error = Describe(request.result, request.responseCode, authenticated);

            if (authenticated && request.responseCode == 401)
            {
                response.sessionExpired = true;
                SaveData.Current.ClearSession();
            }
            done(response);
        }

        static string Describe(UnityWebRequest.Result result, long status, bool authenticated)
        {
            if (result != UnityWebRequest.Result.ProtocolError || status == 0)
                return "Can't reach the server. Check your connection.";
            switch (status)
            {
                case 401: return authenticated ? "Your session expired. Please log in again." : "Wrong username or password.";
                case 409: return "That username is already taken.";
                case 422: return "The server rejected that input. Check the username and password.";
                case 429: return "Too many requests. Wait a moment and try again.";
                default: return status >= 500 ? "The server is having problems. Try again later." : $"Unexpected error ({status}).";
            }
        }
    }
}
