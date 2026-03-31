/*
 * FirebaseBridge.jslib
 * ═══════════════════════════════════════════════════════════════════════
 * NEW FILE — JavaScript plugin that runs inside the Unity WebGL build.
 *
 * This file is the browser-side half of the postMessage ↔ Unity bridge.
 *
 * What it does:
 *   1. Listens for "firebase-auth" messages from the parent portal page.
 *   2. Forwards the auth payload (uid, idToken, displayName, projectId)
 *      into the C# world via SendMessage → FirebaseManager.
 *   3. Exposes SubmitScoreToFirestore() so C# can call the Firestore
 *      REST API directly from JavaScript (avoids CORS issues that
 *      UnityWebRequest sometimes has with Google APIs).
 *
 * Place this file at:  Assets/Plugins/WebGL/FirebaseBridge.jslib
 * ═══════════════════════════════════════════════════════════════════════
 */
var FirebaseBridgeLib = {

    /* ──────────────────────────────────────────────────────────────────────
     * InitFirebaseBridge
     * Called once from C# (FirebaseManager.Start) to register the
     * window.addEventListener("message", ...) listener.
     * ────────────────────────────────────────────────────────────────────── */
    InitFirebaseBridge: function () {
        if (window.__firebaseBridgeInit) return;   // guard against double-init
        window.__firebaseBridgeInit = true;

        // Store credentials received from the portal
        window.__fbAuth = { uid: null, idToken: null, displayName: null, projectId: null };

        window.addEventListener("message", function (event) {
            // Accept messages from any origin (the portal and the game are
            // on different domains: your-site vs farrasarias.github.io).
            var data = event.data;
            if (!data || data.type !== "firebase-auth") return;

            console.log("[FirebaseBridge] Received auth from portal for uid:", data.uid);

            window.__fbAuth.uid         = data.uid;
            window.__fbAuth.idToken     = data.idToken;
            window.__fbAuth.displayName = data.displayName || "Player";
            window.__fbAuth.projectId   = data.projectId   || "";

            // Forward to C# — FirebaseManager.OnAuthReceived(string json)
            var payload = JSON.stringify(window.__fbAuth);

            // SendMessage(gameObjectName, methodName, stringParam)
            SendMessage("FirebaseManager", "OnAuthReceived", payload);
        });

        console.log("[FirebaseBridge] Listener registered — waiting for auth from portal.");
    },


    /* ──────────────────────────────────────────────────────────────────────
     * SubmitScoreToFirestore
     * Called from C# after each game-over to write a score document to
     * the Firestore REST API.
     *
     * Parameters (all passed as char* from C#):
     *   jsonBody — The full Firestore REST API document body as a JSON string.
     *
     * The REST endpoint:
     *   POST https://firestore.googleapis.com/v1/
     *        projects/{projectId}/databases/(default)/documents/scores
     *
     * We also PATCH the user's profile document to update highScore
     * and gamesPlayed.
     * ────────────────────────────────────────────────────────────────────── */
    SubmitScoreToFirestore: function (jsonBodyPtr) {
        var jsonBody = UTF8ToString(jsonBodyPtr);
        var parsed   = JSON.parse(jsonBody);

        var auth      = window.__fbAuth;
        if (!auth || !auth.idToken || !auth.projectId) {
            console.warn("[FirebaseBridge] No auth — score not submitted.");
            return;
        }

        var baseUrl = "https://firestore.googleapis.com/v1/projects/"
                    + auth.projectId
                    + "/databases/(default)/documents";

        var headers = {
            "Content-Type":  "application/json",
            "Authorization": "Bearer " + auth.idToken
        };

        /* ── 1. Create a new document in the "scores" collection ───────── */
        var scoreDoc = {
            fields: {
                userId:    { stringValue:  auth.uid },
                score:     { integerValue: String(parsed.score) },
                pipes:     { integerValue: String(parsed.pipes) },
                duration:  { integerValue: String(parsed.duration) },
                timestamp: { timestampValue: new Date().toISOString() }
            }
        };

        fetch(baseUrl + "/scores", {
            method:  "POST",
            headers: headers,
            body:    JSON.stringify(scoreDoc)
        })
        .then(function (r) { return r.json(); })
        .then(function (d) { console.log("[FirebaseBridge] Score saved:", d.name); })
        .catch(function (e) { console.error("[FirebaseBridge] Score POST failed:", e); });


        /* ── 2. Update the user's profile (highScore, gamesPlayed) ─────── */
        // We need the current values first, so GET then PATCH.
        var userDocUrl = baseUrl + "/users/" + auth.uid;

        fetch(userDocUrl, { method: "GET", headers: headers })
        .then(function (r) { return r.json(); })
        .then(function (doc) {
            var currentHigh  = 0;
            var currentGames = 0;

            if (doc.fields) {
                if (doc.fields.highScore)    currentHigh  = parseInt(doc.fields.highScore.integerValue    || "0");
                if (doc.fields.gamesPlayed)  currentGames = parseInt(doc.fields.gamesPlayed.integerValue  || "0");
            }

            var newHigh  = Math.max(currentHigh, parsed.score);
            var newGames = currentGames + 1;

            var patchBody = {
                fields: {
                    highScore:    { integerValue: String(newHigh) },
                    gamesPlayed:  { integerValue: String(newGames) }
                }
            };

            return fetch(userDocUrl + "?updateMask.fieldPaths=highScore&updateMask.fieldPaths=gamesPlayed", {
                method:  "PATCH",
                headers: headers,
                body:    JSON.stringify(patchBody)
            });
        })
        .then(function (r) { return r.json(); })
        .then(function (d) { console.log("[FirebaseBridge] User profile updated."); })
        .catch(function (e) { console.error("[FirebaseBridge] User PATCH failed:", e); });
    }
};

mergeInto(LibraryManager.library, FirebaseBridgeLib);
