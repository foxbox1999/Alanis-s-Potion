using TMPro;
using UnityEngine;

/// <summary>货币显示：放在任意 UI 下，有存档时实时显示金币/银币，无存档时清空。放好即用，无需额外操作。</summary>
public class CoinDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _goldText;    // 金币文本（留空则自动找）
    [SerializeField] private TextMeshProUGUI _silverText;  // 银币文本（留空则自动找）

    private int _lastGold = -1;      // 缓存，变了才刷，避免每帧写文本
    private int _lastSilver = -1;

    private void Start()
    {
        // 没手动拖就自动找子物体里的 TMP：第一个当金币、第二个当银币
        var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        if (_goldText == null && texts.Length >= 1) _goldText = texts[0];
        if (_silverText == null && texts.Length >= 2) _silverText = texts[1];
    }

    private void Update()
    {
        bool hasSave = SaveManager.Instance != null && SaveManager.Instance.CurrentSlot != -1;

        // 没存档：清空隐藏
        if (!hasSave)
        {
            if (_goldText != null) _goldText.text = "";
            if (_silverText != null) _silverText.text = "";
            _lastGold = -1;
            _lastSilver = -1;
            return;
        }

        // 有存档：实时刷新
        int gold = PlayerDataManager.Instance.GoldCoins;
        int silver = PlayerDataManager.Instance.SilverCoins;

        if (_goldText != null && gold != _lastGold) { _lastGold = gold; _goldText.text = gold.ToString(); }
        if (_silverText != null && silver != _lastSilver) { _lastSilver = silver; _silverText.text = silver.ToString(); }
    }
}