using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

/// <summary>
/// Manages switching between two scenes that are loaded additively.
/// Uses a single MainCamera with layer culling to show/hide scenes.
/// Call SwitchScene() to smoothly transition between scenes with a fade effect.
/// </summary>
public class ScaneManager : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string _sceneA;
    [SerializeField] private string _sceneB;
    [SerializeField] private LayerMask _sceneALayer;
    [SerializeField] private LayerMask _sceneBLayer;

    [Header("Camera")]
    [SerializeField] private Camera _mainCamera;

    [Header("Transition")]
    [SerializeField] private Image _fadeOverlay;
    [SerializeField] private float _fadeDuration = 0.5f;

    private string _currentScene;
    private bool _isTransitioning;

    private void Start()
    {
        if (_fadeOverlay != null)
            _fadeOverlay.raycastTarget = false;

        if (_mainCamera == null)
            _mainCamera = Camera.main;
        
        LoadBothScenes();
    }

    [ContextMenu("SwitchScene")]
    public void SwitchScene()
    {
        if (_isTransitioning) return;

        string targetScene = _currentScene == _sceneA ? _sceneB : _sceneA;
        StartCoroutine(TransitionToScene(targetScene));
    }

    private void LoadBothScenes()
    {
        if (string.IsNullOrEmpty(_sceneA) || string.IsNullOrEmpty(_sceneB))
        {
            Debug.LogError("Scene names not set in ScaneManager!");
            return;
        }

        UnitySceneManager.LoadScene(_sceneA, UnityEngine.SceneManagement.LoadSceneMode.Additive);
        UnitySceneManager.LoadScene(_sceneB, UnityEngine.SceneManagement.LoadSceneMode.Additive);
        
        SetSceneActive(_sceneB, false);
        SetSceneActive(_sceneA, true);
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
        if (_mainCamera == null) return;

        // Switch camera culling mask to show only the active scene's layer
        LayerMask targetLayer = sceneName == _sceneA ? _sceneALayer : _sceneBLayer;
        _mainCamera.cullingMask = targetLayer;

        // Disable canvas interactivity for inactive scene
        var scene = UnitySceneManager.GetSceneByName(sceneName);
        if (!scene.IsValid()) return;

        foreach (var root in scene.GetRootGameObjects())
        {
            var canvas = root.GetComponent<Canvas>();
            if (canvas != null)
            {
               canvas.gameObject.SetActive(active);
            }
        }
    }
}
