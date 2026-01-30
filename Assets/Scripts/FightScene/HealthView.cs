using System;
using DefaultNamespace;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthView : MonoBehaviour
{
    [SerializeField] Slider healthSlider;
    [SerializeField]TMP_Text healthText;
    
    private void Awake()
    {
        healthSlider.maxValue = Data.MaxHealth;
        healthSlider.minValue = 0;
        healthText.text = $"Health:{Data.CurrentHealth}";
    }

    private void FixedUpdate()
    {
        healthSlider.value = Data.CurrentHealth;
        healthText.text = $"Health:{Data.CurrentHealth}";
    }
}