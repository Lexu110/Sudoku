---
name: sudoku-backend
description: How to call the Sudoku backend API from the Unity game — registering and logging in users, storing the JWT, submitting scores and reading leaderboards with UnityWebRequest. Use when the user asks about the Sudoku backend, login/registration, score submission, leaderboards, or networking code in the Unity Sudoku project.
---

## Overview

REST/JSON API (FastAPI) at `https://sudoku.lcorream.com/api/v1`, served over HTTPS via Traefik. Ask the user for the production host if it isn't already defined in the Unity project; keep it in a single config constant.
Interactive docs: `https://sudoku.lcorream.com/docs`.

Authentication: log in once, keep the `access_token` (JWT, valid 30 days by default), and send `Authorization: Bearer <token>` on protected calls. There is no refresh endpoint: on any `401`, discard the token and send the player back to login.

## Endpoints

| Method | Path | Auth | Body / query | Success response |
|---|---|---|---|---|
| POST | `/auth/register` | no | `{"username","password"}` | 201 `{"id","username","created_at"}` |
| POST | `/auth/login` | no | `{"username","password"}` | 200 `{"access_token","token_type"}` |
| GET | `/auth/me` | yes | - | 200 user |
| POST | `/scores` | yes | `{"difficulty","time_seconds","mistakes"}` | 201 `{"id","difficulty","score","time_seconds","mistakes","created_at"}` |
| GET | `/scores/me` | yes | `?difficulty=&limit=&offset=` | 200 array of scores, newest first |
| GET | `/leaderboard` | no | `?difficulty=&limit=&offset=` | 200 array of `{"rank","username","score"}` |
| GET | `/leaderboard/me` | yes | `?difficulty=` | 200 `{"rank","username","score"}`, 404 if the player has no scores |

## Rules and constraints

- `username`: 3-20 characters, only `A-Za-z0-9_`, unique case-insensitively. Validate client-side to avoid round trips.
- `password`: 8-128 characters.
- `difficulty` must be lowercase: `easy`, `medium`, `hard`, `expert`. Serialize the C# enum as a lowercase string, not an int.
- `time_seconds`: integer 20-86400 (lower values get 422). `mistakes`: integer 0-1000.
- The server computes the score. Never send a score; display the `score` from the response.
- `limit` max is 100 (leaderboard default 10, scores default 20).
- Leaderboard rank is competition ranking: tied scores share a rank. Leaderboard without `difficulty` ranks each user's best single score across all difficulties.
- Traefik rate-limits at about 20 req/s; do not poll the leaderboard in a tight loop. Fetch on screen open or on a manual refresh.

## Error handling

| Status | Meaning | Client action |
|---|---|---|
| 401 | Bad credentials (login) or missing/expired token | Clear the stored token, show login |
| 404 | `/leaderboard/me` with no scores yet | Show "no rank yet" |
| 409 | Username taken (register) | Ask for another username |
| 422 | Validation failed (body has `detail` array) | Fix input; indicates a client bug for score fields |
| 429 | Rate limited | Back off and retry later |
| 5xx / network error | Backend unavailable | Retry with backoff; queue unsent scores locally |

Error bodies look like `{"detail": "message"}` (or a list for 422).

## Unity implementation notes

- Use `UnityWebRequest` with a coroutine or async wrapper. For JSON POST, send raw bytes: `new UploadHandlerRaw(Encoding.UTF8.GetBytes(json))`, `new DownloadHandlerBuffer()`, and header `Content-Type: application/json`. Do not use `UnityWebRequest.Post(url, string)` for JSON; it form-encodes.
- `JsonUtility` cannot parse a top-level JSON array (the leaderboard and score history responses). Either wrap it (`{"items":` + raw + `}`) before `JsonUtility.FromJson`, or use Newtonsoft (`com.unity.nuget.newtonsoft-json`). `JsonUtility` also needs `[Serializable]` classes with public fields named exactly like the JSON keys (snake_case, e.g. `access_token`, `time_seconds`).
- Check `request.result == UnityWebRequest.Result.Success`; for HTTP errors (`ProtocolError`) read `request.responseCode` and `request.downloadHandler.text`.
- Store the token in `PlayerPrefs` at minimum (or a platform secure store). Never log the token or the password.
- Set a request `timeout` (e.g. 10 s).
- Submit a score once when a puzzle is completed. Measure `time_seconds` with a pause-aware timer and count `mistakes` as wrong entries the player made.
- For WebGL builds the backend currently sends no CORS headers; browser requests will fail until CORS is enabled on the server.

## Example C# (minimal)

```csharp
[Serializable] class LoginRequest { public string username; public string password; }
[Serializable] class TokenResponse { public string access_token; public string token_type; }
[Serializable] class ScoreSubmit { public string difficulty; public int time_seconds; public int mistakes; }

IEnumerator Login(string user, string pass, Action<string> onToken, Action<long, string> onError)
{
    var body = JsonUtility.ToJson(new LoginRequest { username = user, password = pass });
    using var req = new UnityWebRequest($"{BaseUrl}/auth/login", "POST");
    req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
    req.downloadHandler = new DownloadHandlerBuffer();
    req.SetRequestHeader("Content-Type", "application/json");
    req.timeout = 10;
    yield return req.SendWebRequest();

    if (req.result == UnityWebRequest.Result.Success)
        onToken(JsonUtility.FromJson<TokenResponse>(req.downloadHandler.text).access_token);
    else
        onError(req.responseCode, req.downloadHandler.text);
}

// Protected call: add the header before sending.
// req.SetRequestHeader("Authorization", "Bearer " + token);
```
