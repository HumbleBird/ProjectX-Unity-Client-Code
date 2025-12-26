using UnityEngine;
using static Define;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System;
using UnityEditor.Experimental.GraphView;

public readonly struct LaunchContext
{
    public readonly float ColliderLength;
    public readonly float ObstacleHeight;

    public LaunchContext(float colliderLength, float obstacleHeight)
    {
        ColliderLength = colliderLength;
        ObstacleHeight = obstacleHeight;
    }
}

[CreateAssetMenu(menuName = "Attack Pattern/Range")]
public class AttackPattern_Range : AttackPattern
{
    [Header("Spawn Object")]
    public GameObject m_ProjectilePrefab;
    private List<Projectile> m_SpawnProjectiles = new();

    [Header("Launch Strategy")]
    [SerializeField] private E_Projectile _selectType;
    private IProjectileLauncher _launcher;
    public IProjectileLauncher Launcher
    {
        get
        {
            if (_launcher == null)
                _launcher = LauncherCreator.Create(this._selectType);
            return _launcher;
        }
    }
    public bool m_iIsLaunchProjectileUpParabola { get; private set; } // 직선 형으로 던질 것이냐, 위로 반원형을 그려서 던질 것이냐
    public LaunchContext context { get; private set; }


    [Header("Projectile Property")]
    [SerializeField] private int m_iSpawnProjectileCount = 1;
    [SerializeField] private bool m_IsImmediateLaunch = false;
    [SerializeField, Tooltip("무기에 붙어서 발사체를 생성할지 여부")]
    private bool m_SpawnFromWeapon = true;

    [Header("최적화용도")]
    List<(Item obj, Transform spawnTransform)> keepList = new();
    List<(Item obj, Transform spawnTransform)> removeList = new();

    public E_AttackAnimationType m_EAttackAnimationType { get; private set; }

    public override (E_AttackCondition condition, List<GridPosition> CanAttackablePos) 
        CanExecute(GameEntity attacker, GameEntity target)
    {
        var baseAttackPattern = base.CanExecute(attacker, target);

        if (baseAttackPattern.condition >= E_AttackCondition.Fail_None)
            return baseAttackPattern;

        float colliderLength = Managers.Game.GetObjectColliderLongLength(m_ProjectilePrefab.gameObject);
        float obstacleHeight = 
            Util.GetObstacleMaxHeight(
                Managers.SceneServices.Pathfinder,
                Managers.SceneServices.Grid,
                attacker.GetGridPosition(), target.GetGridPosition());

        context = new LaunchContext(colliderLength, obstacleHeight);

        // 장애물이 너무 높으면 실패
        if (colliderLength + obstacleHeight >= FLOOR_HEIGHT)
            return (E_AttackCondition.Fail_IndividualCondition, default);


        return baseAttackPattern;
    }

    public override void StartAttack(GameEntity attacker, GameEntity target, AttackPattern prevAttackpatern)
    {
        base.StartAttack(attacker, target, prevAttackpatern);

        var combatManager = attacker.m_CombatManager;
        if (combatManager == null)
        {
            Debug.Log($"{attacker}에서 controllableObjectCombatManager가 발견되지 않았습니다.");
            return;
        }

        if(m_ProjectilePrefab == null)
        {
            Debug.Log($"{attacker}에서 m_ProjectilePrefab가 발견되지 않았습니다.");
            return;
        }

        // 기존 오브젝트 정보 가져오기 (삭제 X)
        var existingList = attacker.m_CombatManager.m_AttackReadyItemObject;

        // 비교용 리스트 초기화
        keepList.Clear();
        removeList.Clear();
        m_SpawnProjectiles.Clear();

        foreach (var (obj, spawnT) in existingList)
        {
            if (obj == null) continue;

            // 프리팹 이름으로 비교 (Clone 제거 후 비교)
            string objName = obj.name.Replace("(Clone)", "").Trim();
            string prefabName = m_ProjectilePrefab.name.Replace("(Clone)", "").Trim();

            // 동일 프리팹이라면 유지
            if (objName == prefabName)
                keepList.Add((obj, spawnT));
            else
                removeList.Add((obj, spawnT));
        }

        // 제거 대상 오브젝트만 삭제 (spawnTransform은 보존)
        List<Transform> reusableTransforms = removeList
            .Where(x => x.spawnTransform != null)
            .Select(x => x.spawnTransform)
            .ToList();

        //  제거 대상 오브젝트만 삭제
        foreach (var (obj, _) in removeList)
        {
            obj?.Destroy();
        }

        // 남은 개수
        int remainingCount = keepList.Count;
        int desiredCount = m_iSpawnProjectileCount;

        // 필요한 만큼 새로 생성
        List<Transform> initSpawnTransforms 
            = attacker.m_CombatManager.GetProjectileSpawnTransforms(m_SpawnFromWeapon, desiredCount);


        // 새로 생성해야 할 개수만큼 생성
        //     → 제거된 위치(reusableTransforms)를 우선 재사용
        int reuseIndex = 0;

        for (int i = remainingCount; i < desiredCount; i++)
        {
            Transform spawnT = null;

            // 기존 제거된 위치부터 사용
            if (reuseIndex < reusableTransforms.Count)
            {
                spawnT = reusableTransforms[reuseIndex];
                reuseIndex++;
            }
            else
            {
                // 부족하면 새 위치를 사용
                spawnT = initSpawnTransforms[i % initSpawnTransforms.Count];
            }

            //Debug.Log("Range에서 새로운 준비 오브젝트를 생성");

            var newObj = Managers.Resource.Instantiate<Projectile>(m_ProjectilePrefab, spawnT);
            newObj.transform.localPosition = Vector3.zero;
            newObj.transform.localRotation = Quaternion.identity;
            m_SpawnProjectiles.Add(newObj);
        }

        // Projectile 생성 및 타겟 할당
        m_SpawnProjectiles.AddRange(
            attacker.m_CombatManager.m_AttackReadyItemObject
            .Where(x => x.obj is Projectile)
            .Select(x => x.obj as Projectile)
            .ToList());

        List<GameEntity> m_tempTargets = GetTargets(attacker, target);

        if(m_tempTargets != null && m_tempTargets.Count > 0)
        {
            for (int i = 0; i < m_SpawnProjectiles.Count; i++)
                m_SpawnProjectiles[i].AttackReady(attacker, this, m_tempTargets[i]);

            // 즉시 발사
            if (m_IsImmediateLaunch)
                Managers.SceneServices.CoroutineRunner.Run(LaunchProjectileCoroutine(attacker));
        }

        //  리스트에서 제거한 오브젝트 항목 삭제
        attacker.m_CombatManager.m_AttackReadyItemObject.Clear();

        // Animation
        if(context.ObstacleHeight >= 1)
        {
            m_EAttackAnimationType = E_AttackAnimationType.Parabola;
        }
        else
        {
            m_EAttackAnimationType = E_AttackAnimationType.None;

        }
    }

