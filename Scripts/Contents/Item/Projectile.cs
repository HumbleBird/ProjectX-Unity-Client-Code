using Data;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Poolable), typeof(Rigidbody))]
public class Projectile : Item
{
    public AudioSource m_AudioSource { get; private set; }
    public Rigidbody m_Rigidbody { get; private set; }
    private Collider m_Collider;

    [Header("Info")]
    public float m_fStraightSpeed = 10f;
    public float ParabolaSpeed = 5f;
    public float m_DetectionHitRadius = 2f; // 유도형의 경우 필요

    [Header("Destroy")]
    private AttackPattern m_AttackPattern;
    public GameEntity m_Target { get; private set; }

    [Header("Fly")]
    [SerializeField] private AudioClip m_ProjectileFlyingAudioClip;

    [Header("Hit")]
    [SerializeField] private AudioClip m_ProjectileHitAudioClip;
    [SerializeField] private GameObject m_AfterProjectileHitPrefab;
    public bool m_IsHit { get; private set; } = false;

    public override void Awake()
    {
        base.Awake();

        m_AudioSource = GetComponent<AudioSource>();
        m_Rigidbody = GetComponent<Rigidbody>();
        m_Collider = GetComponent<Collider>();

        m_AudioSource.spatialBlend = 1f;
        m_AudioSource.maxDistance = 40f;
    }

    public override void OnEnable()
    {
        base.OnEnable();

        m_AudioSource.clip = null;
        m_AudioSource.playOnAwake = false;

        // Rigidbody 초기화 및  콜라이더 끄기
        m_Rigidbody.isKinematic = true;
        m_Rigidbody.velocity = Vector3.zero;
        m_Rigidbody.angularVelocity = Vector3.zero;
        m_Collider.enabled = false;

        foreach (Transform child in transform)
            child.gameObject.SetActive(true);
    }

    public override void OnDisable()
    {
        base.OnDisable();

        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(true);
        }
        m_Rigidbody.velocity = Vector3.zero;
        m_Rigidbody.angularVelocity = Vector3.zero;
        m_Rigidbody.Sleep(); // 완전히 물리 시뮬레이션 중단
        m_IsHit = false;
    }

    public void AttackReady(GameEntity owner, AttackPattern attack, GameEntity target)
    {
        foreach (Transform child in transform)
            child.gameObject.SetActive(true);

        // Audio
        if (m_ProjectileFlyingAudioClip != null)
            m_AudioSource.PlayOneShot(m_ProjectileFlyingAudioClip);

        // 콜라이더 켜기
        m_Collider.enabled = true;
        
        m_Owner = owner;
        m_AttackPattern = attack;
        m_Target = target;
    }

    private void HitEffect(Vector3 hitPos)
    {
        if (m_ProjectileHitAudioClip != null)
            m_AudioSource.PlayOneShot(m_ProjectileHitAudioClip);

        if (m_AfterProjectileHitPrefab != null)
        {
            GameObject go = Managers.Resource.Instantiate(m_AfterProjectileHitPrefab);
            go.transform.position = hitPos;
            go.transform.rotation = Quaternion.identity;
        }

        // Kinematic이 아닐 때만 velocity 설정
        if (m_Rigidbody.isKinematic == false)
        {
            m_Rigidbody.velocity = Vector3.zero;
            m_Rigidbody.angularVelocity = Vector3.zero;
        }

        m_IsHit = true;
    }

    private void OnCollisionEnter(Collision col)
    {
        int layerBit = 1 << col.gameObject.layer;

        // 적에게 부딪혔거나 지형 지물에 부딪혔을 경우에 한하여
        if ((layerBit & GameConfig.Layer.HitColLayerMask) != 0 ||
            (layerBit & GameConfig.Layer.m_StructLayer) != 0)
        {
            GameEntity target = col.gameObject.GetComponentInParent<GameEntity>();

            // 충돌 지점을 알 수 있습니다.
            Vector3 hitPoint = col.contacts[0].point;

            // 충돌 순간의 속도 방향을 계산합니다. (화살이 박힐 방향)
            // m_Rigidbody.velocity를 바로 사용하는 것이 가장 정확합니다.
            Vector3 impactDirection = m_Rigidbody.velocity.normalized;

            // 🎯 타겟 유닛 충돌 처리
            if (m_Owner.IsEnemy(target))
            {
                // 타격 처리
                target.m_AttributeSystem.Hit(m_AttackPattern, m_Owner);
                HitEffect(hitPoint);

                // -------------------- ★ 화살이 박히는 로직 추가/수정 ★ --------------------
                // 1. 화살의 위치를 충돌 지점으로 이동 (화살이 타겟을 뚫는 문제 방지)
                transform.position = hitPoint;

                // 2. 화살의 회전을 충돌 방향으로 맞춥니다.
                if(impactDirection  != Vector3.zero)
                    transform.rotation = Quaternion.LookRotation(impactDirection);

                // 3. 타겟에 자식으로 붙여서 (월드 위치 유지) 타겟이 움직일 때 같이 움직이게 함.
                transform.SetParent(col.transform, true);

                // 4. 물리 연산 중지 (필수)
                m_Rigidbody.isKinematic = true;

                //Debug.Log($"오브젝트 충돌!! {target.name}");
                Destroy();
            }
        }
    }

    public void Launch()
    {
        m_Rigidbody.isKinematic = false; // 더 이상 물리 영향 안 받게
    }

    #region Data Save & Load

    public override BaseData CaptureSaveData()
    {
        var iData = base.CaptureSaveData() as ItemData;

        ProjectileData pdata = new ProjectileData
        {
            prefabName = name,
            spawnPosition = spawnTransform.position,
            spawnRotation = spawnTransform.rotation,
            position = transform.position,
            rotation = transform.rotation,
            velocity = m_Rigidbody.velocity,
            angularVelocity = m_Rigidbody.angularVelocity,
            guid = _guid,
            targetGuid = m_Target != null ? m_Target._guid : string.Empty,
            onwerGuid = iData.onwerGuid,

        };

        return pdata;
    }

    public override void RestoreSaveData(BaseData baseData)
    {
        base.RestoreSaveData(baseData);

        ProjectileData data = baseData as ProjectileData;

        m_Rigidbody = GetComponent<Rigidbody>();    

        if (m_Rigidbody != null)
        {
            m_Rigidbody.velocity = data.velocity;
            m_Rigidbody.angularVelocity = data.angularVelocity;
        }

        if (!string.IsNullOrEmpty(data.targetGuid))
        {
            // 로드 후 Managers.Object에서 해당 guid를 가진 GameEntity를 찾아 연결
            m_Target = Managers.Object.FindByGuidObject<GameEntity>(data.targetGuid);
        }
    }

    #endregion
}
