using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// NEW FILE — Manages the Firebase auth bridge between the web portal and Unity.
///
/// Architecture:
///   Portal (React) ─── postMessage ───► FirebaseBridge.jslib ─── SendMessage ───► this script
///   this script     ─── DllImport  ───► FirebaseBridge.jslib ─── fetch()     ───► Firestore REST API
///
/// Setup:
///   1. Create an empty GameObject named exactly "FirebaseManager" in the scene.
///   2. Attach this script to it.
///   3. Make sure FirebaseBridge.jslib is at Assets/Plugins/WebGL/FirebaseBridge.jslib
///
/// The portal sends { uid, idToken, displayName, projectId } via postMessage
/// when the game iframe loads. This script receives it and stores the credentials.
/// When a game ends, ScoreManager calls FirebaseManager.Instance.SubmitScore()
/// to push the score to Firestore via the REST API.
/// </summary>
public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    // ── Auth state (populated by the portal via postMessage → jslib → SendMessage) ──
    public bool   IsAuthenticated { get; private set; } = false;
    public string UserId          { get; private set; } = "";
    public string DisplayName     { get; private set; } = "Player";
    public string IdToken         { get; private set; } = "";
    public string ProjectId       { get; private set; } = "";

    // ────────────────────────────────────────────────────────────────────────
    // JavaScript functions defined in FirebaseBridge.jslib
    // These are only available in WebGL builds; the #if guards prevent
    // compile errors in the Unity Editor.
    // ────────────────────────────────────────────────────────────────────────
    #if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void InitFirebaseBridge();
    [DllImport("__Internal")] private static extern void SubmitScoreToFirestore(string jsonBody);
    #else
    // Editor stubs — log calls so you can test the flow in Play mode
    private static void InitFirebaseBridge()
        => Debug.Log("[FirebaseManager] InitFirebaseBridge (editor stub)");
    private static void SubmitScoreToFirestore(string jsonBody)
        => Debug.Log($"[FirebaseManager] SubmitScoreToFirestore (editor stub): {jsonBody}");
    #endif

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Register the postMessage listener in the browser
        InitFirebaseBridge();
    }

    // ────────────────────────────────────────────────────────────────────────
    // Called from JavaScript (FirebaseBridge.jslib) via SendMessage.
    // The parameter is a JSON string with { uid, idToken, displayName, projectId }.
    // ────────────────────────────────────────────────────────────────────────
    public void OnAuthReceived(string json)
    {
        Debug.Log($"[FirebaseManager] Auth received: {json}");

        var data = JsonUtility.FromJson<AuthPayload>(json);
        UserId       = data.uid;
        IdToken      = data.idToken;
        DisplayName  = data.displayName;
        ProjectId    = data.projectId;
        IsAuthenticated = !string.IsNullOrEmpty(UserId) && !string.IsNullOrEmpty(IdToken);

        Debug.Log($"[FirebaseManager] Authenticated as {DisplayName} (uid: {UserId})");
    }

    // ────────────────────────────────────────────────────────────────────────
    // Public API — call this from ScoreManager when a game ends.
    // ────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Submits a score to Firestore via the REST API.
    /// Called by ScoreManager.SaveHighScore() after each game over.
    /// </summary>
    /// <param name="score">The score achieved this round.</param>
    /// <param name="pipes">Number of pipes passed.</param>
    /// <param name="duration">Session duration in seconds.</param>
    public void SubmitScore(int score, int pipes, int duration)
    {
        if (!IsAuthenticated)
        {
            Debug.LogWarning("[FirebaseManager] Not authenticated — score not submitted to cloud.");
            return;
        }

        // Build a simple JSON object that the jslib will parse and forward to Firestore
        var payload = new ScorePayload
        {
            score    = score,
            pipes    = pipes,
            duration = duration
        };

        string json = JsonUtility.ToJson(payload);
        Debug.Log($"[FirebaseManager] Submitting score: {json}");
        SubmitScoreToFirestore(json);
    }

    // ── Serializable helper classes for JsonUtility ─────────────────────────
    [System.Serializable]
    private class AuthPayload
    {
        public string uid;
        public string idToken;
        public string displayName;
        public string projectId;
    }

    [System.Serializable]
    private class ScorePayload
    {
        public int score;
        public int pipes;
        public int duration;
    }
}
