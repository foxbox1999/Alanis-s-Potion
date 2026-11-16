using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum LocationId
{
    Garden,
    StreetRight,
    StreetLeft,
    Church,
    Triumphal,
    Home
}

/// <summary>
/// 玩家移动的场景的定位
/// </summary>
public static class Location 
{
    public readonly static Vector3 _garden = new Vector3(222.4335f, -5.682965f, 0);
    public readonly static Vector3 _streetRight = new Vector3(63.94256f, -3.406294f, 0);
    public readonly static Vector3 _streetLeft = new Vector3(-59.98648f, -6.619714f, 0);
    public readonly static Vector3 _church = new Vector3(97.16315f, -4.185003f, 0);
    public readonly static Vector3 _triumphal = new Vector3(-17.93034f, -5.52947f, 0);
    public readonly static Vector3 _home = new Vector3(5.729058f, -4.916146f, 0);

    // 枚举 → 坐标
    public static Vector3 GetPosition(LocationId id) => id switch
    {
        LocationId.Garden => _garden,
        LocationId.StreetRight => _streetRight,
        LocationId.StreetLeft => _streetLeft,
        LocationId.Church => _church,
        LocationId.Triumphal => _triumphal,
        LocationId.Home => _home,
        _ => _home
    };

    // 坐标 → 枚举（带容差，因为浮点不能精确比较）
    private static readonly (LocationId id, Vector3 pos)[] _all =
    {
        (LocationId.Garden, _garden),
        (LocationId.StreetRight, _streetRight),
        (LocationId.StreetLeft, _streetLeft),
        (LocationId.Church, _church),
        (LocationId.Triumphal, _triumphal),
        (LocationId.Home, _home),
    };

    public static LocationId? GetIdByPosition(Vector3 pos, float tolerance = 0.5f)
    {
        foreach (var (id, p) in _all)
        {
            if (Vector3.Distance(p, pos) <= tolerance)
                return id;
        }
        return null; // 不在任何已知位置
    }
}
