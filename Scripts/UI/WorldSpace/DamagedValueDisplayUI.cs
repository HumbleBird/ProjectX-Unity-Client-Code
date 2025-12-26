using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using static Define;
using Random = UnityEngine.Random;

public class DamagedValueDisplayUI : MonoBehaviour
{
    public E_DamagedValueTextDisplayType m_EDamagedValueTextDisplayType;

    GameEntity m_GameEntity;
    AttributeSystem StatSystem;

    [SerializeField] TextMeshProUGUI m_DamageValuePrefab;

    [Header("Up")]
    [SerializeField] float m_fUpHeight = 0.3f;

    [Header("Bounds")]
    [SerializeField] float duration1 = 0.3f;
    [SerializeField] float duration2 = 0.25f;
    [SerializeField] float duration3 = 0.2f;

    [SerializeField] float height1 = 1.0f;
    [SerializeField] float height2 = 0.5f;

    [Header("Heal Animation Settings")]
    [SerializeField] float m_fHealUpHeight = 0.5f;
    [SerializeField] float m_fHealDuration = 1.0f;
    private float m_fHealMaxScale = 0.4f;

    // Start is called before the first frame update
    void Awake()
    {
        m_GameEntity = GetComponentInParent<GameEntity>();
        StatSystem = GetComponentInParent<AttributeSystem>();

        StatSystem.OnDamaged += DisplayDamagedValueText;
        StatSystem.OnHealed += DisplayHealValueText;

        int rand = Random.Range(0, 2);
        if (rand % 2 == 0)
            m_EDamagedValueTextDisplayType = E_DamagedValueTextDisplayType.Up;
        else
            m_EDamagedValueTextDisplayType = E_DamagedValueTextDisplayType.MiddleBounce;
    }

    private void OnDestroy()
    {
        if (StatSystem != null)
        {
            StatSystem.OnDamaged -= DisplayDamagedValueText;
            StatSystem.OnHealed -= DisplayHealValueText;
        }
    }

    private void OnEnable()
    {
        foreach (Transform child in transform)
        {
            Managers.Resource.Destroy(child.gameObject);
        }
    }

    private void DisplayDamagedValueText(object sender, AttributeSystem.OnAttackInfoEventArgs e)
    {
        string text = "";
        Color textColor = ColorUtil.GetNormalDamage();

        switch (e.EHitDeCisionType)
        {
            case E_HitDecisionType.Hit:
                text = e.FinalDamage.ToString();
                textColor = ColorUtil.GetNormalDamage();
                break;
            case E_HitDecisionType.CriticalHit:
                text = e.FinalDamage.ToString();
                textColor = ColorUtil.GetCriticalHit();
                break;
            case E_HitDecisionType.AttackMiss:
                text = "Miss";
                textColor = ColorUtil.GetMissOrEvasion();
                break;
            case E_HitDecisionType.Evasion:
                text = "Evasion";
                textColor = ColorUtil.GetMissOrEvasion();
                break;
            case E_HitDecisionType.Counter:
                text = "Counter";
                textColor = ColorUtil.GetMissOrEvasion();
                break;
        }


        var prefab = Managers.Resource.Instantiate<TextMeshProUGUI>(m_DamageValuePrefab.gameObject, transform);
        prefab.text = text;
        prefab.color = textColor;

        var col = m_GameEntity.m_HitCollider;

        if (m_EDamagedValueTextDisplayType == E_DamagedValueTextDisplayType.Up)
        {
            float minX = col.bounds.min.x;
            float maxX = col.bounds.max.x;
            float maxY = col.bounds.max.y;
            float minZ = col.bounds.max.z;
            float maxZ = col.bounds.max.z;
            float centerY = col.bounds.center.y;

            Vector3 start = new Vector3(Random.Range(minX, maxX), maxY, Random.Range(minZ, maxZ));
            prefab.transform.position = start;

            StartCoroutine(PlayUp(start, prefab.gameObject));
        }
        else if (m_EDamagedValueTextDisplayType == E_DamagedValueTextDisplayType.MiddleBounce)
        {

            Vector3 start = col.bounds.center;

            Vector3 attackDir = -(start - e.Attacker.transform.position).normalized;

            // 튀어나가는 방향 (앞 + 위 살짝)
            Vector3 initialOffset = attackDir * 0.5f + Vector3.up * 0.5f;

            Vector3 bounceStart = start + initialOffset;
            prefab.transform.position = bounceStart;

            StartCoroutine(PlayBounceSequence(bounceStart, prefab.gameObject));
        }
    }

    private IEnumerator PlayUp(Vector3 start, GameObject prefab)
    {

        DG.Tweening.Sequence seq = DOTween.Sequence();
        seq.Append(prefab.transform.DOMoveY(start.y + m_fUpHeight, duration1));
        yield return seq.WaitForCompletion();
        Managers.Resource.Destroy(prefab);
    }

    private IEnumerator PlayBounceSequence(Vector3 start, GameObject prefab)
    {
        float groundY = m_GameEntity.m_HitCollider.bounds.min.y + 0.1f; // 거의 바닥

        DG.Tweening.Sequence seq = DOTween.Sequence();

        seq.Append(prefab.transform.DOMoveY(groundY, duration1).SetEase(Ease.InQuad));           // 1차 낙하
        seq.Append(prefab.transform.DOMoveY(groundY + height1, duration1).SetEase(Ease.OutQuad)); // 1차 반등
        seq.Append(prefab.transform.DOMoveY(groundY, duration2).SetEase(Ease.InQuad));            // 2차 낙하
        seq.Append(prefab.transform.DOMoveY(groundY + height2, duration2).SetEase(Ease.OutQuad)); // 2차 반등
        seq.Append(prefab.transform.DOMoveY(groundY, duration3).SetEase(Ease.InQuad));            // 최종 낙하

        yield return seq.WaitForCompletion();

        Managers.Resource.Destroy(prefab);
    }

    private void DisplayHealValueText(object sender, AttributeSystem.OnHealEventArgs e)
    {
        // 회복량이 0이면 표시하지 않음
        if (e.HealAmount <= 0)
            return;

        string text = $"+{e.HealAmount}";

        var prefab = Managers.Resource.Instantiate<TextMeshProUGUI>(m_DamageValuePrefab.gameObject, transform);
        prefab.text = text;

        prefab.color = ColorUtil.GetHeal();
        var col = m_GameEntity.m_HitCollider;

        float minX = col.bounds.min.x;
        float maxX = col.bounds.max.x;
        float maxY = col.bounds.max.y;
        float minZ = col.bounds.min.z;
        float maxZ = col.bounds.max.z;

        Vector3 start = new Vector3(Random.Range(minX, maxX), maxY, Random.Range(minZ, maxZ));
        prefab.transform.position = start;

        StartCoroutine(PlayHealUp(start, prefab.gameObject));
    }

    private IEnumerator PlayHealUp(Vector3 start, GameObject prefab)
    {
        var textMesh = prefab.GetComponent<TextMeshProUGUI>();

        Sequence seq = DOTween.Sequence();

        seq.Append(prefab.transform.DOMoveY(start.y + m_fHealUpHeight, m_fHealDuration));
        seq.Join(prefab.transform.DOScale(m_fHealMaxScale, m_fHealDuration * 0.5f).SetEase(Ease.OutQuad));
        seq.Append(prefab.transform.DOScale(m_fHealMaxScale * 1.1f, m_fHealDuration * 0.5f).SetEase(Ease.InQuad));
        seq.Join(textMesh.DOFade(0f, m_fHealDuration));
        yield return seq.WaitForCompletion();

        Managers.Resource.Destroy(prefab);
    }

}
