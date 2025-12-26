using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static Define;

public class CombatAction : BaseAction
{
    CombatAction()
    {
        m_actionName = "Combat";
    }

    public event EventHandler<OnAttackBaseEventArgs> OnStartAttack;
    public event EventHandler OnEndAttack;
    //public event EventHandler OnAttackCancel;
    public event EventHandler OnPhaseChange;

    public class OnAttackBaseEventArgs : EventArgs
    {
        public AttackPattern attackPattern;
    }

    Func<bool> conditionPase;
    bool isChaningPase;
    float m_fRotateTimer = 0;
    [SerializeField] float m_rotateTick = 0.1f;
    [SerializeField] float rotateSpeed = 70;

    BaseAction m_TODOChangeAction;

    public AttackPattern m_ThisTimeAttack;
    public AttackPattern m_PrevAttackPattern;

    private void OnEnable()
    {
        OnStartAttack += DrawGridVisual;
        OnEndAttack += DrawGridVisual;
    }

    private void OnDisable()
    {
        OnStartAttack -= DrawGridVisual;
        OnEndAttack -= DrawGridVisual;
    }

    protected override void Update()
    {
        base.Update();

        #region Check List

        if (m_GameEntity.m_CurrentAction != this)
            return;

        if (RotateTowardTarget() == false)
            return;

        // 애니메이션이 진행중이면 대기
        if (m_bIsActive)
            return;

        if (isChaningPase)
            return;

        #endregion

        // 1. 유닛이 특수 상태일 경우 페이즈 전환 (예: 2페이즈 보스)
        HandlePhaseTransition();

        // TODO
        // 페이즈 대기

        // 현재 공격 후보들 추리기
        // cooltime, mana의 경우 대기 할지 말지 용도로 뽑기
        var usablePatterns = Managers.Game.EvaluateAttackPatternsByCondition
                            (m_GameEntity,
                             m_GameEntity.m_Target,
                             E_AttackCondition.Success,
                             E_AttackCondition.Fail_CoolTime,
                             E_AttackCondition.Fail_Distance,
                             E_AttackCondition.Fail_ManaCost);


        if (!usablePatterns.Any())
        {
            //Debug.Log($"{m_GameEntity}가 현재 공격할 수 있는 공격 패턴이 없습니다. 추적 모드로 돌아갑니다.");
            m_TODOChangeAction = m_GameEntity.GetAction<ChaseAction>();

            return;
        }

        // 조건별 그룹화
        var grouped = usablePatterns
        .GroupBy(r => r.condition)
        .ToDictionary(g => g.Key, g => g.ToList());

        // 1. Success가 하나라도 있는가?
        if (grouped.TryGetValue(E_AttackCondition.Success, out var successList))
        {
            var toAttack = usablePatterns.RandomPick();
            
            //Debug.Log($"{pobj}가 현재 선택한 공격 {toAttack.pattern}");
            ChangeAttack(toAttack.pattern);

            // Event (Animation, Sound) 실행
            OnStartAttackEventInvoke();
        }

        // 2. Fail_Distance가 있는가?
        else if (grouped.TryGetValue(E_AttackCondition.Fail_Distance, out var distList))
        {
            if(m_GameEntity.m_AttributeSystem.m_CanMoveableGameEntity)
                m_TODOChangeAction = m_GameEntity.GetAction<ChaseAction>();
        }

        // 3. Fail_CoolTime / Fail_ManaCost
        else if (grouped.TryGetValue(E_AttackCondition.Fail_CoolTime, out var coolList)
                || grouped.TryGetValue(E_AttackCondition.Fail_ManaCost, out var manaList))
        {
            // 대기
        }
    }

    private bool RotateTowardTarget()
    {
        if (m_GameEntity.m_Target == null)
            return true;

        // 타겟 방향 계산
        Vector3 moveDirection = (m_GameEntity.m_Target.transform.position - m_GameEntity.transform.position).normalized;

        // 회전 완료 여부 판단
        float angleThreshold = 5f; // 허용 오차 각도 (예: 5도)
        float angle = Vector3.Angle(m_GameEntity.transform.forward, moveDirection);

        if (angle < angleThreshold)
        {
            return true;
        }
        else
        {
            m_fRotateTimer -= Time.deltaTime;
            if(m_fRotateTimer <= 0)
            {
                m_fRotateTimer = m_rotateTick;

                // 회전
                m_GameEntity.transform.forward = Vector3.Slerp(
                    m_GameEntity.transform.forward,
                    moveDirection,
                    Time.deltaTime * rotateSpeed
                );
            }

            return false;
        }
    }

    public override BaseAction TakeAction(GridPosition gridPosition = default)
    {
        if (m_bIsActive)
            return this;

        if (m_TODOChangeAction != null)
        {
            BaseAction ac = m_TODOChangeAction;
            m_TODOChangeAction = null;
            return ac;
        }
        else
            return this;
    }

    public override EnemyAIAction GetEnemyAIAction(GridPosition gridPosition)
    {
        throw new NotImplementedException();
    }

    public override List<GridPosition> GetValidActionGridPositionList()
    {
        throw new NotImplementedException();
    }

    public void HandlePhaseTransition()
    {
        if(conditionPase != null && conditionPase.Invoke())
        {
            OnPhaseChange?.Invoke(this, EventArgs.Empty);
        }
    }

    public void OnStartAttackEventInvoke()
    {
        m_ThisTimeAttack.StartAttack(m_GameEntity, m_GameEntity.m_Target, m_PrevAttackPattern);

        OnStartAttack?.Invoke(this, new OnAttackBaseEventArgs()
        {
            attackPattern = m_ThisTimeAttack
        });

        m_bIsActive = true;
    }

    public void OnEndAttackEventInvoke()
    {
        m_ThisTimeAttack?.EndAttack(m_GameEntity, m_GameEntity.m_Target);

        OnEndAttack?.Invoke(this, EventArgs.Empty);

        m_bIsActive = false;

        m_TODOChangeAction = null;

        // 만약 다음 다음 어택이 있다면 교체
        if (m_ThisTimeAttack?.m_iNextAttackPattern.Length == 0)
        {
            m_ThisTimeAttack = null;
        }
    }

    public override void ClearAction()
    {
        base.ClearAction();

        m_bIsActive = false;
    }

    public void ActiveSet(bool isFalse)
    {
        m_bIsActive = isFalse;
    }

    public void ChangeAttack(AttackPattern todoAttack)
    {
        m_PrevAttackPattern = m_ThisTimeAttack;
        m_ThisTimeAttack = todoAttack;
    }


}
