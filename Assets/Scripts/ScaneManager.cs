using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

/// <summary>
/// Manages switching between two scenes that are loaded additively.
/// Call SwitchScene() to smoothly transition between scenes with a fade effect.
/// </summary>
public class ScaneManager : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string _sceneA;
    [SerializeField] private string _sceneB;

    [Header("Transition")]
    [SerializeField] private Image _fadeOverlay;
    [SerializeField] private float _fadeDuration = 0.5f;

    private string _currentScene;
    private bool _isTransitioning;

    private void Awake()
    {
        if (_fadeOverlay != null)
            _fadeOverlay.raycastTarget = false;
        
        LoadBothScenes();
    }

    [ContextMenu("SwitchScene")]
    public void SwitchScene()
    {
        if (_isTransitioning) return;

        string targetScene = _currentScene == _sceneA ? _sceneB : _sceneA;
        StartCoroutine(TransitionToScene(targetScene));
    }

    public void LoadBothScenes()
    {
        if (string.IsNullOrEmpty(_sceneA) || string.IsNullOrEmpty(_sceneB))
        {
            Debug.LogError("Scene names not set in ScaneManager!");
            return;
        }

        UnitySceneManager.LoadScene(_sceneA, UnityEngine.SceneManagement.LoadSceneMode.Additive);
        UnitySceneManager.LoadScene(_sceneB, UnityEngine.SceneManagement.LoadSceneMode.Additive);
        
        _currentScene = _sceneA;
        UnitySceneManager.SetActiveScene(UnitySceneManager.GetSceneByName(_sceneA));
        
        SetSceneActive(_sceneB, false);
    }

    private IEnumerator TransitionToScene(string targetScene)
    {
        _isTransitioning = true;

        // Fade out
        yield return Fade(0f, 1f);

        // Switch scenes
        SetSceneActive(_currentScene, false);
        SetSceneActive(targetScene, true);
        UnitySceneManager.SetActiveScene(UnitySceneManager.GetSceneByName(targetScene));
        _currentScene = targetScene;

        // Fade in
        yield return Fade(1f, 0f);

        _isTransitioning = false;
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        if (_fadeOverlay == null) yield break;

        float elapsed = 0f;
        Color color = _fadeOverlay.color;

        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _fadeDuration;
            color.a = Mathf.Lerp(startAlpha, endAlpha, t);
            _fadeOverlay.color = color;
            yield return null;
        }

        color.a = endAlpha;
        _fadeOverlay.color = color;
    }

    private void SetSceneActive(string sceneName, bool active)
    {
        var scene = UnitySceneManager.GetSceneByName(sceneName);
        if (!scene.IsValid()) return;

        foreach (var root in scene.GetRootGameObjects())
        {
            // Disable all visual components but keep logic running
            DisableVisualsRecursive(root, active);
        }
    }

    private void DisableVisualsRecursive(GameObject obj, bool active)
    {
        // Disable UI visuals
        var canvas = obj.GetComponent<Canvas>();
        if (canvas != null)
        {
            var canvasGroup = canvas.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
            
            canvasGroup.alpha = active ? 1f : 0f;
            canvasGroup.interactable = active;
            canvasGroup.blocksRaycasts = active;
        }

        // Disable sprite visuals
        var spriteRenderer = obj.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.enabled = active;

        // Disable UI Image visuals
        var image = obj.GetComponent<UnityEngine.UI.Image>();
        if (image != null)
            image.enabled = active;

        // Disable UI RawImage visuals
        var rawImage = obj.GetComponent<UnityEngine.UI.RawImage>();
        if (rawImage != null)
            rawImage.enabled = active;

        // Disable TextMeshPro visuals
        var tmpText = obj.GetComponent<TMPro.TextMeshProUGUI>();
        if (tmpText != null)
            tmpText.enabled = active;

        var tmpText3D = obj.GetComponent<TMPro.TextMeshPro>();
        if (tmpText3D != null)
            tmpText3D.enabled = active;

        // Disable particle systems
        var particleSystem = obj.GetComponent<ParticleSystem>();
        if (particleSystem != null)
            particleSystem.enableEmission = active;

        // Recurse through children
        foreach (Transform child in obj.transform)
            DisableVisualsRecursive(child.gameObject, active);
    }
}
