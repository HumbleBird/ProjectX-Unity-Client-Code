using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[EditorShowInfo("IDungeonCore가 붙은 오브젝트가 피격을 당할때마다 화면을 빨갛게 만듬")]
public class HitVolume : MonoBehaviour
{
    private Volume m_Volume;
    private Vignette _Vignette;

    [SerializeField] private float m_fMaxIntensity = 0.45f;
    [SerializeField] private float m_fFadeDuration = 0.5f;

    private readonly List<AttributeSystem> _coreStats = new();
    private Coroutine _fadeCoroutine;

    private Coroutine _bindCoroutine;
    private Define.IDungeonCoreRegistry _boundRegistry; // 현재 구독중인 "진짜" 레지스트리

    private void OnEnable()
    {
        // NullService/순서 이슈 때문에: 즉시 접근 대신 바인딩 코루틴으로
        _bindCoroutine = StartCoroutine(BindWhenReady());
    }

    private void OnDisable()
    {
        // 바인딩 대기 중이면 중단
        if (_bindCoroutine != null)
        {
            StopCoroutine(_bindCoroutine);
            _bindCoroutine = null;
        }

        // 진짜 레지스트리에 OnReady 구독했었다면 해제
        if (_boundRegistry != null)
        {
            _boundRegistry.OnReady -= HandleRegistryReady;
            _boundRegistry = null;
        }

        UnsubscribeAll();
    }

    private IEnumerator BindWhenReady()
    {
        while (true)
        {
            var reg = Managers.SceneServices.DungeonCores;

            // 1) NullService면: 다음 프레임까지 기다림 (구독하지 않음)
            if (ReferenceEquals(reg, NullService<Define.IDungeonCoreRegistry>.Instance))
            {
                yield return null;
                continue;
            }

            // 2) 진짜 레지스트리면: 바인딩
            _boundRegistry = reg;

            // 이미 준비 완료면 바로 구독
            if (_boundRegistry.IsReady)
            {
                SubscribeAll();
                InitVolume();
                yield break;
            }

            // 아직 준비 전이면 Ready 이벤트 기다리기
            _boundRegistry.OnReady += HandleRegistryReady;
            yield break;
        }
    }

    private void HandleRegistryReady()
    {
        if (_boundRegistry == null)
            return;

        _boundRegistry.OnReady -= HandleRegistryReady;

        SubscribeAll();
        InitVolume();
    }

    private void InitVolume()
    {
        if (m_Volume != null) return;

        m_Volume = GetComponent<Volume>();
        m_Volume.profile.TryGet(out _Vignette);

        if (_Vignette == null)
            Debug.LogError("Error: HitVolume could not find Vignette in Volume Profile!");
        else
            _Vignette.intensity.value = 0f;
    }

    private void CacheCoreStatsAndSubscribe()
    {
        _coreStats.Clear();

        var cores = Managers.SceneServices.DungeonCores.Cores;

        for (int i = 0; i < cores.Count; i++)
        {
            if (cores[i] is Component c && c != null)
            {
                var stat = c.GetComponent<AttributeSystem>();
                if (stat != null)
                    _coreStats.Add(stat);
            }
        }
    }

    private void SubscribeAll()
    {
        CacheCoreStatsAndSubscribe();

        for (int i = 0; i < _coreStats.Count; i++)
            _coreStats[i].OnDamaged += OnDamaged;
    }

    private void UnsubscribeAll()
    {
        for (int i = 0; i < _coreStats.Count; i++)
            _coreStats[i].OnDamaged -= OnDamaged;
    }

    private void OnDamaged(object sender, EventArgs e)
    {
        var damagedStat = sender as AttributeSystem;

        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        _fadeCoroutine = StartCoroutine(HitEffectCoroutine(damagedStat));
    }

    private IEnumerator HitEffectCoroutine(AttributeSystem damagedStat)
    {
        if (_Vignette == null || m_Volume == null)
            yield break;

        float healthNormalized = 1f;
        if (damagedStat != null)
        {
            healthNormalized = damagedStat.GetHealthNormalized();
        }
        else
        {
            float min = 1f;
            for (int i = 0; i < _coreStats.Count; i++)
                min = Mathf.Min(min, _coreStats[i].GetHealthNormalized());
            healthNormalized = min;
        }

        _Vignette.intensity.value = m_fMaxIntensity;
        m_Volume.weight = 0.5f;

        float targetIntensity = Mathf.Lerp(m_fMaxIntensity, 0f, healthNormalized);

        float startIntensity = _Vignette.intensity.value;
        float time = 0f;

        while (time < m_fFadeDuration)
        {
            time += Time.deltaTime;
            float t = time / m_fFadeDuration;

            _Vignette.intensity.value = Mathf.Lerp(startIntensity, targetIntensity, t);
            m_Volume.weight = Mathf.Lerp(m_Volume.weight, 0f, t);

            yield return null;
        }

        _Vignette.intensity.value = targetIntensity;
        m_Volume.weight = 0f;
    }
}
