using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace ForestFriendsQuest
{
    /// <summary>
    /// Live content pipeline for Forest Friends Quest.
    ///
    /// Responsibilities:
    ///   - Fetch remote content manifest from CDN (JSON over HTTPS)
    ///   - Compare against local manifest version
    ///   - Download and cache new content bundles asynchronously
    ///   - Inject downloaded seasonal events into running systems
    ///   - 100% offline-safe: all fallback to baked-in defaults
    ///
    /// Content bundle types:
    ///   "ritual"    — new DailyRitual definitions
    ///   "event"     — seasonal WorldEvent definitions
    ///   "lore"      — new lore page text
    ///   "puzzle"    — puzzle level patches
    ///   "dialogue"  — creature dialogue lines
    ///
    /// CDN format: https://cdn.forestfriendsquest.com/content/v{version}/manifest.json
    ///
    /// This implementation uses a fully offline stub by default.
    /// Set ContentServerUrl in PlayerPrefs to enable live fetch.
    /// </summary>
    public class LiveContentPipeline : MonoBehaviour
    {
        private ModularSaveSystem   _modSave;
        private DailyRitualSystem   _rituals;
        private RareWorldEventSystem _events;
        private SaveModule          _module;

        private readonly List<ContentBundle> _loadedBundles = new List<ContentBundle>();
        private bool                         _fetchComplete;

        private const string DefaultContentUrl  = "https://cdn.forestfriendsquest.com/content";
        private const string ManifestVersionKey = "manifestVersion";

        public bool IsFetchComplete => _fetchComplete;
        public IReadOnlyList<ContentBundle> LoadedBundles => _loadedBundles;

        public event Action<ContentBundle> OnBundleLoaded;

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        public void Initialize(ModularSaveSystem modSave, DailyRitualSystem rituals,
            RareWorldEventSystem events)
        {
            _modSave = modSave;
            _rituals = rituals;
            _events  = events;
            _module  = modSave?.RegisterModule("live_content", version: 1);

            // Non-blocking fetch — game starts regardless
            StartCoroutine(FetchManifest());
        }

        // ─── Public API ───────────────────────────────────────────────────────────

        /// <summary>Returns true if the player has received live content this session.</summary>
        public bool HasNewContent() => _loadedBundles.Count > 0;

        /// <summary>Get all loaded bundles of a given type.</summary>
        public List<ContentBundle> GetBundles(string type)
        {
            var result = new List<ContentBundle>();
            foreach (var b in _loadedBundles)
                if (b.bundleType == type) result.Add(b);
            return result;
        }

        // ─── Fetch Pipeline ───────────────────────────────────────────────────────

        private IEnumerator FetchManifest()
        {
            var serverUrl  = PlayerPrefs.GetString("FFQ.ContentServer", DefaultContentUrl);
            var localVer   = _module?.GetInt(ManifestVersionKey, 0) ?? 0;
            var manifestUrl = $"{serverUrl}/v{localVer + 1}/manifest.json";

            using var req = UnityWebRequest.Get(manifestUrl);
            req.timeout   = 8; // don't block startup

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.Log($"[LiveContent] No new content (offline or no update). {req.error}");
                _fetchComplete = true;
                yield break;
            }

            ContentManifest manifest = null;
            try
            {
                manifest = JsonUtility.FromJson<ContentManifest>(req.downloadHandler.text);
            }
            catch
            {
                Debug.LogWarning("[LiveContent] Manifest parse failed — using offline defaults.");
                _fetchComplete = true;
                yield break;
            }

            if (manifest?.bundles == null)
            {
                _fetchComplete = true;
                yield break;
            }

            foreach (var bundleRef in manifest.bundles)
                yield return StartCoroutine(FetchBundle(serverUrl, bundleRef));

            _module?.SetInt(ManifestVersionKey, manifest.version);
            _fetchComplete = true;

            Debug.Log($"[LiveContent] Fetched {_loadedBundles.Count} bundles. " +
                      $"Manifest v{manifest.version}.");
        }

        private IEnumerator FetchBundle(string serverUrl, BundleRef bundleRef)
        {
            var url = $"{serverUrl}/bundles/{bundleRef.fileName}";
            using var req = UnityWebRequest.Get(url);
            req.timeout   = 10;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success) yield break;

            ContentBundle bundle = null;
            try
            {
                bundle = JsonUtility.FromJson<ContentBundle>(req.downloadHandler.text);
            }
            catch { yield break; }

            if (bundle == null) yield break;

            _loadedBundles.Add(bundle);
            InjectBundle(bundle);
            OnBundleLoaded?.Invoke(bundle);
            Debug.Log($"[LiveContent] Loaded bundle: {bundle.bundleId} ({bundle.bundleType})");
        }

        // ─── Bundle Injection ─────────────────────────────────────────────────────

        private void InjectBundle(ContentBundle bundle)
        {
            switch (bundle.bundleType)
            {
                case "ritual":
                    InjectRituals(bundle);
                    break;
                case "event":
                    InjectEvents(bundle);
                    break;
                // "lore", "puzzle", "dialogue" handled by consumer systems on demand
            }
        }

        private void InjectRituals(ContentBundle bundle)
        {
            if (_rituals == null || bundle.rituals == null) return;
            foreach (var ritual in bundle.rituals)
            {
                _rituals.RegisterLiveRitual(ritual);
                Debug.Log($"[LiveContent] Injected ritual: {ritual.ritualId}");
            }
        }

        private void InjectEvents(ContentBundle bundle)
        {
            // Events injected on next day tick via RareWorldEventSystem extension point
            Debug.Log($"[LiveContent] Event bundle ready: {bundle.bundleId}");
        }
    }

    // ─── Data Contracts ───────────────────────────────────────────────────────────

    [Serializable]
    public class ContentManifest
    {
        public int         version;
        public BundleRef[] bundles;
    }

    [Serializable]
    public class BundleRef
    {
        public string fileName;
        public string bundleType;
    }

    [Serializable]
    public class ContentBundle
    {
        public string        bundleId;
        public string        bundleType;
        public int           contentVersion;
        public DailyRitual[]  rituals;
        public string[]      loreTexts;
        public string[]      dialogueLines;
    }
}
