using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static Define;

// 발사체 발사 전 소환
// 무기에 버프를 둘러서 강화하기
[CreateAssetMenu(menuName = "Attack Pattern/Ready")]
[Serializable]
public class AttackPattern_Ready : AttackPattern
{
    [Header("Spawn Object")]
    public Item m_ReadyGameObjectPrefab;
    public GameObject m_FailPrefab;
    [SerializeField] private int m_iSpawnReadyCount = 1;
    [SerializeField, Tooltip("무기에 붙어서 생성할지 여부")]
    private bool m_SpawnFromWeapon = true;


    [Header("Ready")]
    public float m_AttackReadyTime = 2f;
    protected float lastAttackReadytime;
    public bool m_ISAttackReadyFinished => Time.time - lastAttackReadytime >= m_AttackReadyTime;

    [Header("Ready Object 최적화용도")]
    List<(Item obj, Transform spawnTransform)> keepList = new();
    List<(Item obj, Transform spawnTransform)> removeList = new();


    public override void Init()
    {
        base.Init();
        lastAttackReadytime = -m_AttackReadyTime; // 준비시간 완료된 상태로 시작
    }

    public override void StartAttack(GameEntity attacker, GameEntity target, AttackPattern prevAttackpatern)
    {
        base.StartAttack(attacker, target, prevAttackpatern);

        if (m_ReadyGameObjectPrefab != null)
        {
            // 1️ 기존 오브젝트 정보 가져오기 (삭제 X)
            var existingList = attacker.m_CombatManager.m_AttackReadyItemObject;

            // 2️ 비교용 리스트 초기화
            keepList.Clear();
            removeList.Clear();

            foreach (var (obj, spawnT) in existingList)
            {
                if (obj == null) continue;

                // 프리팹 이름으로 비교 (Clone 제거 후 비교)
                string objName = obj.name.Replace("(Clone)", "").Trim();
                string prefabName = m_ReadyGameObjectPrefab.name.Replace("(Clone)", "").Trim();

                // 동일 프리팹이라면 유지
                if (objName == prefabName)
                    keepList.Add((obj, spawnT));
                else
                    removeList.Add((obj, spawnT));
            }

            // 3️제거 대상 오브젝트만 삭제
            List<Transform> reusableTransforms = removeList
                .Where(x => x.spawnTransform != null)
                .Select(x => x.spawnTransform)
                .ToList();

            //  제거 대상 오브젝트만 삭제
            foreach (var (obj, _) in removeList)
                obj?.Destroy();

            //  리스트에서 제거한 오브젝트 항목 삭제
            attacker.m_CombatManager.
                    m_AttackReadyItemObject.RemoveAll(x => removeList.Any(r => r.obj == x.obj));

            // 남은 개수
            int remainingCount = keepList.Count;
            int desiredCount = m_iSpawnReadyCount;

            // 4️ 필요한 만큼 새로 생성
            List<Transform> initSpawnTransforms
                = attacker.m_CombatManager.GetProjectileSpawnTransforms(m_SpawnFromWeapon, desiredCount);

            // 6️⃣ 새로 생성해야 할 개수만큼 생성
            //     → removeList에서 제거된 위치를 먼저 재활용
            int reuseIndex = 0;


            for (int i = remainingCount; i < desiredCount; i++)
            {
                Transform spawnT = null;

                if (reuseIndex < reusableTransforms.Count)
                    spawnT = reusableTransforms[reuseIndex++];
                else
                    spawnT = initSpawnTransforms[i % initSpawnTransforms.Count];

                //Debug.Log("Ready에서 새로운 준비 오브젝트를 생성");

                var newObj = Managers.Resource.Instantiate<Item>(m_ReadyGameObjectPrefab.gameObject, spawnT);
                newObj.transform.localPosition = Vector3.zero;
                newObj.transform.localRotation = Quaternion.identity;

                // CombatManager에 등록
                attacker.m_CombatManager.m_AttackReadyItemObject.Add((newObj, spawnT));
            }


            // 5️ 위치 및 개수 동기화
            if (keepList.Count > 0)
            {
                // 기존 위치 유지
                foreach (var (obj, t) in keepList)
                    obj.transform.SetPositionAndRotation(t.position, t.rotation);
            }
        }

    }

    public override void EndAttack(GameEntity attacker, GameEntity target) // 종료
    {
        lastAttackReadytime = Time.time;
        attacker.m_CombatManager.m_ReadyAttackPattern.Add(this);
    }

    public override void StartAttackFail(GameEntity attacker, GameEntity target)
    {
        base.StartAttackFail(attacker, target);

        if(m_FailPrefab !=null)
        {
            var go = Managers.Resource.Instantiate(m_FailPrefab);
            attacker.StartCoroutine(ObjectDestroy(go, 3f));
        }
    }
}
