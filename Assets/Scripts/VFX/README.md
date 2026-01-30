# Adding a VFX from Scratch

This guide walks through adding a new visual effect and wiring it to a game event.

## 1. Create the effect prefab

1. In the Hierarchy: **Right-click → Effects → Particle System** (or create an empty GameObject and add a **Particle System** component).
2. Tune the particle system (duration, start lifetime, start speed, color, etc.). Set **Stop Action** to **Disable** or **Destroy** if you want the object to clean up after playing.
3. Drag the object from the Hierarchy into your **Project** (e.g. under `Assets/Prefabs/VFX/`) to create a **prefab**. Delete the instance from the scene if you don’t need it there.

Your effect can also be a prefab with other components (Animator, custom scripts, etc.). The manager will instantiate it and, if present, call **Play** on any **ParticleSystem** in the prefab.

## 2. Add a mapping in VFXConfig

1. Open the VFX config asset: **Resources/Configs/VFXConfig** (or create one via **Assets → Create → Configs → VFX Config** and put it in `Resources/Configs/`).
2. In the Inspector, under **Event to effect mapping**, increase **Mappings** size by one.
3. For the new element:
   - **Event Key**: A string that will trigger this effect (e.g. `BoostStarted`, `DateSiteHit`, `Death`). Use a clear, consistent name.
   - **Prefab**: Assign the effect prefab you created.
   - **Spawn Parent**: (Optional) Leave empty to spawn in world space, or assign a Transform to parent the effect under.
   - **Use World Position**: If true, the effect is placed at the position/rotation from the `Play()` call (or default). If false and **Spawn Parent** is set, it spawns under that transform.

The **first** mapping whose **Event Key** matches the key you pass to `Play()` is used. You can set **Default Event Key** to a fallback key for any unknown keys.

## 3. Trigger the effect

You can trigger the effect in two ways.

### Option A: Use VFXEventBridge (for built‑in events)

If the effect should play when one of the known events fires (e.g. boost start/end, death, enemy deal damage):

1. Ensure the scene has a **VFXEventBridge** component (e.g. on a manager GameObject). If not, add an empty GameObject and add **VFX Event Bridge**.
2. In the Inspector, under **Event bindings**, add a binding:
   - **Event**: Choose the event (e.g. **Boost Started**, **Death**, **Enemy Deal Damage**).
   - **Source**: For **Boost Started** / **Boost Ended**, assign the scene’s **PlayerBehavior**. For **Enemy Deal Damage**, assign an **EnemyCollision** (optional; you can also trigger from code). Leave empty for **Death**.
   - **VFX Key**: Enter the **exact** same string you used as **Event Key** in VFXConfig (e.g. `BoostStarted`).

When that event fires, the bridge will call `VFXManager.Instance.Play(yourKey)`.

### Option B: Call from code (any event or custom logic)

From any script:

```csharp
// Simple: play by key (uses default position/rotation)
VFXManager.Instance?.Play("DateSiteHit");

// At a world position
VFXManager.Instance?.PlayAt("DateSiteHit", new Vector3(0, 0, 0));

// At a transform (e.g. player or hit point)
VFXManager.Instance?.PlayAt("DateSiteHit", someTransform);

// With full context (position, rotation, intensity)
var ctx = VFXPlayContext.At(hitPosition, Quaternion.identity, intensity: 1.5f);
VFXManager.Instance?.Play("DateSiteHit", ctx);
```

Use the **same** string as the **Event Key** in VFXConfig. If the key has no mapping, nothing happens (unless you set **Default Event Key** in the config).

## 4. Ensure VFXManager is available

- If the scene has a GameObject with **VFX Manager** on it, that instance is used.
- If not, the first call to `VFXManager.Instance` will create one and load **Resources/Configs/VFXConfig** automatically. So either place a manager in the scene and optionally assign a config, or rely on the auto-created instance and the config in Resources.

## Checklist

- [ ] Effect prefab created (e.g. Particle System) and saved in Project.
- [ ] **VFXConfig** has a mapping: **Event Key** = your key, **Prefab** = your effect prefab.
- [ ] Effect is triggered either by a **VFXEventBridge** binding (Event + Source + same **VFX Key**) or by code calling `VFXManager.Instance.Play("YourKey")` (and optionally `PlayAt` / `VFXPlayContext`).
- [ ] **VFXConfig** lives under **Resources/Configs/** (or a VFXManager in scene has a config assigned).

After that, when the event fires or your code calls `Play`, the manager will instantiate your prefab and play any ParticleSystem on it.
