using Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static Define;
using static Util;


[RequireComponent(typeof(SetupAnimation), typeof(Poolable))]
public class ControllableObject : GameEntity
{
    // TODO 인터페이스로 뺴기
    public event EventHandler<OnChangeGradeEventArgs> OnChangeGrade;
    public class OnChangeGradeEventArgs: EventArgs
    {
        public E_ObjectGrade objGrade;
        public E_ObjectEnhanceType gradeEnhanceType;
        public float enhanceValue;
        public bool isSuccessGrade;
    }

    [Header("Grade")]
    public E_ObjectGrade m_originalEObjectGrade; //원래 등급
    public E_ObjectGrade m_EObjectGrade; //조정된 등급
    public OnChangeGradeEventArgs m_OnChangeGradeEventArgs; // 조정 수치
    [SerializeField] [Range(0, 100)] private float m_fEnhanceChance;
    [SerializeField] private List<E_ObjectEnhanceType> n_EnhanceTypeList;

    protected override void Awake()
    {
        base.Awake();

        // Event
        OnChangeGrade += ChangeMaterialOfGrade;
    }

    protected override void Start()
    {
        base.Start();

        if(m_originalEObjectGrade != m_EObjectGrade)
        {
            OnChangeGrade?.Invoke(this, m_OnChangeGradeEventArgs);
        }
    }

    #region Action

    public void DirectCommand<TAction>
        (TAction toChangeAction,
        GridPosition destGridPosition = default) 
        where TAction : BaseAction
    {
        m_BeforeAction = m_CurrentAction;

        // 순서 대기
        m_ActionQueue.Enqueue((toChangeAction, destGridPosition));
    }
    #endregion

    #region Grade

    // 등급 강화 시도
    public void TryEnhanceGrade()
    {
        if(n_EnhanceTypeList.Count == 0)
        {
            Debug.Log($"강화 타입이 없습니다. {name}");
            return;
        }

        float value = Mathf.Round(UnityEngine.Random.Range(0f, 100f) * 100f) / 100f;

        // 강화 성공
        if (value < m_fEnhanceChance)
        {
            m_EObjectGrade = E_ObjectGrade.Elite;
        }
        // 원래 등급으로
        else
        {
            m_EObjectGrade = m_originalEObjectGrade;
        }

        m_OnChangeGradeEventArgs = new OnChangeGradeEventArgs()
        {
            objGrade = m_EObjectGrade,
            enhanceValue = GetRandomValue(1.2f, 1.5f, 0.1f),
            gradeEnhanceType = n_EnhanceTypeList.RandomPick(),
            isSuccessGrade = m_EObjectGrade != m_originalEObjectGrade
        };

        // 업그레이드 실행
        OnChangeGrade?.Invoke(this, m_OnChangeGradeEventArgs);
    }

    // 등급 변화에 따른 변화
    private void ChangeMaterialOfGrade(object sender, OnChangeGradeEventArgs args)
    {
        switch (args.objGrade)
        {
            case E_ObjectGrade.Normal:
                ChangeMaterialOutlineColor(GetModelsMaterial(), Color.white);   // 아웃라인 효과
                break;
            case E_ObjectGrade.Elite:
                ChangeMaterialOutlineColor(GetModelsMaterial(), Color.yellow);   // 아웃라인 효과
                break;
            case E_ObjectGrade.Boss:
                ChangeMaterialOutlineColor(GetModelsMaterial(), Color.red);    // 아웃라인 효과 
                break;
            default:
                break;
        }
    }

    private void ChangeMaterialOutlineColor(IEnumerable<(Material, GameObject obj)> materials, Color color)
    {
        foreach (var material in materials)
        {
            if(material.Item1.HasProperty("_OutlineColor"))
            {
                material.Item1.SetColor("_OutlineColor", color);
            }
        }
    }

    #endregion

    #region Data Save & Load

    public override BaseData CaptureSaveData()
    {
        var baseData =  base.CaptureSaveData() as GameEntityData;

        return new ControllableObjectData()
        {
            // 공통 필드 복사
            prefabName = baseData.prefabName,
            position = baseData.position,
            rotation = baseData.rotation,
            guid = baseData.guid,
            attributeSystemData = baseData.attributeSystemData,
            gradeArgs = m_OnChangeGradeEventArgs,

            // 하위 클래스 고유 데이터 추가
            attackReadyItemData =
                m_CombatManager?.m_AttackReadyItemObject.Select(item => item.obj.CaptureSaveData()).ToList(),

            readyAttackPatternData =
               m_CombatManager?.m_ReadyAttackPattern != null
                   ? m_CombatManager.m_ReadyAttackPattern
                       .Select(attack => attack?.CaptureSaveData())
                       .Where(data => data != null)
                       .ToHashSet()
                   : new HashSet<AttackPatternData>(),

            targetGuid = m_Target?.guid
        };
    }

    public override void RestoreSaveData(BaseData data)
    {
        base.RestoreSaveData(data);

        ControllableObjectData cData = data as ControllableObjectData;

        // readyAttackPatternData가 null이 아니고, 비어있지 않을 때만 복원
        if (cData.readyAttackPatternData != null && cData.readyAttackPatternData.Count > 0)
        {
            m_CombatManager.m_ReadyAttackPattern =
                m_AttributeSystem.m_AttackPatterns
                    .Where(a => cData.readyAttackPatternData.Any(b => a.ID == b.id))
                    .OfType<AttackPattern_Ready>() // 타입 안전 변환
                    .ToHashSet();
        }

        m_OnChangeGradeEventArgs = cData.gradeArgs;
        m_EObjectGrade = m_OnChangeGradeEventArgs.objGrade;

        SetTarget(Managers.Object.FindByGuidObject<GameEntity>(cData.targetGuid));
    }

    #endregion
}
