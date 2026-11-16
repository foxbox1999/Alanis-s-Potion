using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>症状项：Toggle 选中/取消，触发筛选。</summary>
public class SymptomItem : MonoBehaviour
{
    [SerializeField] private Toggle _toggle;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private Image _background;

    public string SymptomId { get; private set; }

    private void Awake()
    {
        if (_toggle == null) _toggle = GetComponentInChildren<Toggle>();
        if (_nameText == null) _nameText = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void Setup(string symptomId, string name, Action<string, bool> onToggle)
    {
        SymptomId = symptomId;
        if (_nameText != null) _nameText.text = name;

        _toggle.onValueChanged.RemoveAllListeners();
        _toggle.onValueChanged.AddListener(isOn => onToggle?.Invoke(SymptomId, isOn));
    }

    public void SetSelected(bool selected){
        transform.localScale = selected ? Vector3.one * 1.05f : Vector3.one;
        _toggle.isOn = selected;
    } 
}