using System;
using System.Collections.Generic;
using UnityEngine;

public class AlchemyManager : MonoSingleton<AlchemyManager>, ISaveable
{
    public string SaveKey => "alchemy";
    public int SaveVersion => 1;

    private AlchemyJob _currentJob;
    private int _jobSeq;

    protected override void Start() { base.Start(); SaveManager.Instance?.RegisterModule(this); }

    public class AlchemyJob
    {
        public string jobId;
        public string resultItemId;
        public float remaining;        // 剩余真实秒数
        public int resultCount = 1;
        public bool IsDone => remaining <= 0f;
    }

    public AlchemyJob CurrentJob => _currentJob;
    public bool IsBrewing => _currentJob != null;

    /// <summary>每帧递减（真实时间，跟游戏时间暂停解耦，后台也继续走）。</summary>
    private void Update()
    {
        if (_currentJob == null || _currentJob.IsDone) return;
        _currentJob.remaining -= Time.deltaTime;
    }

    public bool StartBrew(string recipeId)
    {
        if (_currentJob != null) return false;

        var potion = DatabaseManager.Instance.GetPotion(recipeId);
        if (potion?.recipe == null) return false;

        foreach (var ing in potion.recipe.ingredients)
            if (!InventoryManager.Instance.Has(ing.itemId, ing.quantity)) return false;
        foreach (var ing in potion.recipe.ingredients)
            InventoryManager.Instance.Remove(ing.itemId, ing.quantity);

        _currentJob = new AlchemyJob
        {
            jobId = "job_" + (_jobSeq++),
            resultItemId = potion.id,
            remaining = potion.recipe.timeRequired,   // 真实秒
            resultCount = 1
        };
        return true;
    }

    public List<ItemStackData> PeekResult()
    {
        if (_currentJob == null || !_currentJob.IsDone) return null;
        return new List<ItemStackData>
        {
            new ItemStackData { itemId = _currentJob.resultItemId, count = _currentJob.resultCount }
        };
    }

    public List<ItemStackData> TryClaim()
    {
        if (_currentJob == null || !_currentJob.IsDone) return null;
        var gained = new List<ItemStackData>
        {
            new ItemStackData { itemId = _currentJob.resultItemId, count = _currentJob.resultCount }
        };
        InventoryManager.Instance.Add(_currentJob.resultItemId, _currentJob.resultCount);
        _currentJob = null;
        return gained;
    }

    #region ISaveable
    public string SerializeState()
    {
        if (_currentJob == null)
            return JsonUtility.ToJson(new AlchemySaveData { job = null });

        return JsonUtility.ToJson(new AlchemySaveData
        {
            job = new AlchemyJobData
            {
                jobId = _currentJob.jobId,
                remaining = _currentJob.remaining,
                resultItemId = _currentJob.resultItemId,
                resultCount = _currentJob.resultCount
            }
        });
    }

    public void DeserializeState(string json, int version)
    {
        _currentJob = null;
        if (string.IsNullOrEmpty(json)) return;
        var dto = JsonUtility.FromJson<AlchemySaveData>(json);
        if (dto?.job == null) return;
        if (string.IsNullOrEmpty(dto.job.jobId))
        {
            _currentJob = null;
            return;
        }
        _currentJob = new AlchemyJob
        {
            jobId = dto.job.jobId,
            remaining = dto.job.remaining,
            resultItemId = dto.job.resultItemId,
            resultCount = dto.job.resultCount
        };
    }

    public void ResetState() { _currentJob = null; }
    #endregion
}