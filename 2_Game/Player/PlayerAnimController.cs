using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class PlayerAnimController : MonoBehaviour
{
    //用于配合捏脸系统的动画切换控制器
    /**
     * 提供如下接口：
     *      1.根据传入的各部位样式和颜色，切换对应的动画
     *      2.待续...
     */
    List<KeyValuePair<AnimationClip, AnimationClip>> overrideList = new List<KeyValuePair<AnimationClip, AnimationClip>>();

    [SerializeField] List<AnimatorAndClip> animControlAndClips;
    [Serializable]
    [SerializeField]
    private class AnimatorAndClip
    {
        public string Part;
        public Animator _animator;
        public AnimatorOverrideController _animatorOverrideController;
        public List<AnimationClip> IdleAnimation;
        public List<AnimationClip> LeftAnimation;
        public List<AnimationClip> RightAnimation;
    }

    /// <summary>
    /// 切换动画的接口
    /// </summary>
    /// <param name="PartsData"></param>
    public void ReplaceAnimation(Dictionary<string, string> PartsData)
    {
        ReplacePartAnimation(PartsData["BackHair"], animControlAndClips.Find(x => x.Part == "BackHair"));
        ReplacePartAnimation(PartsData["Eyebrows"], animControlAndClips.Find(x => x.Part == "Eyebrows"));
        ReplacePartAnimation(PartsData["Eyes"], animControlAndClips.Find(x => x.Part == "Eyes"));
        ReplacePartAnimation(PartsData["FrontHair"], animControlAndClips.Find(x => x.Part == "FrontHair"));
        ReplacePartAnimation(PartsData["Mouth"], animControlAndClips.Find(x => x.Part == "Mouth"));
    }
    private void Start()
    {
        //初始化动画控制器
        foreach (var ac in animControlAndClips)
        {
            ac._animator.runtimeAnimatorController = ac._animatorOverrideController;
        }

        //overrideList
    }
    private string GetCurrentPartData(string PartData, string type)
    {
        //根据传入的部位数据，返回对应的动画名称
        //例如：传入"BackHair_0_1"，返回"BackHair_Idle_0_1"
        int underscoreIndex = PartData.IndexOf('_');
        if (underscoreIndex >= 0 && type == "Idle")
        {
            // 在第一个下划线之前插入 "_Idle"
            return PartData.Insert(underscoreIndex, "_Idle");
        }
        if (underscoreIndex >= 0 && type == "Right")
        {
            // 在第一个下划线之前插入 "_Right"
            return PartData.Insert(underscoreIndex, "_Right");
        }
        if (underscoreIndex >= 0 && type == "Left")
        {
            // 在第一个下划线之前插入 "_Left"
            return PartData.Insert(underscoreIndex, "_Left");
        }
        // 如果没有下划线（容错），直接返回原字符串
        return PartData;
    }
    private void ReplacePartAnimation(string partData, AnimatorAndClip ac)
    {
        overrideList.Clear();

        ac._animatorOverrideController.GetOverrides(overrideList);

        AnimationClip IdleClip = ac.IdleAnimation.Find(x => x.name == GetCurrentPartData(partData, "Idle"));
        AnimationClip LeftClip = ac.LeftAnimation.Find(x => x.name == GetCurrentPartData(partData, "Left"));
        AnimationClip RightClip = ac.RightAnimation.Find(x => x.name == GetCurrentPartData(partData, "Right"));

        for (int i = 0; i < overrideList.Count; i++)
        {
            AnimationClip originalClip = overrideList[i].Key;
            if (originalClip.name.Contains("Idle"))  // 假设原始 Idle 剪辑命名为 "Idle"
            {
                overrideList[i] = new KeyValuePair<AnimationClip, AnimationClip>(originalClip, IdleClip);
                continue;
            }
            else if (originalClip.name.Contains("Left"))  // 假设原始 Left 剪辑命名为 "Left"
            {
                overrideList[i] = new KeyValuePair<AnimationClip, AnimationClip>(originalClip, LeftClip);
                continue;
            }
            else if (originalClip.name.Contains("Right"))  // 假设原始 Right 剪辑命名为 "Right"
            {
                overrideList[i] = new KeyValuePair<AnimationClip, AnimationClip>(originalClip, RightClip);
                continue;
            }
        }
        ac._animatorOverrideController.ApplyOverrides(overrideList);
    }
}
