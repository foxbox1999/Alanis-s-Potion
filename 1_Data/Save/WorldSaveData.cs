using System;
using System.Collections.Generic;

/// <summary>
/// 世界存档数据（WorldManager 的序列化 DTO）。
/// JsonUtility 约束：不能用 long / Dictionary，所以旗标和区域用 List<string>。
/// </summary>
[Serializable]
public class WorldSaveData
{
    /// <summary>下次交租的绝对游戏日。</summary>
    public int nextRentDay;

    /// <summary>已解锁采集区域 id（未来用）。</summary>
    public List<string> unlockedRegions = new List<string>();

    /// <summary>世界旗标（未来用）。</summary>
    public List<string> worldFlags = new List<string>();
}