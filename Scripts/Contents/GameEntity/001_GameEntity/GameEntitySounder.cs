using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using UnityEngine;
using static Define;
using static Unity.Cinemachine.CinemachineInputAxisController;
using Random = UnityEngine.Random;

[Serializable]
public  class GameEntitySounder : MonoBehaviour
{
    [SerializeField] private Transform m_AudiosTransform;
    public Dictionary<string, AudioSource> m_DicAudioSources = new Dictionary<string, AudioSource> ();

    [Header("Ref")]
    protected AttributeSystem m_StatSystem;
    private GameEntity m_GameEntity;
    private GameEntityCombat m_GameEntityCombat;
    private GameEntityAnimator[] m_GameEntityAnimator;

    [Header("Spawn And DeSpawn")]
    public AudioClip[] SpawnClipList; // 소환
    public AudioClip[] DeSpawnClipList; // 소멸
    public AudioClip[] SpawnObjectSelectedClipList; // 스폰 오브젝트 선택

    [Header("Live")]
    public AudioClip[] ReviveClipList; // 사망
    public AudioClip[] DestroyClipList; // 사망

    [Header("Battle")]
    public AudioClip[] DamagedClipList; // 피격
    public AudioClip[] CriticalDamagedClipList; // 피격
    public AudioClip[] PhaseChangeClipList;
    public AudioClip[] DodgeClipList;

    [Header("Move")]
    public AudioClip[] WalkClipList;
    public AudioClip[] RunClipList;

    [Header("Interact")]
    public AudioClip[] InteractClipList;


    protected virtual void Awake()
    {
        Managers.Sound.InitAudioSourceWith3dObject<E_GameEntityClipType>(m_AudiosTransform, ref m_DicAudioSources);

        // Event Set 
        m_GameEntity = GetComponent<GameEntity>();
        m_GameEntityCombat = GetComponent<GameEntityCombat>();
        m_StatSystem = GetComponentInParent<AttributeSystem>();
        m_GameEntityAnimator = GetComponentsInChildren<GameEntityAnimator>();
    }

    private void OnEnable()
    {
        m_GameEntity.OnObjectSpawned += Spawnd;
        m_GameEntity.OnSpawnObjectSelected += SpawnObjectSelected;
        m_GameEntity.OnObjectDespawned += OnObjectDespawned;
        m_GameEntity.OnInteracted += Interact;

        m_StatSystem.OnRevived += Revived;
        m_StatSystem.OnDead += Dead;

        m_StatSystem.OnDamaged += DamagedSoundPlay;

        if (m_GameEntity.TryGetComponent<CombatAction>(out CombatAction combat))
        {
            combat.OnPhaseChange += PhaseChange;
        }

        m_GameEntityCombat.OnAttackReadyFailed += AttackReadyFail;

        foreach (var anim in m_GameEntityAnimator)
            anim.OnStep += StepSoundPlay;
    }

    private void OnDisable()
    {
        m_GameEntity.OnObjectSpawned -= Spawnd;
        m_GameEntity.OnSpawnObjectSelected -= SpawnObjectSelected;
        m_GameEntity.OnObjectDespawned -= OnObjectDespawned;
        m_GameEntity.OnInteracted -= Interact;

        m_StatSystem.OnRevived -= Revived;
        m_StatSystem.OnDead -= Dead;

        m_StatSystem.OnDamaged -= DamagedSoundPlay;

        if (m_GameEntity.TryGetComponent<CombatAction>(out CombatAction combat))
        {
            combat.OnPhaseChange -= PhaseChange;
        }

        m_GameEntityCombat.OnAttackReadyFailed -= AttackReadyFail;


        foreach (var anim in m_GameEntityAnimator)
            anim.OnStep -= StepSoundPlay;
    }

    private void Spawnd(object sender, EventArgs e)
    {
        SoundPlay(SpawnClipList, E_GameEntityClipType.Spawn.ToString());
    }

    private void OnObjectDespawned(object sender, EventArgs e)
    {
        SoundPlay(DeSpawnClipList, E_GameEntityClipType.DeSpawn.ToString());
    }

