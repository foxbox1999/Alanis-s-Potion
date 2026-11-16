using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 角色立绘显示类型
/// </summary>
public enum CharacterDisplayType
{
    Sprite,     // 静态图片
    Live2D      // Live2D模型
}

[CreateAssetMenu(fileName = "CharacterSpriteConfig", menuName = "Dialogue/Character Sprite Config")]
public class CharacterSpriteConfig : ScriptableObject
{
    public List<CharacterData> characters;

    /// <summary>
    /// 根据角色ID获取配置数据
    /// </summary>
    public CharacterData GetCharacter(string characterId)
    {
        return characters.Find(c => c.characterId == characterId);
    }

    public void ChangeNielian()
    {
        CharacterData charData = GetCharacter("艾拉妮丝");
        if (charData == null) return;

        Sprite nielian = PlayerDataManager.Instance != null
            ? PlayerDataManager.Instance.PlayerAppearance
            : null;
        if (nielian == null) return;

        var entry = charData.expressions.Find(e => e.expressionName == "nielian");
        if (entry == null)
        {
            charData.expressions.Add(new ExpressionEntry { expressionName = "nielian", sprite = nielian });
        }
        else
        {
            entry.sprite = nielian;   // ★ 始终更新为最新捏脸，而不是只补 null
        }
    }

    [System.Serializable]
    public class CharacterData
    {
        [Tooltip("角色ID，需与CSV中的人物列一致")]
        public string characterId;

        [Tooltip("显示类型：Sprite（静态图片）或 Live2D（动态模型）")]
        public CharacterDisplayType displayType = CharacterDisplayType.Sprite;

        [Header("静态立绘配置")]
        [Tooltip("默认表情图片")]
        public Sprite defaultSprite;

        [Tooltip("其他表情映射")]
        public List<ExpressionEntry> expressions;

        [Header("Live2D配置")]
        [Tooltip("Live2D模型预制体（需包含CubismModel组件）")]
        public GameObject live2DPrefab;

        // 运行时当前类型
        [System.NonSerialized]
        public CharacterDisplayType currentDisplayType;

        public void Init()
        {
            currentDisplayType = displayType;
        }

        /// <summary>
        /// 根据表情名获取Sprite
        /// </summary>
        public Sprite GetSprite(string expression)
        {
            if (string.IsNullOrEmpty(expression))
                return defaultSprite;

            var entry = expressions.Find(e => e.expressionName == expression);
            return entry != null ? entry.sprite : defaultSprite;
        }
    }

    [System.Serializable]
    public class ExpressionEntry
    {
        [Tooltip("表情名称，对应CSV中的表情列")]
        public string expressionName;

        [Tooltip("表情对应的Sprite")]
        public Sprite sprite;
    }

    public void InitAllCharacters()
    {
        foreach (var character in characters)
        {
            character.Init();
        }
    }
}