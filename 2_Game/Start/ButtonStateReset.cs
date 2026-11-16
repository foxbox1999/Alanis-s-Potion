using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonStateReset : MonoBehaviour, IPointerUpHandler, IPointerExitHandler
{
    private Button button;

    void Start()
    {
        button = GetComponent<Button>();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // 当鼠标松开时，如果按钮没有被点击（例如移出后松开），强制进入 Normal 状态
        if (!eventData.fullyExited) // 或者根据你的逻辑判断
        {
            // 重置按钮状态（需要访问动画触发器或直接与Animator交互）
            if (button.transition == Selectable.Transition.Animation)
            {
                Animator anim = button.animator;
                if (anim != null)
                {
                    anim.ResetTrigger("Pressed");
                    anim.ResetTrigger("Selected");
                    anim.SetTrigger("Normal"); // 假设你有一个Normal触发器
                }
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 如果鼠标移出时按钮处于按下状态，但尚未松开，可以在这里处理
        if (button.transition == Selectable.Transition.Animation)
        {
            Animator anim = button.animator;
            if (anim != null)
            {
                anim.ResetTrigger("Pressed");
                anim.SetTrigger("Normal");
            }
            if (EventSystem.current.currentSelectedGameObject == gameObject)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }
    }
}