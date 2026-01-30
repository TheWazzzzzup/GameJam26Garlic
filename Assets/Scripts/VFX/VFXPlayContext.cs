using UnityEngine;

/// <summary>
/// Optional position, rotation, transform, and intensity for VFXManager.Play().
/// Use for world-space particles, attached effects, or screen-space.
/// </summary>
public struct VFXPlayContext
{
    /// <summary>World position to spawn at; used when Transform is null or UseWorldPosition is true.</summary>
    public Vector3? Position;

    /// <summary>Rotation to spawn with; optional.</summary>
    public Quaternion? Rotation;

    /// <summary>Optional parent/attach transform; if set, position/rotation may be relative.</summary>
    public Transform Transform;

    /// <summary>Optional intensity scale (e.g. for particle emission or scale).</summary>
    public float Intensity;

    /// <summary>Create a context with world position and optional rotation.</summary>
    public static VFXPlayContext At(Vector3 position, Quaternion? rotation = null, float intensity = 1f)
    {
        return new VFXPlayContext
        {
            Position = position,
            Rotation = rotation,
            Intensity = intensity
        };
    }

    /// <summary>Create a context attached to a transform (position/rotation from transform).</summary>
    public static VFXPlayContext AtTransform(Transform transform, float intensity = 1f)
    {
        return new VFXPlayContext
        {
            Transform = transform,
            Position = transform != null ? transform.position : (Vector3?)null,
            Rotation = transform != null ? transform.rotation : (Quaternion?)null,
            Intensity = intensity
        };
    }

    /// <summary>Empty context (manager will use default spawn position, e.g. origin or camera).</summary>
    public static VFXPlayContext None => new VFXPlayContext { Intensity = 1f };
}
