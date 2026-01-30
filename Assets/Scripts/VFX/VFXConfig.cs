using UnityEngine;

/// <summary>
/// ScriptableObject config: event key to effect prefab mapping.
/// Store in Resources/Configs/VFXConfig for auto-load, or assign in scene.
/// </summary>
[CreateAssetMenu(fileName = "VFXConfig", menuName = "Configs/VFX Config")]
public class VFXConfig : ScriptableObject
{
    [System.Serializable]
    public class VFXMapping
    {
        [Tooltip("Event key (e.g. BoostStarted, DateSiteHit). First match wins.")]
        public string EventKey = "";

        [Tooltip("Effect prefab to instantiate (e.g. with ParticleSystem). Leave empty to skip.")]
        public GameObject Prefab;

        [Tooltip("Optional parent for spawned effect. If set and UseWorldPosition is false, spawn under this transform.")]
        public Transform SpawnParent;

        [Tooltip("If true, spawn at context Position (or world position). If false and SpawnParent set, spawn under parent.")]
        public bool UseWorldPosition = true;
    }

    [Header("Event to effect mapping")]
    [Tooltip("List of event keys to prefabs. Order matters: first matching key is used.")]
    [SerializeField] private VFXMapping[] _mappings = new VFXMapping[0];

    [Header("Unknown keys")]
    [Tooltip("If Play(unknownKey) is called and no mapping exists, play this key's effect instead (optional).")]
    [SerializeField] private string _defaultEventKey = "";

    /// <summary>Mappings (read-only).</summary>
    public VFXMapping[] Mappings => _mappings ?? new VFXMapping[0];

    /// <summary>Optional fallback key when the requested key has no mapping.</summary>
    public string DefaultEventKey => _defaultEventKey ?? "";

    /// <summary>Look up mapping by key. Returns null if not found.</summary>
    public VFXMapping GetMapping(string eventKey)
    {
        if (string.IsNullOrEmpty(eventKey)) return null;
        foreach (var m in Mappings)
        {
            if (m != null && m.EventKey == eventKey)
                return m;
        }
        return null;
    }

    /// <summary>Resolve key: exact match, or DefaultEventKey if no match.</summary>
    public VFXMapping ResolveKey(string eventKey)
    {
        var m = GetMapping(eventKey);
        if (m != null) return m;
        if (!string.IsNullOrEmpty(_defaultEventKey) && _defaultEventKey != eventKey)
            return GetMapping(_defaultEventKey);
        return null;
    }

    private static VFXConfig _instance;

    /// <summary>Config loaded from Resources/Configs/VFXConfig. Null if not found.</summary>
    public static VFXConfig Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<VFXConfig>("Configs/VFXConfig");
            return _instance;
        }
    }
}
