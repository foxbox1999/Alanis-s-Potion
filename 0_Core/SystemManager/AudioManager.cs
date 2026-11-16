using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>音频管理器：预加载 + 同步播放 + 双源交叉淡入淡出 + 音量控制。</summary>
public class AudioManager : MonoSingleton<AudioManager>
{
    #region 配置
    [SerializeField] private AudioMixer audioMixer;
    private const string MASTER_VOL_KEY = "Master";
    private const string BGM_VOL_KEY = "BGM";
    private const string SFX_VOL_KEY = "SFX";

    [Header("音频源")]
    [SerializeField] private AudioSource bgmSourceA;
    [SerializeField] private AudioSource bgmSourceB;
    private AudioSource activeBgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("音量曲线")]
    public float volumeCurveExponent = 3f;

    [Header("预加载")]
    [Tooltip("启动时预加载的音频 key（Resources 相对路径，如 Audio/SFX/通用提示）")]
    [SerializeField] private string[] preloadKeys;
    #endregion

    #region 运行时
    private readonly Dictionary<string, AudioClip> _clips = new();
    private Coroutine bgmFadeCoroutine;
    private IEventBus _eventBus;
    private Dictionary<string, IDisposable> _subscriptions;
    #endregion

    #region 生命周期
    protected override void Awake()
    {
        base.Awake();
        activeBgmSource = bgmSourceA;
        foreach (var key in preloadKeys) GetClip(key);   // 启动时预热，消除首次播放延迟
        SubEvents();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (_subscriptions != null)
        {
            foreach (var sub in _subscriptions.Values) sub?.Dispose();
            _subscriptions.Clear();
        }
    }

    private void SubEvents()
    {
        _eventBus = EventBus.Instance;
        SafeSubscribe<SettingsChangedEvent>("SettingsChanged", _ => LoadVolumesFromConfig());
    }
    #endregion

    #region 音量控制
    private void LoadVolumesFromConfig()
    {
        var s = ConfigManager.Instance.CurrentSettings;
        SetMasterVolume(s.masterVolume);
        SetBGMVolume(s.bgmVolume);
        SetSFXVolume(s.sfxVolume);
    }

    public void SetMasterVolume(float v) => audioMixer?.SetFloat(MASTER_VOL_KEY, ToDB(v));
    public void SetBGMVolume(float v) => audioMixer?.SetFloat(BGM_VOL_KEY, ToDB(v));
    public void SetSFXVolume(float v) => audioMixer?.SetFloat(SFX_VOL_KEY, ToDB(v));

    private float ToDB(float value)
    {
        float safe = Mathf.Max(Mathf.Clamp01(value), 0.0001f);
        return Mathf.Clamp(20f * volumeCurveExponent * Mathf.Log10(safe), -80f, 0f);
    }
    #endregion

    #region 音频加载（同步 + 缓存）
    private AudioClip GetClip(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (_clips.TryGetValue(key, out var c) && c != null) return c;

        var clip = Resources.Load<AudioClip>(key);   // Resources 自带缓存，配合 _clips 做 O(1) 查找
        if (clip != null) _clips[key] = clip;
        else Debug.LogWarning($"[AudioManager] 音频加载失败: {key}");
        return clip;
    }
    #endregion

    #region BGM（同步切换 + 交叉淡入淡出）
    /// <summary>按昼夜播放（白天 key / 夜晚 key）。</summary>
    public void PlayBGMByTime(string dayKey, string nightKey)
    {
        var tod = DayCycleManager.GetTimeOfDay(TimeManager.Instance.TimeData.Hour);
        bool night = tod == TimeOfDay.Night || tod == TimeOfDay.Dusk;
        PlayBGM(night ? nightKey : dayKey);
    }

    public void PlayBGM(string key, float fadeDuration = 1f)
    {
        AudioClip clip = GetClip(key);
        if (clip == null) return;
        if (activeBgmSource.isPlaying && activeBgmSource.clip == clip) return;

        AudioSource next = (activeBgmSource == bgmSourceA) ? bgmSourceB : bgmSourceA;
        next.clip = clip;
        next.volume = 0f;
        next.Play();

        if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
        bgmFadeCoroutine = StartCoroutine(CrossFadeBGM(next, fadeDuration));
    }

    private IEnumerator CrossFadeBGM(AudioSource newSource, float duration)
    {
        AudioSource oldSource = activeBgmSource;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.Clamp01(elapsed / duration);
            s = s * s * (3f - 2f * s);   // SmoothStep
            oldSource.volume = Mathf.Lerp(1f, 0f, s);
            newSource.volume = Mathf.Lerp(0f, 1f, s);
            yield return null;
        }
        oldSource.volume = 0f;
        newSource.volume = 1f;
        oldSource.Stop();
        activeBgmSource = newSource;
        bgmFadeCoroutine = null;
    }
    #endregion

    #region SFX（同步播放 + PlayOneShot 允许叠加，不打断正在响的音效）
    public void PlaySFX(string key)
    {
        AudioClip clip = GetClip(key);
        if (clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    public void StopAll()
    {
        activeBgmSource.Stop();
        sfxSource.Stop();
        if (bgmFadeCoroutine != null) { StopCoroutine(bgmFadeCoroutine); bgmFadeCoroutine = null; }
    }
    #endregion

    #region 工具
    private void SafeSubscribe<T>(string key, Action<T> action, int priority = 0) where T : EventBase
    {
        if (_eventBus == null) return;
        if (_subscriptions == null) _subscriptions = new Dictionary<string, IDisposable>();
        if (_subscriptions.TryGetValue(key, out var oldSub)) oldSub?.Dispose();
        _subscriptions[key] = _eventBus.Subscribe(action, priority);
    }
    #endregion
}