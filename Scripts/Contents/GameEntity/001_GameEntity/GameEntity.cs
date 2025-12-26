using Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using static Define;
using Material = UnityEngine.Material;
using Type = System.Type;

[RequireComponent(typeof(AttributeSystem))]
public class GameEntity : MonoBehaviour,
    ISaveable, 
    IGuidObject,
    ISelectable,
    IGameEntityView
{
    #region Field

    private Define.IGridQuery _grid;
    private Define.IPathfinder _path;
    private Define.IUnitGridManager _unitGrid;
    private Define.IUnitActionTickService _tick;
    private Define.ICoroutineRunner _coroutines;
    private Define.IGridVisualUpdateSource _gridVisualSource;


    // Event
    public event EventHandler OnSpawnObjectSelected; // 오브젝트를 배치하려고 할 때
    public event EventHandler OnObjectSpawned; // 씬에 생성되거나 활성화될 때
    public event EventHandler OnObjectDespawned; // 파괴되거나 비활성화될 때
    public event EventHandler OnSelectedEvent;
    public event EventHandler OnDeselectedEvent;
    public event EventHandler OnChangeBaseActionEvent;
    public event Action OnInteracted; // 상호작용 했을 때 (TODO IInteract 가 붙었을 때만)

    public string _guid { get; private set; } = string.Empty; // private field로 변경하고 프로퍼티로 접근
    public string guid => _guid;

    public void SetGUID(string inputGuid)
    {
        _guid = inputGuid;
    }

    // Ref
    [Header("Ref")]
    protected List<GameEntityAnimator> m_AnimatorManagers;
    public GameEntitySounder m_Sounder { get; protected set; }
    public AttributeSystem m_AttributeSystem { get; private set; }
    public Collider m_HitCollider { get; protected set; }
    public SetupAnimation m_SetupAnimation { get; private set; }
    public GameEntityCombat m_CombatManager { get; private set; }    

    protected HashSet<(Material mat, GameObject obj)> m_ModelMaterials = new();

    [Header("Info")]
    public GridPosition[] m_GridPositionOffsets;
    public GridPosition m_GridPosition { get; protected set; }
    public E_ObjectType m_EObjectType;
    public E_Dir m_CurrentEDir = E_Dir.South;
    public E_TeamId m_TeamId;

    [Header("Action")]
    [SerializeField, ReadOnly] protected BaseAction currentAction;
    public BaseAction m_CurrentAction
    {
        get => currentAction;
        protected set => currentAction = value;
    }
    protected BaseAction m_NextAction;
    protected BaseAction m_BeforeAction;

    protected Dictionary<Type, BaseAction> baseActionDict = new Dictionary<Type, BaseAction>();

    [Tooltip("현재 객체에 할당된 Action 큐")]
    protected Queue<(BaseAction action, GridPosition grid)> m_ActionQueue = new();
    
    // 지정 명령 예약
    // 1, 2 번의 Action이 들어왔을 때 1번 Action이 완전히 끝나면 2번 Action이 실행되게 만든다.
    protected bool m_IsCommandAction = false;

    [Header("Flag")]
    public bool m_IsDirectDesawnAtDeath = true; // 사망 후 바로 디스폰 처리 하는가?
    public bool m_IsSetuping { get; protected set; } = false; // 카드에서 뽑아 소환 중인가?

    [Tooltip("체크 되어 있을 경우 무조건 던전 코어를 향해 이동 (몬스터 전용)")]
    public bool m_IsTowardDungeonCore = false;

    // 전역 캐시 (Prefab 단위)
    private static Dictionary<string, bool> s_RotateSymmetryCache = new();
    public bool m_IsRotateSymmetry { get; private set; }

    #endregion

    #region 기본 함수

    protected virtual void Awake()
    {
        // 씬에 배치된 오브젝트인 경우, GUID가 없으면 새로 생성
        if (string.IsNullOrEmpty(_guid))
        {
            _guid = Guid.NewGuid().ToString(); // System.Guid를 사용하여 새 GUID 생성
        }

        m_AttributeSystem = GetComponent<AttributeSystem>();

        if(m_AnimatorManagers == null)
            m_AnimatorManagers = GetComponentsInChildren<GameEntityAnimator>().ToList();
        if(m_Sounder == null)
            m_Sounder = GetComponent<GameEntitySounder>();
        if (m_CombatManager == null)
            m_CombatManager = GetComponent<GameEntityCombat>();

        m_SetupAnimation = GetComponent<SetupAnimation>();

        foreach (var action in GetComponentsInChildren<BaseAction>())
            baseActionDict[action.GetType()] = action;

        m_AttributeSystem.OnDead += (s, e) => ClearAction();

        // 현재 Command Action에만 지정 명령 종료를 지정함.
        GetActions()
            .Where(action => action.GetComponent<ICommandAction>() != null)
            .ToList()
            .ForEach(a =>
            {
                a.OnActionCompleted += (s, e) =>
                {
                    m_IsCommandAction = false;
                    //Debug.Log($"{a.m_actionName}의 실행 종료");
                };
            });

    }

    protected virtual void Start()
    {
        CacheServices();

        CheckRotateSymmetry();

        if (m_IsSetuping)
            return;

        // 맵에 그냥 배치되어 있을 경우
        InitSpawn();
        SpawnComplete();
    }

    protected virtual void OnEnable()
    {
        // 이미 월드 내에 배치되어 있는 경우
        // 오브젝트를 배치하지 않은 상태에서 삭제한 경우를 대비하여
        if (baseActionDict.Count > 0)
            _tick.OnUpdateActionTick += ExecuteAction;

        m_IsSetuping = false;
    }

    protected virtual void OnDisable()
    {
        if (baseActionDict.Count > 0)
            _tick.OnUpdateActionTick -= ExecuteAction;

        ClearAction();
    }

    protected virtual void OnDestroy()
    {
    }

    protected virtual void Update()
    {

    }

    protected void OnValidate()
    {
        // 해당 조건은 몬스터 전용이다.
        if (m_TeamId != E_TeamId.Monster && m_IsTowardDungeonCore)
        {
            m_IsTowardDungeonCore = false;
            Debug.Log("해당 조건은 팀 타입이 몬스터일 때에만 허용됩니다.");
        }
    }

    #endregion

    private void CacheServices()
    {
        var ss = Managers.SceneServices;
        _grid = ss.Grid;
        _path = ss.Pathfinder;
        _unitGrid = ss.UnitGrid;
        _tick = ss.UnitActionTick;
        _coroutines = ss.CoroutineRunner;
        _gridVisualSource=ss.GridVisualUpdateSource;
    }

    public IEnumerable<(Material mat, GameObject obj)> GetModelsMaterial()
    {
        if (m_ModelMaterials == null || m_ModelMaterials.Count == 0)
        {
            // 렌더러 리스트를 먼저 구해서 즉시 평가
            var renderers = GetComponentsInChildren<Transform>()
                .Where(t => t != null)
                .SelectMany(t => t.GetComponentsInChildren<Renderer>(true))
                .ToArray(); // ✅ ToArray()로 즉시 평가 (지연 실행 방지)

            // 이제 renderer.materials로 개별 인스턴스 생성
            m_ModelMaterials = Enumerable.ToHashSet( renderers
                .SelectMany(r => r.materials   // ✅ 인스턴스화 발생
                    .Where(m => m != null)
                    .Select(m => (mat: m, obj: r.gameObject)))
                );
        }

        return m_ModelMaterials;
    }

    public List<Collider> GetChildColliders()
    {
        List<Collider> colliders = new List<Collider>();

        // root 포함 모든 자식 탐색
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (((1 << child.gameObject.layer) & GameConfig.Layer.HitColLayerMask) != 0)
            {
                Collider col = child.GetComponent<Collider>();
                if (col != null)
                {
                    colliders.Add(col);
                }
            }
        }

        return colliders;
    }

    public void UpdateGridPosition()
    {
        GridPosition newGridPosition = _grid.GetGridPosition(transform.position);

        if (newGridPosition != m_GridPosition)
        {
            // Unit changed Grid Position
            List<GridPosition> oldGridPositions = GetGridPositionListAtCurrentDir();
            m_GridPosition = newGridPosition;
            List<GridPosition> newGridPositions = GetGridPositionListAtCurrentDir();

            _unitGrid.MoveUnit(this, oldGridPositions, newGridPositions);
        }
    }

    public GridPosition GetGridPosition()
    {
        return m_GridPosition;
    }

    public Vector3 GetWorldPosition()
    {
        return transform.position;
    }

    public List<GameEntityAnimator> GetAnimationsManager()
    {
        return m_AnimatorManagers;
    }

    #region Select

    public void OnDeselected()
    {
        //Debug.Log($"{name} DeSelect");
        OnDeselectedEvent?.Invoke(this, EventArgs.Empty);
    }

    public void OnSelected()
    {
        //Debug.Log($"{name} Select");
        OnSelectedEvent?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Dir

    public E_Dir GetNextDir()
    {
        switch (m_CurrentEDir)
        {
            default:
            case E_Dir.South: return E_Dir.West;
            case E_Dir.West: return E_Dir.North;
            case E_Dir.North: return E_Dir.East;
            case E_Dir.East: return E_Dir.South;
        }
    }


    // GameEntity의 GridOffset과 원점인 GridPosition을 반환한다.
    public List<GridPosition> GetGridPositionListAtCurrentDir()
    {
        return GetGridPositionListAtSelectPosition(m_GridPosition);
    }

    // 지정된 위치인 pivot을 기준으로 GameEntity의 GridOffset을 반영한다.
    public List<GridPosition> GetGridPositionListAtSelectPosition(GridPosition pivot)
    {
        // 피벗 셀 포함해서 점유 셀 리스트 만들기
        var result = new List<GridPosition> { pivot };

        // m_GridPositionOffsets의 오프셋들을 pivot 기준으로 회전 적용
        result.AddRange(Util.ToGridPosition(this, pivot));

        return result;
    }

    // GetRotationAngle 함수: 각 방향에 따른 회전 각도를 반환합니다.
    public int GetRotationAngle()
    {
        switch (m_CurrentEDir)
        {
            default:
            case E_Dir.South: return 0;   // 기존 Dir.Down
            case E_Dir.West: return 90;  // 기존 Dir.Left
            case E_Dir.North: return 180; // 기존 Dir.Up
            case E_Dir.East: return 270; // 기존 Dir.Right
        }
    }

    #endregion

    #region Setup & Spawn

    // 몬스터의 경우 몬스터 스포너에서 소환
    // 플레이어의 경우 카드 선택 -> 드로우 소환
    public virtual void SpawnStart()
    {
        InitSpawn();

        m_IsSetuping = true;

        OnObjectSpawned?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void InitSpawn()
    {
        //Level grid 
        m_GridPosition = _grid.GetGridPosition(transform.position);
        //Managers.SceneServices.GridMut.SetCellType(GetGridPositionListAtCurrentDir(), E_GridCheckType.Walkable);
        //Managers.SceneServices.UnitGrid.AddUnitAtGridPositions(GetGridPositionListAtCurrentDir(), this);

        _gridVisualSource.DrawGridVisual();


        // 타격 콜라이더 켜기

        // Find Hit Collider
        if (m_HitCollider == null)
        {
            m_HitCollider = GetChildColliders().FirstOrDefault();
            m_HitCollider.enabled = true;
        }
    }

    // 조작 가능해짐
    public virtual void SpawnComplete()
    {
        Managers.Object.Add(gameObject);

        m_IsSetuping = false;
        m_AnimatorManagers.ToList().ForEach(animator => animator.AnimationPlay());

        // Base Action
        if (m_CurrentAction == null && baseActionDict.Count > 0)
        {
            var action = baseActionDict.First().Value;
            if (action != null)
                SwitchToNextStateAction(action);

        }

        _unitGrid.AddUnitAtGridPositions(GetGridPositionListAtCurrentDir(), this);
    }

    // 보통은 디스폰을 사망 애니메이션이 끝나면 바로 호출.
    public void DeSpawnStart()
    {
        OnObjectDespawned?.Invoke(this, EventArgs.Empty);
    }

    // 디스폰 후 호출되는 함수.
    public virtual void DeSpawnComplete()
    {
        _unitGrid.RemoveUnitAtGridPositions(GetGridPositionListAtCurrentDir(), this);

        Managers.Object.Remove(gameObject);

        Managers.Resource.Destroy(gameObject);

        Managers.Selection.Deselect(this);
    }

    // 플레이어가 카드에서 오브젝트를 드래그 해서 선택 중일 때
    public void SelectSpawnObject()
    {
        // 타격 콜라이더 끄기
        // Find Hit Collider
        if (m_HitCollider == null)
        {
            m_HitCollider = GetChildColliders().FirstOrDefault();
        }

        m_HitCollider.enabled = false;

        m_IsSetuping = true;

        // 고스트 메테리얼은 buildingGhost에서

        OnSpawnObjectSelected?.Invoke(this, EventArgs.Empty);

        if (m_AnimatorManagers == null)
            m_AnimatorManagers = GetComponentsInChildren<GameEntityAnimator>().ToList();
        if (m_Sounder == null)
            m_Sounder = GetComponent<GameEntitySounder>();

        m_AnimatorManagers.ToList().ForEach(a => a.AnimationStop());
    }

    public (int Min, int Max) GetGridPositionYOffset()
    {
        int min = 0;
        int max = 0;

        if(m_GridPositionOffsets.Length > 0)
        {
            min = m_GridPositionOffsets.Min(offset => offset.floor);
            max = m_GridPositionOffsets.Max(offset => offset.floor);
        }

        return (min, max);
    }

    private void CheckRotateSymmetry()
    {
        string prefabKey = gameObject.name; // Prefab 이름 기준 (필요하면 GUID 기반으로 교체)

        if (!s_RotateSymmetryCache.ContainsKey(prefabKey))
        {
            // 실제 체크 로직 실행
            var dirs = new[] { E_Dir.West, E_Dir.South, E_Dir.North, E_Dir.East };
            var results = dirs.Select(d => Util.ToGridPosition(this, d)).ToList();

            bool isSymmetry = results.All(r => r == results[0]);

            s_RotateSymmetryCache[prefabKey] = isSymmetry;
        }

        m_IsRotateSymmetry = s_RotateSymmetryCache[prefabKey];
    }

    #endregion

    #region Action

    /// <summary>
    /// 매 Tick마다 호출되는 유닛의 메인 Action 루프
    /// 1. CommandQueue에 쌓인 명령을 우선 처리
    /// 2. Command가 없으면 FSM(Action) Tick 수행
    /// </summary>
    protected void ExecuteAction(object sender, EventArgs args) 
    {
        // 사망 상태면 모든 Action 중단
        if (m_AttributeSystem.m_IsDead)
            return;

        // Command는 FSM보다 우선 처리 (Tick당 1개)
        // Command가 소비되면 FSM Tick은 실행하지 않음
        if (TryConsumeCommand())
            return;

        // 일반 FSM Action Tick
        TickCurrentAction();
    }

    /// <summary>
    /// 현재 Action을 즉시 교체한다.
    /// (Action 종료 여부와 무관하게 강제 전환)
    /// </summary>
    public void SwitchToNextStateAction(BaseAction nextAction)
    {
        m_CurrentAction = nextAction;

        // 디버그 / UI / 로그용 Action 변경 이벤트
        OnChangeBaseActionEvent?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 현재 Action과 예약된 ActionQueue를 전부 초기화한다.
    /// (유닛 리셋 / 강제 상태 변경 시 사용)
    /// </summary>
    private void ClearAction()
    {
        m_CurrentAction = null;

        ActionQueueClear();
    }

    /// <summary>
    /// Command / Action 예약 큐를 완전히 비운다.
    /// </summary>
    private void ActionQueueClear()
    {
        m_ActionQueue.Clear();
    }

    /// <summary>
    /// 유닛이 보유한 모든 Action 목록을 반환한다.
    /// (디버그 / 초기화 / 이벤트 바인딩 용도)
    /// </summary>
    public IEnumerable<BaseAction> GetActions()
    {
        return baseActionDict.Values;
    }

    /// <summary>
    /// 특정 타입의 Action을 가져온다.
    /// (없으면 null 반환)
    /// </summary>
    public T GetAction<T>() where T : BaseAction
    {
        if (baseActionDict.TryGetValue(typeof(T), out var action))
            return action as T;
        return null;
    }

    /// <summary>
    /// 이전 Action이 존재하면 되돌아가고,
    /// 없으면 IdleAction으로 복귀한다.
    /// </summary>
    public BaseAction GetBackStateAction()
    {
        if (m_BeforeAction == null)
        {
            return GetAction<IdleAction>();
        }
        else
        {
            return m_BeforeAction;
        }
    }

    /// <summary>
    /// 다음에 실행할 Action을 큐에 예약한다.
    /// (현재 Command 또는 Action이 끝난 후 실행됨)
    /// </summary>
    public void EnqueueNextAction(BaseAction action, GridPosition grid)
    {
        if (action == null)
            return;

        m_ActionQueue.Enqueue((action, grid));
    }

    /// <summary>
    /// 여러 개의 Action을 순차적으로 예약한다.
    /// (Shift+명령, 연속 행동 등에 사용)
    /// </summary>
    public void EnqueueNextAction(List<(BaseAction action, GridPosition grid)> list)
    {
        if (list.Count == 0)
            return;

        foreach (var (action, grid) in list)
            m_ActionQueue.Enqueue((action, grid));
    }

    /// <summary>
    /// ActionQueue에 쌓인 Command를 하나 소비한다.
    /// - Tick당 최대 1개만 처리
    /// - Command 수행 중에는 중복 소비 방지
    /// </summary>
    /// <returns>
    /// Command를 소비했으면 true,
    /// 소비하지 않았으면 false
    /// </returns>
    private bool TryConsumeCommand()
    {
        // 예약된 Action이 없으면 처리하지 않음
        if (m_ActionQueue.Count == 0)
            return false;

        // 현재 CommandAction이 아직 끝나지 않음
        if (m_IsCommandAction)
            return false;

        var (action, grid) = m_ActionQueue.Dequeue();
        m_IsCommandAction = true;

        // CommandAction으로 즉시 전환
        SwitchToNextStateAction(action);

        // CommandAction은 항상 목표 Grid를 들고 최초 1회 실행
        TrySwitchByResult(m_CurrentAction.TakeAction(grid));

        return true;
    }

    /// <summary>
    /// 현재 Action의 일반 FSM Tick 처리
    /// (Command가 없을 때만 실행됨)
    /// </summary>
    private void TickCurrentAction()
    {
        if (m_CurrentAction == null)
            return;

        TrySwitchByResult(m_CurrentAction.TakeAction());
    }

    /// <summary>
    /// Action의 실행 결과로 반환된 다음 Action이 유효할 경우
    /// 상태를 전환한다.
    /// </summary>
    private void TrySwitchByResult(BaseAction next)
    {
        // null 이거나 같은 Action이면 전환하지 않음
        if (next == null || next == m_CurrentAction)
            return;

        SwitchToNextStateAction(next);
    }

    #endregion

    #region Team Service

    public bool IsEnemy(GameEntity target)
    => target != null && TeamRules.IsEnemy(m_TeamId, target.m_TeamId);

    public bool IsAlly(GameEntity target)
        => target != null && TeamRules.IsAlly(m_TeamId, target.m_TeamId);

    public GameEntity m_Target { get; protected set; }

    public bool IsSetuping => m_IsSetuping;
    public bool IsDead => m_AttributeSystem != null && m_AttributeSystem.m_IsDead;

    public void SetTarget(GameEntity target)
    {
        m_Target = target;
    }

    #endregion

    #region Save & Load Data

    public virtual BaseData CaptureSaveData()
    {
        var state = GetAnimationsManager().FirstOrDefault();

        GameEntityAnimationData adata = null;
        
        if(state != null)
        {
            new GameEntityAnimationData()
            {
                stateNameHash = state.m_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash,
                normalizedTime = state.m_Animator.GetCurrentAnimatorStateInfo(0).normalizedTime,
                speed = GetAnimationsManager().FirstOrDefault().m_Animator.speed,
            };
        }

        AttackPatternData attackData = null;

        if(m_CurrentAction != null && m_CurrentAction is CombatAction combatAction)
        {
            if(combatAction.m_ThisTimeAttack != null)
            {
                attackData = combatAction.m_ThisTimeAttack.CaptureSaveData() as AttackPatternData;
            }
        }

        return new GameEntityData
        {
            prefabName = name,
            position = transform.position,
            rotation = transform.rotation,
            guid = _guid,
            attributeSystemData = m_AttributeSystem.CaptureSaveData(),

            // Action type
            CurrentActionType = GetEActionTypeByAction(m_CurrentAction),
            BeforeActionType = GetEActionTypeByAction(m_BeforeAction),
            NextActionType = GetEActionTypeByAction(m_NextAction),

            gameEntityAnimationData = adata,

            thisAttackPattern = attackData
        };

        E_ActionType GetEActionTypeByAction(BaseAction action)
        {
            if (action == null)
                return E_ActionType.None;

            if (action is IdleAction)
                return E_ActionType.Idle;
            else if (action is ChaseAction)
                return E_ActionType.Chase;
            else if (action is CombatAction)
                return E_ActionType.Combat;
            else if (action is PatrolAction)
                return E_ActionType.Patrol;
            else if (action is CommandAttackAction)
                return E_ActionType.CommandAttack;
            else if (action is CommandMoveAction)
                return E_ActionType.CommandMove;
            else 
                return E_ActionType.None;
        }
    }

    public virtual void RestoreSaveData(BaseData data)
    {
        GameEntityData gdata = data as GameEntityData;
        _guid = gdata.guid;
        transform.position = gdata.position;
        transform.rotation = gdata.rotation;
        //m_AttributeSystem.RestoreSaveData(gdata.attributeSystemData);
        m_CurrentAction = GetActionByEActionType(gdata.CurrentActionType);
        m_BeforeAction = GetActionByEActionType(gdata.BeforeActionType);
        m_NextAction = GetActionByEActionType(gdata.NextActionType);

        GameEntityAnimationData animData = gdata.gameEntityAnimationData;
        if (animData != null)
        {
            var animManager = GetAnimationsManager().FirstOrDefault();
            if (animManager != null)
            {
                var animator = animManager.GetComponent<Animator>();

                if (animator != null)
                {

                    // 2. 저장된 스테이트와 진행 정도(Normalized Time)부터 재생 시작
                    // Animator.Play(int stateNameHash, int layer, float normalizedTime) 사용
                    animator.Play(animData.stateNameHash, 0, animData.normalizedTime);

                    animator.speed = animData.speed;
                    // 로드 후 애니메이터가 멈춰 있을 수 있으므로 speed를 복구합니다.
                    animManager.AnimationPlay();

                }
            }
        }

        if(m_CurrentAction != null && m_CurrentAction is CombatAction combatAction)
        {
            combatAction.m_ThisTimeAttack = m_AttributeSystem.m_AttackPatterns.FirstOrDefault(attack => attack.ID == gdata.thisAttackPattern.id);
        }

        // TODO 구조 변경
        BaseAction GetActionByEActionType(E_ActionType action)
        {
            if (action == E_ActionType.None)
                return null;

            switch (action)
            {
                case E_ActionType.None:
                    return null;
                case E_ActionType.Idle:
                    return GetAction<IdleAction>();
                case E_ActionType.Chase:
                    return GetAction<ChaseAction>();
                case E_ActionType.Combat:
                    return GetAction<CombatAction>();
                case E_ActionType.Patrol:
                    return GetAction<PatrolAction>();
                case E_ActionType.CommandAttack:
                    return GetAction<CommandAttackAction>();
                case E_ActionType.CommandMove:
                    return GetAction<CommandMoveAction>();
                default:
                    return null;
            }
        }
    }

    #endregion

    public virtual void Interact(GameEntity interactor)
    {
        OnInteracted?.Invoke();
    }
}

public static class TeamRules
{
    static TeamRules()
    {
        if (Enemy.GetLength(0) != (int)E_TeamId.Count)
            Debug.LogError("TeamRules matrix size mismatch with E_TeamId.Count");
    }

    // [A, B] : A 기준으로 B가 적인가?
    private static readonly bool[,] Enemy =
    {
        //             Player   NPC     Monster  None
        /* Player */  { false,  false,  true,    false },
        /* NPC    */  { false,  false,  true,    false },
        /* Monster*/  { true,   true,   false,   false },
        /* None  */   { false,  false,  false,   false },
    };

    // [A, B] : A 기준으로 B가 아군인가?
    private static readonly bool[,] Ally =
    {
        //             Player   NPC     Monster  None
        /* Player */  { true,   true,   false,   false },
        /* NPC    */  { true,   true,   false,   false },
        /* Monster*/  { false,  false,  true,    false },
        /* None  */   { false,  false,  false,   false },
    };

    public static bool IsEnemy(E_TeamId self, E_TeamId other)
    {
        if ((uint)self >= (uint)E_TeamId.Count || (uint)other >= (uint)E_TeamId.Count)
            return false;
        return Enemy[(int)self, (int)other];
    }

    public static bool IsAlly(E_TeamId self, E_TeamId other)
    {
        if ((uint)self >= (uint)E_TeamId.Count || (uint)other >= (uint)E_TeamId.Count)
            return false;
        return Ally[(int)self, (int)other];
    }

}

