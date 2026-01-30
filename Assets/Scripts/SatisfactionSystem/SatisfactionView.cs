using DefaultNamespace;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SatisfactionView : MonoBehaviour
{
    [SerializeField] Slider slider;
    [SerializeField]TMP_Text text;
    
    private void Start()
    {
        slider.maxValue = Data.MaxSatisfaction;
        slider.minValue = 0;
        slider.maxValue = Data.MaxSatisfaction;
        slider.value = Data.CurrentSatisfaction;
        text.text = $"Satisfaction:{Data.CurrentSatisfaction}";
    }

    private void FixedUpdate()
    {
        slider.value = Data.CurrentSatisfaction;
        text.text = $"Satisfaction:{Data.CurrentHealth}";
    }
}