    private void SpawnObjectSelected(object sender, EventArgs e)
    {
        SoundPlay(SpawnObjectSelectedClipList, E_GameEntityClipType.Select.ToString());
    }

    private void Revived(object sender, EventArgs e)
    {
        SoundPlay(ReviveClipList, E_GameEntityClipType.Revive.ToString());
    }

    private void Dead(object sender, EventArgs e)
    {
        SoundPlay(DestroyClipList, E_GameEntityClipType.Death.ToString());
    }

    private void Damaged(object sender, EventArgs e)
    {
        SoundPlay(SpawnClipList, E_GameEntityClipType.Spawn.ToString());
    }

    private void PhaseChange(object sender, EventArgs e)
    {
        SoundPlay(PhaseChangeClipList, E_GameEntityClipType.PhaseChange.ToString());
    }

    private void Interact()
    {
        SoundPlay(InteractClipList, E_GameEntityClipType.Interact.ToString());
    }

    // 이동 상태에 따른 발자국 사운드 플레이
    public virtual void StepSoundPlay()
    {
        switch (m_GameEntity.m_AttributeSystem.m_EMoveType)
        {
            case E_MoveType.Walk:
                SoundPlay(WalkClipList, E_GameEntityClipType.Walk.ToString());
                break;
            case E_MoveType.Run:
                SoundPlay(RunClipList, E_GameEntityClipType.Run.ToString());
                break;
            default:
                break;
        }
    }

    public virtual void DamagedSoundPlay(object sender, AttributeSystem.OnAttackInfoEventArgs e) 
    {
        if (e.AttackPattern != null)
            return;

        if (m_GameEntity.m_AttributeSystem.m_IsDead)
            return;

        switch (e.EHitDeCisionType)
        {
            case E_HitDecisionType.Hit:
                SoundPlay(DamagedClipList, E_GameEntityClipType.Damaged.ToString());
                break;
            case E_HitDecisionType.CriticalHit:
                SoundPlay(CriticalDamagedClipList, E_GameEntityClipType.Damaged.ToString());
                break;
            case E_HitDecisionType.Evasion:
                SoundPlay(DodgeClipList, E_GameEntityClipType.Evasion.ToString());
                break;
            case E_HitDecisionType.Counter:
                break;
            case E_HitDecisionType.AttackMiss: // 공격자 입장에서 쓰고 싶은데, 무기 종류에 따라
                break;
        }
    }

    public void AttackSoundPlay(AttackPattern attack)
    {
        SoundPlay(attack.selectInfoClip.AttackSuccessAudioClip, E_GameEntityClipType.Attack.ToString());
    }

    public void AttackMissSoundPlay(object sender, AttackPattern attack)
    {
        SoundPlay(attack.selectInfoClip.AttackMissAudioClip, E_GameEntityClipType.Attack.ToString());
    }

    public void AttackReadyFail(AttackPattern_Ready readyPattern)
    {
        SoundPlay(readyPattern.selectInfoClip.AttackFailAudioClip, E_GameEntityClipType.Attack.ToString());
    }

    public void SoundPlay(AudioClip audioClip, string audioClipName, int loop = 0, float pitch = 1.0f)
    {
        // Check Audio Source
        if (!m_DicAudioSources.TryGetValue(audioClipName, out var source))
        {
            Debug.LogWarning($"{audioClipName} Audio Source 가 없습니다.");
            return;
        }

        if (audioClip == null)
        {
            Debug.Log($"{m_GameEntity.name} 의 {audioClipName}의 Audio Clip이 없습니다.");
            return;
        }

        source.pitch = pitch;

        if (loop == 0)
            source.PlayOneShot(audioClip);
        else
        {
            source.clip = audioClip;
            source.Play();
        }
    }

    public void SoundPlay(AudioClip[] audioClip, string audioClipName, int loop = 0, float pitch = 1.0f)
    {
        // Check Clip
        if (audioClip.Length == 0)
        {
            //Debug.Log($"{m_GameEntity.name} 의 {audioClipName} Audio Clip이 없습니다.");
            return;
        }

        SoundPlay(audioClip.RandomPick(), audioClipName, loop, pitch);
    }
}
