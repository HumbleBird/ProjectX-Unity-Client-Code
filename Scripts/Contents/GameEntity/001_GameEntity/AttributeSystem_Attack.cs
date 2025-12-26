using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

public partial class AttributeSystem : MonoBehaviour
{
    public class OnAttackInfoEventArgs : EventArgs
    {
        public AttackPattern AttackPattern;
        public E_HitDecisionType EHitDeCisionType;
        public GameEntity Attacker;
        public int FinalDamage;
    }

    [Header("Attack Pattern")]
    // 원본 공격 패턴
    [SerializeField] private List<AttackPattern> m_originalAttackPatterns = new List<AttackPattern>();
    private List<AttackPattern> attackPatterns = new List<AttackPattern>();
    public IReadOnlyList<AttackPattern> m_AttackPatterns => attackPatterns;
    
    // 데이터 로드 후 원본 복사를 2번 하는 것을 방지하기 위해서
    bool m_isAttackPatternInstantiate;

    private void AttackPatternInitInstantiate()
    {
        if (m_isAttackPatternInstantiate)
            return;

        //Debug.Log("공격 패턴 원본 복사");

        if (m_originalAttackPatterns.Count > 0)
        {
            m_originalAttackPatterns = m_originalAttackPatterns
            .Select(pattern => {
                var instance = Instantiate(pattern);

                // 애니메이션을 스탭 애니메이션 변경
                Managers.Setting.ReplaceAnimationClipsInAttackPattern(m_Stat.Name, instance);
                return instance;
            })
            .ToList();

        }

        ReStoreAttackPattern();
        m_isAttackPatternInstantiate = true;
    }

    public void ReStoreAttackPattern()
    {
        attackPatterns = m_originalAttackPatterns;
    }


}