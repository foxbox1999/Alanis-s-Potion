using UnityEngine;

/// <summary>弹窗 View 基类：读 PopupData、绑定按钮、关闭自己。</summary>
public abstract class PopupViewBase : BaseUIView
{
    /// <summary>当前弹窗数据。</summary>
    protected PopupData Popup => Data as PopupData;

    protected override void OnOpen()
    {
        Bind();
        base.OnOpen();
        GameManager.Instance.EnterBusy();
    }

    protected override void OnClose()
    {
        base.OnClose();
        GameManager.Instance.ExitBusy();
    }
    /// <summary>子类实现：读 Popup 数据、绑定按钮与文本。</summary>
    protected abstract void Bind();

    /// <summary>确认并关闭。</summary>
    protected void ConfirmAndClose()
    {
        Popup?.onConfirm?.Invoke();
        UIManager.Instance.ClosePanel(PanelKey);
    }

    /// <summary>取消并关闭。</summary>
    protected void CancelAndClose()
    {
        Popup?.onCancel?.Invoke();
        UIManager.Instance.ClosePanel(PanelKey);
    }
}