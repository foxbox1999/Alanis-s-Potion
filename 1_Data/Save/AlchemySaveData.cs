using System;

[Serializable]
public class AlchemyJobData
{
    public string jobId;
    public float remaining;       // 剩余真实秒数
    public string resultItemId;
    public int resultCount;
}

[Serializable]
public class AlchemySaveData
{
    public AlchemyJobData job;
}