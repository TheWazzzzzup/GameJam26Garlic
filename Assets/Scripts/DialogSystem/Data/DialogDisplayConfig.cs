using UnityEngine;

namespace DialogSystem
{
    [CreateAssetMenu(fileName = "DialogDisplayConfig", menuName = "Configs/Dialog Display Config")]
    public class DialogDisplayConfig : ScriptableObject
    {
        [Header("Viewport Settings")]
        [Tooltip("Number of lines visible in the viewport at once (e.g. 2-3)")]
        [Min(1)]
        public int VisibleLineCount = 3;

        [Header("Scroll Settings")]
        [Tooltip("How fast the text scrolls up (units per second)")]
        [Min(0.1f)]
        public float ScrollSpeed = 80f;

        [Header("Fade Settings")]
        [Tooltip("Height of the fade zone above/below viewport (pixels)")]
        [Min(1)]
        public float FadeHeight = 48f;
        
        [Tooltip("Color of the fade overlay (match your dialog background)")]
        public Color FadeColor = Color.black;

        private static DialogDisplayConfig _instance;

        public static DialogDisplayConfig Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<DialogDisplayConfig>("Configs/DialogDisplayConfig");
                
                if (_instance == null)
                    Debug.LogError("DialogDisplayConfig not found in Resources/Configs/! Please create one.");
                
                return _instance;
            }
        }
    }
}
