using Data;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

/// <summary>
/// 개별 보상 베이스 클래스
/// 실제 보상(잼, 카드, 버프 등)은 이 클래스를 상속받음
/// </summary>
[System.Serializable]
public abstract class BaseReward
{
    [Tooltip("가중치")]
    [Range(0, 100)]
    public float Weight = 1f;

    [Tooltip("희귀도")]
    public RewardRarity Rarity = RewardRarity.Common;

    /// <summary>
    /// (현재 구조에서는 사용되지 않지만)
    /// 독립 확률 방식으로 보상을 실행할 수 있는 함수
    /// </summary>
    public bool TryExecute(GameEntity source)
    {
        if (UnityEngine.Random.value > Weight)
            return false;

        Execute(source);
        return true;
    }

    /// <summary>
    /// 실제 보상 지급 로직
    /// </summary>
    public abstract void Execute(GameEntity source);
}

/// <summary>
/// 보상 테이블
/// 몬스터, 보물상자, 이벤트 등이 이 테이블을 참조
/// </summary>
[CreateAssetMenu(menuName = "RewardTable")]
public class RewardTable : ScriptableObject
{
    [Header("확정 드롭")]
    [SerializeReference]
    public List<BaseReward> guaranteedRewards = new();

    [Header("랜덤 드롭")]
    [SerializeReference]
    public List<BaseReward> randomRewards = new();

    [Tooltip("랜덤 보상 중 하나만 드롭")]
    // false인 경우 여러 보상이 나옴, 주로 상자에서 여러 재료 드롭, 잡몹 파밍 보상에 적합함.
    public bool dropOnlyOne;

    /// <summary>
    /// 보상 테이블 실행
    /// </summary>
    public void Execute(GameEntity source)
    {
        // 1️ 확정 보상은 조건 없이 전부 지급
        foreach (var reward in guaranteedRewards)
        {
            if (reward != null)
                reward.Execute(source);
        }

        // 2️ 랜덤 보상은 가중치 기반 처리
        ExecuteWeightedRandom(source);
    }

    /// <summary>
    /// 가중치 기반 랜덤 보상 처리
    /// </summary>
    private void ExecuteWeightedRandom(GameEntity source)
    {
        // Weight가 0 이하인 보상은 제외
        var validRewards = randomRewards
            .Where(r => r != null && r.Weight > 0f)
            .ToList();

        if (validRewards.Count == 0)
            return;

        // 하나만 드롭
        if (dropOnlyOne)
        {
            var reward = PickOneByWeight(validRewards);
            reward?.Execute(source);
        }
        // 여러 개 가능
        else
        {
            // 여러 개 허용 → 각 보상 개별 판정
            foreach (var reward in validRewards)
            {
                // 상대 가중치 기반 개별 판정
                if (UnityEngine.Random.value <
                    reward.Weight / GetTotalWeight(validRewards))
                {
                    reward.Execute(source);
                }
            }
        }
    }

    /// <summary>
    /// 가중치 비율에 따라 하나의 보상을 선택
    /// </summary>
    private BaseReward PickOneByWeight(List<BaseReward> rewards)
    {
        // 전체 가중치 합
        float totalWeight = rewards.Sum(r => r.Weight);

        // 0 ~ totalWeight 사이 랜덤 값
        float roll = UnityEngine.Random.value * totalWeight;

        float cumulative = 0f;

        // 누적 가중치를 넘는 순간 선택
        foreach (var reward in rewards)
        {
            cumulative += reward.Weight;
            if (roll <= cumulative)
                return reward;
        }

        return null;
    }


    /// <summary>
    /// 유효한 보상들의 가중치 총합 계산
    /// </summary>
    private float GetTotalWeight(List<BaseReward> rewards)
    {
        return rewards
            .Where(r => r != null && r.Weight > 0f)
            .Sum(r => r.Weight);
    }


#if UNITY_EDITOR
    public Dictionary<System.Type, int> SimulateRandomDrop(int iterations)
    {
        var result = new Dictionary<System.Type, int>();

        var validRewards = randomRewards
            .Where(r => r != null && r.Weight > 0f)
            .ToList();

        if (validRewards.Count == 0)
            return result;

        for (int i = 0; i < iterations; i++)
        {
            if (dropOnlyOne)
            {
                var picked = PickOneByWeight(validRewards);
                if (picked == null) continue;

                var type = picked.GetType();
                if (!result.ContainsKey(type))
                    result[type] = 0;

                result[type]++;
            }
            else
            {
                float totalWeight = GetTotalWeight(validRewards);

                foreach (var reward in validRewards)
                {
                    if (UnityEngine.Random.value <
                        reward.Weight / totalWeight)
                    {
                        var type = reward.GetType();
                        if (!result.ContainsKey(type))
                            result[type] = 0;

                        result[type]++;
                    }
                }
            }
        }

        return result;
    }
#endif

}


[System.Serializable]
[RewardDisplayName("Down Jam")]
public class RewardDownJam : BaseReward
{
    public int downJamMin;
    public int downJamMax;

    public override void Execute(GameEntity source)
    {
        int jam = UnityEngine.Random.Range(downJamMin, downJamMax + 1);
        Managers.SceneServices.InventoryWrite.AddDownJam(jam);
    }
}

[System.Serializable]
[RewardDisplayName("Card")]
public class RewardCard : BaseReward
{
    //public string cardId;
    public GameEntity m_GameEntity;

    public override void Execute(GameEntity source)
    {
        var ui = Managers.SceneServices.BuildingCardUI;
        ui?.AddCard(m_GameEntity, source.transform.position);
    }
}

[System.Serializable]
[RewardDisplayName("Buff")]
public class RewardBuff : BaseReward
{
    public int buffId;

    public override void Execute(GameEntity source)
    {
        Debug.Log($"버프 획득: {buffId}");
    }
}

[System.Serializable]
[RewardDisplayName("Effect")]
public class RewardEffect : BaseReward
{
    public int effectId;

    public override void Execute(GameEntity source)
    {
        Debug.Log($"이펙트 발동: {effectId}");
    }
}



[AttributeUsage(AttributeTargets.Class)]
public class RewardDisplayNameAttribute : Attribute
{
    public string Name;

    public RewardDisplayNameAttribute(string name)
    {
        Name = name;
    }
}

