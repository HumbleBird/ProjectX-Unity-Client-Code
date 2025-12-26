using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeveloperToolUI : MonoBehaviour
{
    [Header("Com")]
    DeveloperTool_BaseObject developerTool_Attack;

    public Button m_RunButton;
    public TMP_Dropdown m_UnitDropDown;
    public TMP_Dropdown m_MethodDropDown;


    private List<GameEntity> m_SelectObjectList = new();

    public void Awake()
    {
        // Unit Drop Down
        m_UnitDropDown.ClearOptions();
        Managers.Selection.OnSelectionChanged += UpdateUnitDropdown;
        Managers.Object.OnAdd +=  AddDropdownOption;
        Managers.Object.OnRemove +=  AddDropdownOption;

        // Method Drop Down
        m_MethodDropDown.ClearOptions();
        m_MethodDropDown.options.Add(new TMP_Dropdown.OptionData("Select Object Damaged"));

        m_RunButton.onClick.AddListener(ButtonDownSelectMethod);

        // Com
        developerTool_Attack = GetComponentInChildren<DeveloperTool_BaseObject>();
    }

    public void Start()
    {
        m_UnitDropDown.onValueChanged.AddListener(OnDropdownValueChanged);
    }

    #region Unit DropDown

    public void AddDropdownOption(object sender, EventArgs e)
    {
        if (sender is ControllableObject baseObject)
        {
            string name = baseObject.name;

            // 이미 있는지 확인 후 중복 방지
            if (m_UnitDropDown.options.Any(opt => opt.text == name))
                return;

            m_UnitDropDown.options.Add(new TMP_Dropdown.OptionData(name));
        }
    }

    public void RemoveDropdownOption(object sender, EventArgs e)
    {
        if (sender != null && sender is ControllableObject baseObject)
        {
            string name = baseObject.name;

            // 옵션 목록에서 해당 이름의 인덱스를 찾음
            int index = m_UnitDropDown.options.FindIndex(opt => opt.text == name);

            // 인덱스가 존재하면 삭제
            if (index != -1)
            {
                m_UnitDropDown.options.RemoveAt(index);
            }
        }
    }

    public void UpdateUnitDropdown(object sender, EventArgs e)
    {
        //var list = Managers.SceneServices.UnitActionTick.m_SelectedObjects;

        //m_UnitDropDown.captionText.text = string.Join(", ", list.Select(unit => unit.gameObject.name));

        //if (list.Count > 0)
        //    OnDropdownValueChanged();
    }

    private void UpdateSelectUnit()
    {
        // 이름을 이용해 유닛 매니저에서 가져오기
        string names = m_UnitDropDown.captionText.text;
        if (string.IsNullOrEmpty(name))
            return;

        string[] rtxString = names.Split(", ");

        m_SelectObjectList =  Managers.Object.GetObjectListByName<GameEntity>(rtxString).ToList();
        if (m_SelectObjectList.Count == 0)
        {
            Debug.Log("선택된 유닛이 없습니다.");
            return;
        }
    }

    void OnDropdownValueChanged()
    {
        string selectedText = m_UnitDropDown.captionText.text;
        //Debug.Log(selectedText + "유닛을 클릭해서 선택");

        UpdateSelectUnit();
    }

    void OnDropdownValueChanged(int index)
    {
        string selectedText = m_UnitDropDown.options[index].text;
        //Debug.Log(selectedText + " 드롭다운에서 선택");

        UpdateSelectUnit();
    }

    #endregion

    public void ButtonDownSelectMethod()
    {
        if (m_SelectObjectList.Count == 0)
        {
            Debug.Log("선택된 유닛이 없습니다.");
            return;
        }

        if (m_MethodDropDown.value == 0)
        {
            developerTool_Attack.DamageObject(m_SelectObjectList);
        }
    }


}




