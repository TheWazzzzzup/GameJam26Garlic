using System;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

/// <summary>
/// Subscribes to known game events and calls VFXManager.Play(key). Add bindings in inspector;
/// new events = new row (source + key). Manager stays unaware of concrete events.
/// </summary>
public class VFXEventBridge : MonoBehaviour
{
    public enum KnownEvent
    {
        None,
        BoostStarted,
        BoostEnded,
        Death,
        EnemyDealDamage
    }

    [Serializable]
    public class Binding
    {
        [Tooltip("Which event to listen for.")]
        public KnownEvent Event = KnownEvent.None;

        [Tooltip("Source component (PlayerBehavior for Boost*, EnemyCollision for EnemyDealDamage). Not used for Death.")]
        public UnityEngine.Object Source;

        [Tooltip("VFX config key to play when this event fires (e.g. BoostStarted, Death).")]
        public string VFXKey = "";
    }

    [Header("Event bindings")]
    [Tooltip("List of event sources and VFX keys. Subscribe in Start, unsubscribe in OnDestroy.")]
    [SerializeField] private Binding[] _bindings = new Binding[0];

    private readonly List<(KnownEvent eventType, object source, Action action)> _subscribed = new List<(KnownEvent, object, Action)>();

    private void Start()
    {
        foreach (var b in _bindings)
        {
            if (b == null || b.Event == KnownEvent.None || string.IsNullOrEmpty(b.VFXKey)) continue;

            string key = b.VFXKey;
            Action action = () => Play(key);

            switch (b.Event)
            {
                case KnownEvent.BoostStarted:
                    var pbStart = b.Source as PlayerBehavior;
                    if (pbStart != null)
                    {
                        pbStart.OnBoostStarted += action;
                        _subscribed.Add((KnownEvent.BoostStarted, pbStart, action));
                    }
                    break;
                case KnownEvent.BoostEnded:
                    var pbEnd = b.Source as PlayerBehavior;
                    if (pbEnd != null)
                    {
                        pbEnd.OnBoostEnded += action;
                        _subscribed.Add((KnownEvent.BoostEnded, pbEnd, action));
                    }
                    break;
                case KnownEvent.Death:
                    Data.OnDeath += action;
                    _subscribed.Add((KnownEvent.Death, null, action));
                    break;
                case KnownEvent.EnemyDealDamage:
                    var ec = b.Source as EnemyCollision;
                    if (ec != null)
                    {
                        ec.EnemyDealDamage += action;
                        _subscribed.Add((KnownEvent.EnemyDealDamage, ec, action));
                    }
                    break;
            }
        }
    }

    private void OnDestroy()
    {
        foreach (var (eventType, source, action) in _subscribed)
        {
            switch (eventType)
            {
                case KnownEvent.BoostStarted:
                    (source as PlayerBehavior)?.OnBoostStarted -= action;
                    break;
                case KnownEvent.BoostEnded:
                    (source as PlayerBehavior)?.OnBoostEnded -= action;
                    break;
                case KnownEvent.Death:
                    Data.OnDeath -= action;
                    break;
                case KnownEvent.EnemyDealDamage:
                    (source as EnemyCollision)?.EnemyDealDamage -= action;
                    break;
            }
        }
        _subscribed.Clear();
    }

    private void Play(string key)
    {
        if (VFXManager.Instance != null)
            VFXManager.Instance.Play(key);
    }
}