    public List<GameEntity> GetTargets(GameEntity attacker, GameEntity target)
    {
        var result = GetAttackGridPositions(attacker, target);

        List<GameEntity> targets = result.targetGridList.Select(p => Managers.SceneServices.Grid.GetCellEntity(p)).ToList();   

        if (result.targetGridList.Count() == 0)
            return default;

        // 실제로 사용할 타겟 리스트
        List<GameEntity> assignedTargets = new();

        // ① 적 1명인 경우 -> 모든 발사체가 같은 타겟
        if (targets.Count == 1)
        {
            for (int i = 0; i < m_iSpawnProjectileCount; i++)
                assignedTargets.Add(targets[0]);
        }
        // ② 적의 수와 발사체 수가 같은 경우 -> 1:1 대응
        else if (targets.Count == m_iSpawnProjectileCount)
        {
            assignedTargets.AddRange(targets);
        }
        // ③ 적이 발사체보다 많으면 -> 랜덤으로 뽑기
        else if (targets.Count > m_iSpawnProjectileCount)
        {
            // 중복 없는 랜덤 샘플링
            assignedTargets = targets.OrderBy(_ => UnityEngine.Random.value)
                                     .Take(m_iSpawnProjectileCount)
                                     .ToList();
        }
        // ④ 적이 더 적은 경우(예: 2명밖에 없는데 3발 쏴야 함)
        else if (targets.Count < m_iSpawnProjectileCount)
        {
            // 적들을 순환하면서 배분
            for (int i = 0; i < m_iSpawnProjectileCount; i++)
            {
                int index = i % targets.Count;
                assignedTargets.Add(targets[index]);
            }
        }

        return assignedTargets;
    }

    public override void Attack(GameEntity attacker, GameEntity target)
    {
        if (m_IsImmediateLaunch)
            return;

        if (m_SpawnProjectiles == null || m_SpawnProjectiles.Count <= 0)
            return;

        Managers.SceneServices.CoroutineRunner.Run(LaunchProjectileCoroutine(attacker));
    }

    // 애니메이션에서 event를 호출하기 때문에 분리 위치가 안 맞음. 반드시 한 프레임 늦춰야 됨
    private IEnumerator LaunchProjectileCoroutine(GameEntity attacker)
    {
        yield return new WaitForEndOfFrame();

        // 모든 발사체 준비
        for (int i = 0; i < m_SpawnProjectiles.Count; i++)
        {
            var projectile = m_SpawnProjectiles[i];
            projectile.transform.SetParent(null, true); // true는 월드 위치 유지
            projectile.Launch();
            Launcher.Launch(projectile, attacker, projectile.m_Target, context);
        }
    }


    protected override void SelectClip()
    {
        if(m_EAttackAnimationType == E_AttackAnimationType.Parabola)
        {
            if (m_AttackPatternInfoClips.Any(clip => clip.AttackAnimationClip.name.Contains("Parabola")))
            {
                selectInfoClip = m_AttackPatternInfoClips.Where(clip => clip.AttackAnimationClip.name.Contains("Parabola")).RandomPick();
                return;
            }
        }

        selectInfoClip = m_AttackPatternInfoClips.Where(clip => !clip.AttackAnimationClip.name.Contains("Parabola")).RandomPick();
    }
}
