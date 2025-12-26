using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Define;

public class StatBarUI : MonoBehaviour
{
    private AttributeSystem StatSystem;
    private GameEntity m_GameEntity;

    [SerializeField] private Image healthBarImage;
    [SerializeField] private Image ManaBarImage;
    [SerializeField] private GameObject ManaBar;
    [SerializeField] private GameObject healthBar;
    [SerializeField] private TextMeshProUGUI ObjectNameText;

    private void Awake()
    {
        m_GameEntity = GetComponentInParent<GameEntity>();
        m_GameEntity.OnObjectSpawned += (s, e) => Init();
        m_GameEntity.OnSpawnObjectSelected += (s, e) => SetActiveFalseBars();

        StatSystem = GetComponentInParent<AttributeSystem>();

        // Event
        StatSystem.OnUpdateStat += (s, e) => UpdateHealthBar();
        StatSystem.OnUpdateStat += (s, e) => UpdateManaBar();

        StatSystem.OnDead += (s, e) => SetActiveFalseBars();
        StatSystem.OnRevived += (s, e) => Init();


        if (m_GameEntity is ControllableObject cobj)
        {

            ObjectNameText.text = StatSystem.m_Stat.Name;
            switch (cobj.m_originalEObjectGrade)
            {
                case E_ObjectGrade.Normal:
                    ObjectNameText.gameObject.SetActive(true);
                    ObjectNameText.color = Color.white;
                    break;
                case E_ObjectGrade.Elite:
                    ObjectNameText.gameObject.SetActive(true);
                    ObjectNameText.color = Color.yellow;
                    break;
                case E_ObjectGrade.Boss:
                    ObjectNameText.gameObject.SetActive(true);
                    ObjectNameText.color = Color.red;
                    break;
                default:
                    break;
            }

            cobj.OnChangeGrade += UpdateGrade;
        }


    }

    private void Start()
    {
        if (!m_GameEntity.m_IsSetuping)
            Init();
    }

    public void Init()
    {
        healthBar.SetActive(true);

        UpdateHealthBar();

        if (StatSystem.IsManaCharacter())
        {
            UpdateManaBar();
            ManaBar.SetActive(true);
        }
        else
            ManaBar.SetActive(false);
    }

    private void UpdateHealthBar()
    {
        healthBarImage.fillAmount = StatSystem.GetHealthNormalized();
    }

    private void UpdateManaBar()
    {
        ManaBarImage.fillAmount = StatSystem.GetManaNormalized();
    }

    private void SetActiveFalseBars()
    {
        ManaBar.SetActive(false); 
        healthBar.SetActive(false);
        ObjectNameText.gameObject.SetActive(false);
    }

    private void UpdateGrade(object sender, ControllableObject.OnChangeGradeEventArgs args)
    {
        if (ObjectNameText == null)
            return;

        if (!args.isSuccessGrade)
        {
            ObjectNameText.text = StatSystem.m_Stat.Name;
        }
        else
        {
            string prefix = "";

            switch (args.gradeEnhanceType)
            {
                case E_ObjectEnhanceType.Health:
                    prefix = "Iron";          // 강철 같은 체력
                    break;
                case E_ObjectEnhanceType.Magic:
                    prefix = "Arcane";        // 비전의, 마법적인
                    break;
                case E_ObjectEnhanceType.Physical:
                    prefix = "Brutal";        // 잔혹하고 강력한 물리형
                    break;
                case E_ObjectEnhanceType.Defense:
                    prefix = "Bulwark";       // 방패 같은 수호자
                    break;
                case E_ObjectEnhanceType.Speed:
                    prefix = "Swift";         // 빠르고 민첩한
                    break;
                case E_ObjectEnhanceType.Critical:
                    prefix = "Deadeye";       // 명중률과 치명타에 강한
                    break;
                case E_ObjectEnhanceType.Range:
                    prefix = "Longshot";      // 장거리 공격형
                    break;
                case E_ObjectEnhanceType.Skill:
                    prefix = "Ascendant";     // 고급 스킬을 가진 존재
                    break;
                default:
                    prefix = "";
                    break;
            }

            // 예시 출력
            ObjectNameText.text = $"{prefix} {StatSystem.m_Stat.Name}";
        }


        // 표시와 등급
        switch (args.objGrade)
        {
            case E_ObjectGrade.Normal:
                ObjectNameText.gameObject.SetActive(true);
                ObjectNameText.color = Color.white;
                break;
            case E_ObjectGrade.Elite:
                ObjectNameText.gameObject.SetActive(true);
                ObjectNameText.color = Color.yellow;
                break;
            case E_ObjectGrade.Boss:
                ObjectNameText.gameObject.SetActive(true);
                ObjectNameText.color = Color.red;
                break;
            default:
                break;
        }

    }
}
