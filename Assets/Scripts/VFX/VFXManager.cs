using UnityEngine;

/// <summary>
/// Single entry point for playing VFX by event key. Resolves key via VFXConfig and spawns prefab.
/// Use Instance (loads from Resources/Configs/VFXConfig) or assign config in scene.
/// </summary>
public class VFXManager : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("Optional. If unset, uses Resources/Configs/VFXConfig.")]
    [SerializeField] private VFXConfig _config;

    private static VFXManager _instance;

    /// <summary>Singleton instance. Creates a runtime instance if none exists and config is in Resources.</summary>
    public static VFXManager Instance
    {
        get
        {
            if (_instance != null) return _instance;
            var config = VFXConfig.Instance;
            if (config == null) return null;
            var go = new GameObject("VFXManager");
            _instance = go.AddComponent<VFXManager>();
            _instance._config = config;
            return _instance;
        }
    }

    private void Awake()
    {
        if (_config == null)
            _config = VFXConfig.Instance;
        if (_instance == null)
            _instance = this;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    /// <summary>Config in use (assigned or from Resources).</summary>
    public VFXConfig Config => _config ?? (_config = VFXConfig.Instance);

    /// <summary>
    /// Play effect for the given event key. If key has no mapping, tries DefaultEventKey; else no-op.
    /// Context provides optional position, rotation, transform, intensity.
    /// </summary>
    public void Play(string eventKey, VFXPlayContext? context = null)
    {
        if (Config == null) return;
        var mapping = Config.ResolveKey(eventKey ?? "");
        if (mapping == null || mapping.Prefab == null) return;

        var ctx = context ?? VFXPlayContext.None;
        Vector3 position = ctx.Position ?? Vector3.zero;
        Quaternion rotation = ctx.Rotation ?? Quaternion.identity;
        Transform parent = mapping.UseWorldPosition ? null : (mapping.SpawnParent != null ? mapping.SpawnParent : ctx.Transform);

        if (parent != null && !mapping.UseWorldPosition)
        {
            position = parent.position;
            rotation = parent.rotation;
        }
        else if (ctx.Transform != null && mapping.UseWorldPosition)
        {
            position = ctx.Position ?? ctx.Transform.position;
            rotation = ctx.Rotation ?? ctx.Transform.rotation;
        }

        var instance = Instantiate(mapping.Prefab, position, rotation, parent);
        if (instance == null) return;

        var ps = instance.GetComponentInChildren<ParticleSystem>();
        if (ps != null)
        {
            ps.Clear(true);
            ps.Play(true);
        }

        if (ctx.Intensity != 1f && ctx.Intensity > 0f)
        {
            instance.transform.localScale = instance.transform.localScale * ctx.Intensity;
        }
    }

    /// <summary>Convenience: play at world position.</summary>
    public void PlayAt(string eventKey, Vector3 position, Quaternion? rotation = null, float intensity = 1f)
    {
        Play(eventKey, VFXPlayContext.At(position, rotation, intensity));
    }

    /// <summary>Convenience: play at transform.</summary>
    public void PlayAt(string eventKey, Transform transform, float intensity = 1f)
    {
        Play(eventKey, VFXPlayContext.AtTransform(transform, intensity));
    }
}
