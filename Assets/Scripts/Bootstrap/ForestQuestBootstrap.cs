using UnityEngine;
using UnityEngine.EventSystems;

namespace ForestFriendsQuest
{
    /// <summary>
    /// ForestQuestBootstrap — the absolute entry point for Forest Friends Quest.
    ///
    /// Called before any scene loads via [RuntimeInitializeOnLoadMethod].
    /// Creates the root GameObject, attaches both ForestQuestApp and
    /// ForestSystemsContainer, and wires the full initialization chain.
    ///
    /// Order:
    ///   1. Camera (if missing)
    ///   2. EventSystem (if missing)
    ///   3. Root GameObject (DontDestroyOnLoad)
    ///      ├── ForestSystemsContainer.InitializeAll()  ← all 19 systems
    ///      └── ForestQuestApp                          ← UI + game loop
    /// </summary>
    public static class ForestQuestBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            // Global render settings
            Application.targetFrameRate       = 60;
            QualitySettings.vSyncCount        = 0;
            Application.runInBackground       = false;

            // Prevent multiple initializations if scene reloads
            if (Object.FindFirstObjectByType<ForestQuestApp>() != null) return;

            EnsureCamera();
            EnsureEventSystem();

            var root = new GameObject("ForestQuestRoot");
            Object.DontDestroyOnLoad(root);

            // 1. Initialize all systems first
            var container = root.AddComponent<ForestSystemsContainer>();
            container.InitializeAll();

            // 2. Attach main app controller (uses systems via ForestSystemsContainer.Instance)
            root.AddComponent<ForestQuestApp>();

            Debug.Log("[Bootstrap] Forest Friends Quest initialized successfully.");
        }

        // ─── Camera Setup ─────────────────────────────────────────────────────────

        private static void EnsureCamera()
        {
            if (Camera.main != null) return;

            var cameraObject = new GameObject("MainCamera");
            cameraObject.tag = "MainCamera";

            var cam = cameraObject.AddComponent<Camera>();
            cam.clearFlags       = CameraClearFlags.SolidColor;
            cam.backgroundColor  = new Color(0.06f, 0.18f, 0.12f); // Deep forest night
            cam.orthographic     = true;
            cam.orthographicSize = 5f;
            cam.nearClipPlane    = -10f;
            cam.farClipPlane     = 100f;
            cam.depth            = -1f;

            // Smooth camera listener will be added by CameraFeelController
        }

        // ─── Event System Setup ───────────────────────────────────────────────────

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;

            var eventSystem = new GameObject("EventSystem");
            Object.DontDestroyOnLoad(eventSystem);
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }
    }
}
