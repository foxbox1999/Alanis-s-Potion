using System;

/// <summary>
/// 游戏时间数据类，用于存储和管理游戏内的时间信息
/// </summary>
[Serializable]
public class GameTimeData
{
    /// <summary>
    /// 游戏内年份
    /// </summary>
    public int Year;
    /// <summary>
    /// 游戏内月份，其他同理
    /// </summary>
    public int Month;
    public int Day;
    public int Hour;
    public int Minute;
    public int Second;
    /// <summary>
    /// 游戏内总秒数
    /// </summary>
    public float TotalSeconds;

    // 时间缩放（独立于 Time.timeScale）
    public bool IsPaused = false;

    public GameTimeData()
    {
        Year = 1509;
        Month = 4;
        Day = 20;
        Hour = 8;
        Minute = 0;
        Second = 0;
        TotalSeconds = 0;
        IsPaused = false;
    }

    public void UpdateFromTotalSeconds(float totalSec)
    {
        TotalSeconds = totalSec;
        // 简易转换：1天 = 86400秒（现实秒对应游戏秒，但实际游戏中可能需要1现实秒=1游戏分钟）
        // 这里采用 "1现实秒 = 1游戏分钟" 的常用设计，让游戏时间流逝更快
        // 但为了演示，我们直接使用 1:1 映射，方便调试
        // 实际项目中可改为：int totalGameMinutes = (int)(totalSec / 60f);

        // 简单实现：从起始日期 + 总秒数推算
        DateTime baseTime = new DateTime(1509, 4, 20, 8, 0, 0);
        DateTime current = baseTime.AddSeconds(totalSec);

        Year = current.Year;
        Month = current.Month;
        Day = current.Day;
        Hour = current.Hour;
        Minute = current.Minute;
        Second = current.Second;
    }

    // ===== 便捷方法：获取 DateTime 对象 =====
    public DateTime ToDateTime()
    {
        return new DateTime(Year, Month, Day, Hour, Minute, Second);
    }

    public override string ToString()
    {
        return $"{Year:0000}/{Month:00}/{Day:00} {Hour:00}:{Minute:00}:{Second:00} (总秒数: {TotalSeconds:F1}),Paused:{IsPaused}";
    }
}
