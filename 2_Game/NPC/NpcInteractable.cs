using UnityEngine;
using UnityEngine.UI;
public class NpcInteractable : MonoBehaviour
{
    [SerializeField] private string _npcId;          // bake 进 prefab
    [SerializeField] private int _defaultDialogueId; // bake 进 prefab
    [SerializeField] private Image _aimImage; 
    [Header("交互距离")]
    [SerializeField] private float _interactRange = 2.5f;
    private Transform _player;
    public string NpcId => _npcId;

    private void Update()
    {
        // 玩家运行时才实例化，没找到就下帧再找（懒查找）
        if (_player == null) _player = PlayerManager.Instance.PlayerTransform;
        if (_player == null) return;

        float dist = Vector3.Distance(_player.position, transform.position);
        bool canClick = dist <= _interactRange;

        // ★ 用目标图片的 raycastTarget 控制能否点击：false 点不到、也不变灰
        if (_aimImage != null)
            _aimImage.raycastTarget = canClick;
    }

    public void onClicked()
    {
        int id = DialogueRouter.GetStartId(_npcId, _defaultDialogueId);
        EventBus.Instance.Publish(new RequestDialogue { StartID = id, NeedBlackFade = false });
    }
}