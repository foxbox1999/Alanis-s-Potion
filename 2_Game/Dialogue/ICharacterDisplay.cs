using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICharacterDisplay 
{
    /// <summary>
    /// 显示立绘
    /// </summary>
    /// <param name="parent">父容器</param>
    /// <param name="data">角色配置数据</param>
    /// <param name="expression">表情名称</param>
    void Show(Transform parent, CharacterSpriteConfig.CharacterData data, string expression);

    /// <summary>
    /// 隐藏立绘
    /// </summary>
    void Hide();

    /// <summary>
    /// 切换表情（不改变其他状态）
    /// </summary>
    void SetExpression(string expression, CharacterSpriteConfig.CharacterData data);
}
