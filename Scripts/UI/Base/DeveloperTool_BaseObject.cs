using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Define;

public class DeveloperTool_BaseObject : MonoBehaviour
{
    [Header("Damage Info")]
    public TMP_InputField m_iPhysicalAttackDamage;
    public TMP_InputField m_iMagicAttackDamage;
    public TMP_InputField m_iPhysicalFixedDamage;
    public TMP_InputField m_iMagicFixedDamage;
    public TMP_InputField m_iPhysicalArmorPenetraion;
    public TMP_InputField m_iMagicalArmorPenetraion;

    [Header("Battle Attack Chance")]
    public TMP_InputField m_impCriticalChance;
    public TMP_InputField m_impCriticalDamageUp;
    public TMP_InputField m_impKnockbackChance;

    AttackPattern attack;

    private void Awake()
    {
        InitInputField(m_iPhysicalAttackDamage);
        InitInputField(m_iMagicAttackDamage);
        InitInputField(m_iPhysicalFixedDamage);
        InitInputField(m_iMagicFixedDamage);
        InitInputField(m_iPhysicalArmorPenetraion);
        InitInputField(m_iMagicalArmorPenetraion);
        InitInputField(m_impCriticalChance);
        InitInputField(m_impCriticalDamageUp);
        InitInputField(m_impKnockbackChance);

        // 실제 인스턴스 생성은 상속 클래스에서 해야 함
        attack = ScriptableObject.CreateInstance<AttackPattern>(); // ✅
    }

    private void InitInputField(TMP_InputField field)
    {
        if (field != null && string.IsNullOrWhiteSpace(field.text))
            field.text = "0";
    }

    public void DamageObject(List<GameEntity> units)
    {
        E_HitDecisionType type = E_HitDecisionType.Hit;

        attack.m_iPhysicalAttackDamage = TryParseInt(m_iPhysicalAttackDamage.text);
        attack.m_iMagicAttackDamage = TryParseInt(m_iMagicAttackDamage.text);
        attack.m_iPhysicalFixedDamage = TryParseInt(m_iPhysicalFixedDamage.text);
        attack.m_iMagicFixedDamage = TryParseInt(m_iMagicFixedDamage.text);
        attack.m_fPhysicalArmorPenetraion = TryParseInt(m_iPhysicalArmorPenetraion.text);
        attack.m_fMagicalArmorPenetraion = TryParseInt(m_iMagicalArmorPenetraion.text);

        attack.m_fAccuracy = 100;
        attack.m_iCriticalChance = TryParseInt(m_impCriticalChance.text);
        attack.m_fCriticalDamageUp = TryParseFloat(m_impCriticalDamageUp.text);
        attack.m_iKnockbackChance = TryParseInt(m_impKnockbackChance.text);

        int rand = Random.Range(1, 101);
        if (rand < attack.m_iCriticalChance)
            type = E_HitDecisionType.CriticalHit;

        foreach (ControllableObject obj in units)
        {
            obj.m_AttributeSystem.ApplyDamage(attack, type, null);
        }
    }

    private int TryParseInt(string text)
    {
        if (int.TryParse(text, out int result))
            return result;
        return 0;
    }

    private float TryParseFloat(string text)
    {
        if (float.TryParse(text, out float result))
            return result;
        return 0f;
    }
}
