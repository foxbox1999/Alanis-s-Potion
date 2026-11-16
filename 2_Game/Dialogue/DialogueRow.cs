using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueRow
{
    public string flag;          // "#"=对白, "&"=选项, "END"=段落结束
    public int id;               // 全局唯一ID
    public string characterId;   // 角色内部ID
    public string expression;    // 表情名称
    public string content;       // 对白文本 / 选项文本
    public int nextId;           // 跳转目标ID（对白行指向下一句，选项行指向分支，END行为-1）
    public string effect;        // 效果指令（如 "好感度加@1"）
    public string target;        // 效果目标角色ID
}
