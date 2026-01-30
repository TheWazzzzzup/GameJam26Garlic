# Configuration System Migration - Summary

## Overview
All configuration parameters have been extracted from SerializeFields in MonoBehaviours into centralized ScriptableObject configs that automatically load from the `Assets/Resources/Configs/` folder.

## What Changed

### New Config Files Created

1. **DialogSystemConfig.cs**
   - Location: `Assets/Scripts/DialogSystem/Data/DialogSystemConfig.cs`
   - Controls: Question timing and dialog data array
   - Auto-loads from: `Resources/Configs/DialogSystemConfig`
   - **Dialogs array auto-loads from:** `Resources/Configs/DialogsConfigs/`
   - Simply add DialogData assets to the DialogsConfigs folder - no manual assignment needed!

2. **DialogDisplayConfig.cs**
   - Location: `Assets/Scripts/DialogSystem/Data/DialogDisplayConfig.cs`
   - Controls: Visual display settings (scroll speed, visible lines, fade effects)
   - Auto-loads from: `Resources/Configs/DialogDisplayConfig`

3. **SatisfactionConfig.cs** (Updated)
   - Location: `Assets/Scripts/SatisfactionSystem/SatisfactionConfig.cs`
   - Controls: Satisfaction values and reduction rates
   - Auto-loads from: `Resources/Configs/SatisfactionConfig`
   - Added: Singleton Instance pattern for auto-loading

### Modified Files

#### DialogSystemManager.cs
**Removed:**
- `[SerializeField] private float _minAskQuestionTimeRange`
- `[SerializeField] private float _maxAskQuestionTimeRange`
- `[SerializeField] private DialogData[] dialogs`

**Added:**
- `private DialogSystemConfig _config` (auto-loaded in Awake)
- References to config values: `_config.MinAskQuestionTimeRange`, `_config.MaxAskQuestionTimeRange`, `_config.Dialogs`

#### DialogHandler.cs
**Removed:**
- `[SerializeField] private int _visibleLineCount`
- `[SerializeField] private float _scrollSpeed`
- `[SerializeField] private float _fadeHeight`
- `[SerializeField] private Color _fadeColor`

**Added:**
- `private DialogDisplayConfig _config` (auto-loaded in Awake)
- References to config values: `_config.VisibleLineCount`, `_config.ScrollSpeed`, `_config.FadeHeight`, `_config.FadeColor`

**Kept:**
- UI References: `_dialogSystemManager`, `_text`, `_viewport`, `_viewportMask`, `_topFadeOverlay`, `_bottomFadeOverlay`

#### SatisfactionHandler.cs
**Removed:**
- `[SerializeField] private SatisfactionConfig _config`

**Added:**
- `private SatisfactionConfig _config` (auto-loaded in Awake using `SatisfactionConfig.Instance`)

**Kept:**
- `[SerializeField] private DialogSystemManager _dialogSystemManager` (necessary runtime reference)

### New Folder Structure
```
Assets/
  └── Resources/
      └── Configs/
          ├── README.md (comprehensive guide for game designers)
          ├── SETUP_GUIDE.txt (quick reference)
          ├── DialogSystemConfig.asset ✓
          ├── DialogDisplayConfig.asset ✓
          ├── SatisfactionConfig.asset ✓
          └── DialogsConfigs/
              ├── New Dialog Data.asset ✓
              ├── New Dialog Data 1.asset ✓
              └── (add more DialogData here - auto-loads!)
```

## How It Works

### Auto-Loading Pattern
Each config uses a Singleton Instance property:
```csharp
private static ConfigType _instance;

public static ConfigType Instance
{
    get
    {
        if (_instance == null)
            _instance = Resources.Load<ConfigType>("Configs/ConfigType");
        
        if (_instance == null)
            Debug.LogError("ConfigType not found in Resources/Configs/!");
        
        return _instance;
    }
}
```

### Manager Loading
Each manager loads its config in `Awake()`:
```csharp
private void Awake()
{
    _config = ConfigType.Instance;
    if (_config == null)
    {
        Debug.LogError("Config not found! Please create one in Resources/Configs/");
        enabled = false;
        return;
    }
    // ... rest of initialization
}
```

## Setup Instructions

### For Game Designers

1. **Create the config assets:**
   - Navigate to `Assets/Resources/Configs/`
   - Right-click → Create → Configs → [Config Type]
   - Create all three configs with exact names:
     - `DialogSystemConfig`
     - `DialogDisplayConfig`
     - `SatisfactionConfig`

2. **Configure values:**
   - Open each asset in the Inspector
   - Set desired values (see README.md for recommendations)
   - Save changes (Ctrl+S / Cmd+S)

3. **Test:**
   - Enter Play Mode
   - Changes take effect immediately
   - No need to assign references in scenes!

### For Programmers

**No scene setup required!** All configs auto-load. However:

- Ensure config assets exist in `Resources/Configs/` folder
- Config file names must match exactly (case-sensitive)
- UI references in managers (like `_text`, `_viewport`) still need manual assignment in scene
- Only configuration values were extracted, not runtime references

## Benefits

1. **Centralized Configuration**
   - All game parameters in one location
   - Easy for game designers to find and modify

2. **No Scene Dependencies**
   - Configs auto-load at runtime
   - No need to drag-and-drop references
   - Works across multiple scenes automatically

3. **Version Control Friendly**
   - Config changes don't affect scene files
   - Easier to merge config changes
   - Game designers can work independently

4. **Iteration Speed**
   - Quick value tweaks without scene editing
   - No risk of breaking scene references
   - Immediate feedback in Play Mode

5. **Organized Structure**
   - Logical separation: DialogSystem, Display, Satisfaction
   - Each config has clear responsibility
   - Well-documented with tooltips and README

## Migration Checklist

- [x] Create DialogSystemConfig ScriptableObject
- [x] Create DialogDisplayConfig ScriptableObject
- [x] Update SatisfactionConfig with Instance pattern
- [x] Update DialogSystemManager to use DialogSystemConfig
- [x] Update DialogHandler to use DialogDisplayConfig
- [x] Update SatisfactionHandler to use SatisfactionConfig.Instance
- [x] Create Resources/Configs folder structure
- [x] Create comprehensive README for game designers
- [x] Remove all config SerializeFields from managers
- [x] Add auto-loading in Awake() methods
- [x] Add error handling for missing configs
- [x] Create actual config assets in Resources/Configs/
- [x] Move existing assets to Resources/Configs/
- [x] Add auto-loading for DialogData from DialogsConfigs/
- [x] DialogSystemConfig.Dialogs array auto-populated at runtime
- [ ] **TODO: Test in Play Mode to verify everything works**

## Testing Verification

After creating the config assets, verify:
1. No errors in Console about missing configs
2. Questions appear at configured intervals
3. Dialog displays with configured visual settings
4. Satisfaction system uses configured values
5. All three systems work together properly

## Rollback Plan

If issues occur, the previous code is in git history. To rollback:
1. Revert manager files (DialogSystemManager, DialogHandler, SatisfactionHandler)
2. Delete new config files (DialogSystemConfig, DialogDisplayConfig)
3. Revert SatisfactionConfig changes
4. Reassign SerializeFields in scene objects
