using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Define
{
    public enum RewardRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    public enum E_AttackAnimationType
    {
        None,
        Parabola
    }

    /// <summary>
    /// 감지 당할 떄의 타입
    /// Player -> Player에 의해서만 탐지됨.
    /// </summary>
    public enum E_DetectedType
    {
        None,        // 탐지 되지 않음.
        Player,      // 플레이어에 의해 탐지됨
        GameEntity,  // 게임 오브젝트(유닛)에 의해 탐지됨
        All          // 모두에게 탐지됨
    }


    #region Attack

    public enum E_AttackStartPos
    {
        Attacker,
        Target,
        None,
    }

    public enum E_RangeFillType
    {
        // 지정 범위 내 모든 타일을 대상으로 함
        FullRange,

        // 가장 바깥쪽 칸만 대상으로 함
        OuterRing,

        // 중심 제외하고 안쪽 3x3 또는 n-1 범위만
        Inner,
    }

    public enum E_RangeShapeType
    {
        Square,     // 사각형 형태 (정사각형 범위)
        // 일정 간격마다만 적용 (예: 1칸 건너)
        Checker,

        // 대각선 축 방향만 대상으로 함
        Diamond,

        Arc,          // 호(부채꼴) 형태
        ReverseTriangle,      // 삼각형(시전자가 꼭지점 기준 ▼) 형태
        Triangle,      // 삼각형(시전자가 밑변의 가운데 기준 ▲) 형태
        CustomList,      // 임의 리스트(분석 불가)
        Plus,       // 플러스 형태 (상하좌우)
        Vertical,   // 세로 일자 (ㅣ)
        Horizontal  // 가로 일자 (ㅡ)
    }

    public enum E_Projectile
    {
        Guided, // 유도탄
        Straight, // 직격탄
    }

    public enum E_AttackType
    {
        None,           // 비공격형
        Physical,       // 물리 공격 (근접, 투사체 등)
        Magic,          // 마법 공격 (MP 소비, 마법 방어 계산)
        Dot,            // 지속 피해 (DoT)
        Buff,           // 강화/보조형
        Debuff,         // 약화형 (적에게 상태이상 or 디버프)
        Heal,           // 회복형 (아군 회복)
        Summon,         // 소환형 (새로운 유닛 생성)
        Knockback,      // 밀치기 등 위치 이동
    }


    public enum E_AttackCondition
    {
        Success,
        Fail_None,

        // 공격 쿨타임이 돌고 있음.
        Fail_CoolTime,

        // 공격 거리가 
        Fail_Distance,

        // 각 공격 타입에 따른 공격 실패
        Fail_IndividualCondition,

        // 마나가 없음
        Fail_ManaCost,

        // 콤보 공격에 실패
        Fail_Combo,

        // 공격 조건을 만족하는 그리드 타입이 없음.
        Fail_ConditionGridType,

        // 이동 가능한 공격 위치가 없음.
        Fail_HasNotMovableGridPosition,
    }

    public enum E_AttackEffectType
    {
        None,
        Damage,     // 직접 피해
        Dot,        // 지속 피해 (DoT)
        Buff,       // 강화
        Debuff,     // 약화
        Heal,       // 회복
        Knockback,  // 밀치기 등 위치 이동
    }

    public enum E_HitDecisionType
    {
        Hit, // 공격 적중
        CriticalHit, // 치명타 공격 적중
        AttackMiss, // 공격 미스
        Evasion, // 회피
        Counter, // 반격
    }

    public enum E_HealType
    {
        LifeSteal,      // 흡혈
        None,
    }

    public enum E_WeaponItemType
    {
        None,
        Sword,
        Bow
    }

    #endregion

    public enum E_ActionType
    {
        None,
        Idle,
        Chase,
        Combat,
        Patrol,
        CommandAttack,
        CommandMove
    }

    public enum E_SetupObjectOffsetChange
    {
        None,
        YOffset,
        XZOffset,
        All
            
    }

    public enum E_DamagedValueTextDisplayType
    {
        Up,
        MiddleBounce,

    }


    #region Object


    public enum E_ObjectEnhanceType
    {
        Health,
        Magic,
        Physical,
        Defense,
        Speed,
        Critical,
        Range,
        Skill
    }

    public enum E_ObjectGrade
    {
        Normal,
        Elite,
        Boss
    }

    public enum E_GameEntityClipType
    {
        // Animation State의 이름과 똑같이 해야 됨.

        // Spawn And DeSpawn
        Spawn,
        DeSpawn,
        Select,

        // Live
        Revive,
        Death,

        // Move
        Idle,
        Walk,
        Run,

        // Battle
        Attack,
        AttackMiss,
        AttackReadyFail,

        Damaged,
        PhaseChange,
        Evasion,

        // Interact
        Interact,
    }

    public enum E_Dir
    {
        North,
        NorthEast,
        NorthWest,
        East,
        South,
        SouthEast,
        SouthWest,
        West
    }

    public enum E_MoveType
    {
        Idle,
        Walk,
        Run,
    }

    public enum E_ObjectType
    {
        None = 0,
        Unit = 1,
        Building = 2,
        Interact = 3,
        AutoTrigger = 4,
        Obstacle,
        Skill,
        PassiveObject
    }



    #endregion

    #region Grid

    [System.Flags]
    public enum E_GridCheckType
    {
        // 비어 있음
        Walkable = 0,

        // 유닛이 있음
        GameEntity = 1 << 1,

        // 예약된 자리임
        Reserve = 1 << 2,

        // 장애물이 있음
        Obstacle = 1 << 3,

        // 비어 있음, 공간이 없음
        Void = 1 << 4,
    }

    public enum E_GridVisualType_Color
    {
        White,      // 이동 가능, 배치 가능
        Blue,       // 이동 예약
        Red,        // 주의 표시 (공격 혹은 배치 불가 자리)
        Yellow,     // 주의 표시
        Green
    }

    public enum E_GridVisualType_Intensity
    {
        Light,
        Medium,
        Strong
    }

    #endregion

    #region Sound

    public enum E_UISoundType
    {

    }

    public enum E_PlayerSoundType
    {

    }

    #endregion

    #region Base

    public enum E_TeamId
    {
        Player = 0,
        NPC = 1,
        Monster = 2,
        None,
        // ...
        Count // 마지막에 추가
    }


    public enum E_TargetTendencyType
    {
        All, // 모두
        Ally, // 같은 팀
        Enemy, // 적
        Neutral, // 중립?
    }

    public enum Scene
    {
        Unknown = 0,
        Start = 1,
        Dungeon = 2, // 미궁
        Test = 3,
        Loading,
        Camp, // 거점 구역
    }

    public enum Sound
    {
        Bgm = 0,
        Effect = 1,
        MaxCount,
    }

    public enum EColorMode
    {
        RGB,       // 일반 RGB (0~255 기반)
        RGB01,     // 정규화된 RGB (0~1)
        HSV        // HSV 기반
    }

    public enum UIEvent
    {
        Click,
        Pressed,
        PointerDown,
        PointerUp,
        
    }

    public enum CursorType
    {
        None,
        Arrow,
        Hand,
        Look,
    }
    #endregion
}
