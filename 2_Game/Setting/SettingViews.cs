using UnityEngine;
using UnityEngine.UI;

/// <summary>设置窗口：预览（改动立即生效）→ 确定保存 / 取消或关闭回滚。</summary>
public class SettingViews : BaseUIView
{
    [Header("音量")]
    [SerializeField] private Slider bgmVolume;      // 音乐音量
    [SerializeField] private Slider sfxVolume;      // 音效音量

    [Header("窗口")]
    [SerializeField] private GameObject WindowsControl;
    [SerializeField] private Slider WindowSilder;   // 0=全屏 1=窗口

    [Header("确认/取消")]
    [SerializeField] private Button SaveSetButton;
    [SerializeField] private Button CancelButton;

    private float _pMaster, _pBgm, _pSfx;
    private bool _pFullScreen;
    private bool _confirmed;

    protected override void OnOpen()
    {
        base.OnOpen();
        var s = ConfigManager.Instance.CurrentSettings;
        if (s == null) return;

        _pMaster = s.masterVolume; _pBgm = s.bgmVolume; _pSfx = s.sfxVolume;
        _pFullScreen = s.isFullScreen;
        _confirmed = false;


        bgmVolume.value = _pBgm;
        sfxVolume.value = _pSfx;
        if (WindowSilder != null) WindowSilder.value = _pFullScreen ? 0 : 1;

        bgmVolume.onValueChanged.AddListener(v => { _pBgm = v; PreviewAudio(); });
        sfxVolume.onValueChanged.AddListener(v => { _pSfx = v; PreviewAudio(); });
        if (WindowSilder != null) WindowSilder.onValueChanged.AddListener(v => { _pFullScreen = v < 0.5f; PreviewScreen(); });

        SaveSetButton.onClick.AddListener(OnSave);
        CancelButton.onClick.AddListener(() => CloseSelf());
    }

    private void PreviewAudio()
    {
        AudioManager.Instance.SetMasterVolume(_pMaster);
        AudioManager.Instance.SetBGMVolume(_pBgm);
        AudioManager.Instance.SetSFXVolume(_pSfx);
    }

    private void PreviewScreen()
    {
        if (_pFullScreen)
            Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow);
        else
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
    }

    private void OnSave()
    {
        var c = ConfigManager.Instance;
        c.SetMasterVolume(_pMaster);
        c.SetBGMVolume(_pBgm);
        c.SetSFXVolume(_pSfx);
        c.SetFullScreen(_pFullScreen);
        c.SaveSettings();
        c.ApplySettings();   // 屏幕 + 广播 SettingsChangedEvent → AudioManager 应用
        _confirmed = true;
        CloseSelf();
    }

    protected override void OnClose()
    {
        // 未点确定就关闭（取消按钮 / 返回按钮 / 关窗口）→ 回滚
        if (!_confirmed)
        {
            var s = ConfigManager.Instance.CurrentSettings;
            if (s != null)
            {
                AudioManager.Instance.SetMasterVolume(s.masterVolume);
                AudioManager.Instance.SetBGMVolume(s.bgmVolume);
                AudioManager.Instance.SetSFXVolume(s.sfxVolume);
                Screen.SetResolution(s.resolutionWidth, s.resolutionHeight, s.isFullScreen);
            }
        }
        base.OnClose();
    }
}