using UnityEngine;
using UnityEngine.UI;

/// <summary>距离显隐：随玩家靠近渐显，完全显现/靠近才可点击。用 CanvasGroup，避免 interactable=false 变灰。</summary>
public class ProximityReveal : MonoBehaviour
{
    [SerializeField] private float _startDistance = 8f;   // 开始显现的距离（再远看不见）
    [SerializeField] private float _fullDistance = 3f;    // 完全显现/可点击的距离
    [SerializeField] private bool _fade = true;           // true=渐显（按钮），false=始终可见只挡点击（NPC）
    [SerializeField] private Image buttonImage;

    private Transform _player;
    private Color _baseColor;
    private void Start()
    {
        if (buttonImage == null) buttonImage = GetComponent<Image>();
        if (buttonImage != null) _baseColor = buttonImage.color;   // 记住原始颜色，只改 alpha
        _player = PlayerManager.Instance.PlayerTransform;
    }

    private void Update()
    {
        if (_player == null) _player = PlayerManager.Instance.PlayerTransform;   // 懒查找
        if (_player == null || buttonImage == null) return;

        float dist = Vector3.Distance(_player.position, transform.position);
        float t = Mathf.InverseLerp(_startDistance, _fullDistance, dist);  // 远=0，近=1

        _baseColor.a = _fade ? t : 1f;
        buttonImage.color = _baseColor;

        // 射线（可点击）：完全显现/靠近才开，不变灰
        buttonImage.raycastTarget = t >= 1f;
    }

    public void Test()
    {
        Debug.Log("点击");
    }
}