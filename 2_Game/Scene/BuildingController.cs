using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class BuildingController : MonoBehaviour
{
    [Serializable]
    public class Part
    {
        public string partName;            // 统一后的名字，如 "蛋糕店"，用于按名字加载
        public SpriteRenderer renderer;    // 场景里已有的主 SpriteRenderer
        [HideInInspector] public SpriteRenderer fadeRenderer;  // 运行时创建，用于平滑过渡
    }

    [SerializeField] private List<Part> _parts;            // 这个建筑的多个 part
    [SerializeField] private string _folder = "Images/Buildings/street";  // street 或 教堂
    [SerializeField] private float _fadeDuration = 2.5f;    // 平滑过渡时长

    private TimeOfDay _current = TimeOfDay.None;
    private IDisposable _sub;

    private void OnEnable()
    {
        _sub = EventBus.Instance.Subscribe<TimeOfDayChangedEvent>(e => StartCoroutine(CrossFade(e.TimeOfDay)));
        // 初始化：按当前时间设置初始图
        ApplyInstant(TimeOfDay.None);
    }
    private void OnDisable() => _sub?.Dispose();

    private Sprite Load(Part p, TimeOfDay tod)
        => Resources.Load<Sprite>($"{_folder}/{tod}/{p.partName}");

    // 首次：直接赋值，不平滑
    private void ApplyInstant(TimeOfDay tod)
    {
        if (tod == TimeOfDay.None)
            tod = TimeOfDay.Day;
        foreach (var p in _parts) p.renderer.sprite = Load(p, tod);
        _current = tod;
    }

    // 平滑过渡：下一时段图淡入，覆盖旧图
    private IEnumerator CrossFade(TimeOfDay target)
    {
        float t = 0;
        foreach (var p in _parts)
        {
            EnsureFadeRenderer(p);
            p.fadeRenderer.sprite = Load(p, target);
            p.fadeRenderer.color = new Color(1, 1, 1, 0);
        }

        while (t < _fadeDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / _fadeDuration);
            foreach (var p in _parts)
                p.fadeRenderer.color = new Color(1, 1, 1, a);  // 下一时段图淡入
            yield return null;
        }

        // 结束：旧图换成新图，隐藏过渡层
        foreach (var p in _parts)
        {
            p.renderer.sprite = p.fadeRenderer.sprite;
            p.fadeRenderer.color = new Color(1, 1, 1, 0);
        }
        _current = target;
    }

    // 给 part 建一个「过渡层」SpriteRenderer（盖在主图上，排序+1）
    private void EnsureFadeRenderer(Part p)
    {
        if (p.fadeRenderer != null) return;
        var go = new GameObject(p.partName + "_fade");
        go.transform.SetParent(p.renderer.transform, false);
        p.fadeRenderer = go.AddComponent<SpriteRenderer>();
        p.fadeRenderer.sortingOrder = p.renderer.sortingOrder + 1;
        p.fadeRenderer.color = new Color(1, 1, 1, 0);
    }
}