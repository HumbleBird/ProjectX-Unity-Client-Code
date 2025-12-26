using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Define;

public class SelectionManager
{
    // 필드 / 프로퍼티 (유닛 리스트)
    private readonly HashSet<ISelectable> _selectedUnits = new();
    public IReadOnlyCollection<ISelectable> SelectedUnits => _selectedUnits;

    // Selection UI가 갱신되도록 하기 위해 필요.
    public event EventHandler OnSelectionChanged;

    // 단일 선택
    public void Select(ISelectable obj)
    {
        DeselectAll();
        Add(obj);
    }

    // (CTRL 선택, 박스 선택에 사용)
    public void Add(ISelectable obj)
    {
        if (obj == null) return;
        if (_selectedUnits.Add(obj))
            obj.OnSelected();

        OnSelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Deselect(ISelectable obj)
    {
        if (obj == null) return;
        if (_selectedUnits.Remove(obj))
            obj.OnDeselected();

        OnSelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    // (전체 해제)
    public void DeselectAll()
    {
        foreach (var unit in _selectedUnits)
            unit.OnDeselected();

        _selectedUnits.Clear();

        OnSelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    // (CTRL+클릭 기능)
    public void Toggle(ISelectable obj)
    {
        if (_selectedUnits.Contains(obj))
            Deselect(obj);
        else
            Add(obj);
    }


    public bool IsSelected(ISelectable obj)
    {
        return _selectedUnits.Contains(obj);
    }

    public IReadOnlyCollection<T> GetSelectedByClass<T>() where T : class
    {
        return _selectedUnits
            .OfType<T>()
            .ToList()
            .AsReadOnly();
    }


    public void Clear()
    {

    }
}
