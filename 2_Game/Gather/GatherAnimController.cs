using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GatherAnimController : MonoBehaviour
{

    private void onAnimOver()
    {
        var gained = GatherManager.Instance.CompleteGather();
        if (gained != null && gained.Count > 0)
            PopupManager.Instance.ShowItem(gained);
        GameManager.Instance.ExitBusy();//释放占用
        transform.parent.gameObject.SetActive(false);
    }
}
