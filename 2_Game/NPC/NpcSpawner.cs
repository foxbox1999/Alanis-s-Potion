using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

/// <summary>NPC 生成器：每天随机刷新巫师 NPC。第一天必 3 个（含达拉玛），之后每天 0~3 个随机。</summary>
public class NpcSpawner : MonoBehaviour
{
    [Header("生成")]
    [SerializeField] private GameObject[] _npcPrefabs;   // 8 个巫师 prefab（含达拉玛）
    [SerializeField] private GameObject _dalmarPrefab;   // 达拉玛 prefab（第一天必出）
    [SerializeField] private Transform[] _spawnPoints;   // 点位（至少 3 个）
    [Header("交互")]
    [SerializeField] private float _interactRange = 2.5f; // 交互键距离

    private readonly List<NpcInteractable> _active = new();
    private IDisposable _newDaySub;

    private void Start()
    {
        foreach (var prefab in _npcPrefabs)
            if (prefab != null) PoolManager.Instance.Warm(prefab, 1);   // 每个 prefab 预热 1 个

        _newDaySub = EventBus.Instance.Subscribe<NewDayEvent>(_ => Refresh());  // ★ 事件驱动，不用 Update 轮询
        Refresh();
    }
    private void OnApplicationQuit()
    {
        InputManager.Instance.UnregisterInteract(OnInteractKey);
        _newDaySub?.Dispose();
    }

    private void Refresh()
    {
        // 回收昨天的 NPC
        foreach (var npc in _active)
            if (npc != null) PoolManager.Instance.Return(npc.gameObject);
        _active.Clear();

        bool firstDay = WorldManager.Instance.CurrentDay == 0;   // 游戏第一天
        int count = firstDay ? 3 : Random.Range(0, 4);           // 第一天必 3，之后 0~3

        // 1. 随机选点位（洗牌，取前 count 个）★ 补上随机点位
        var points = new List<Transform>(_spawnPoints);
        Shuffle(points);

        // 2. 随机选巫师（洗牌）★ 你原来的随机 NPC 顺序是对的，保留
        var witches = new List<GameObject>();
        foreach (var p in _npcPrefabs) if (p != null) witches.Add(p);
        Shuffle(witches);

        // 3. 配对：第一个随机点位（第一天）放达拉玛，其余随机巫师放随机点位
        int placed = 0;
        if (firstDay && _dalmarPrefab != null)
            SpawnAt(_dalmarPrefab, points[placed++]);

        foreach (var prefab in witches)
        {
            if (placed >= count) break;
            if (firstDay && prefab == _dalmarPrefab) continue;  // 达拉玛已出
            SpawnAt(prefab, points[placed++]);
        }
    }

    private void SpawnAt(GameObject prefab, Transform point)
    {
        GameObject go = PoolManager.Instance.Get(prefab, point);
        go.transform.localPosition = Vector3.zero;
        var npc = go.GetComponent<NpcInteractable>();
        if (npc != null) _active.Add(npc);
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void OnInteractKey()
    {
        Transform player = PlayerManager.Instance.PlayerTransform;
        if (player == null) return;

        NpcInteractable nearest = null;
        float best = _interactRange;
        foreach (var npc in _active)
        {
            if (npc == null) continue;
            float d = Vector3.Distance(player.position, npc.transform.position);
            if (d < best) { best = d; nearest = npc; }
        }
        nearest?.onClicked();
    }
}