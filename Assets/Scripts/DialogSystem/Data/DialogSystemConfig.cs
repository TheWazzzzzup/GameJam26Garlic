using UnityEngine;

namespace DialogSystem
{
    [CreateAssetMenu(fileName = "DialogSystemConfig", menuName = "Configs/Dialog System Config")]
    public class DialogSystemConfig : ScriptableObject
    {
        [Header("Question Timing")]
        [Tooltip("Minimum time (seconds) between questions")]
        public float MinAskQuestionTimeRange = 5f;
        
        [Tooltip("Maximum time (seconds) between questions")]
        public float MaxAskQuestionTimeRange = 15f;

        [Header("Dialog Data")]
        [Tooltip("All available dialogs - auto-loaded from Resources/Configs/DialogsConfigs")]
        public DialogData[] Dialogs;

        private static DialogSystemConfig _instance;
        private static bool _dialogsLoaded = false;

        public static DialogSystemConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<DialogSystemConfig>("Configs/DialogSystemConfig");
                    
                    if (_instance == null)
                    {
                        Debug.LogError("DialogSystemConfig not found in Resources/Configs/! Please create one.");
                        return null;
                    }
                }

                // Auto-load dialogs from Resources/Configs/DialogsConfigs on first access
                if (!_dialogsLoaded)
                {
                    _instance.LoadDialogs();
                    _dialogsLoaded = true;
                }
                
                return _instance;
            }
        }

        private void LoadDialogs()
        {
            DialogData[] loadedDialogs = Resources.LoadAll<DialogData>("Configs/DialogsConfigs");
            
            if (loadedDialogs == null || loadedDialogs.Length == 0)
            {
                Debug.LogWarning("No DialogData found in Resources/Configs/DialogsConfigs/. Please add DialogData assets there.");
                Dialogs = new DialogData[0];
            }
            else
            {
                Dialogs = loadedDialogs;
                Debug.Log($"Auto-loaded {Dialogs.Length} DialogData assets from Resources/Configs/DialogsConfigs/");
            }
        }
    }
}
