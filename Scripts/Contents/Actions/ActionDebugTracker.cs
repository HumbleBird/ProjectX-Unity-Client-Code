using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 액션 시스템 디버깅을 위한 추적 컴포넌트
/// Inspector에서 실시간 액션 상태 확인 및 전환 히스토리 추적
/// </summary>
public class ActionDebugTracker : MonoBehaviour
{
    [Header("현재 액션 상태")]
    [SerializeField] private string _currentActionName = "None";
    [SerializeField] private string _nextActionName = "None";
    [SerializeField] private string _beforeActionName = "None";
    [SerializeField] private string _commandActionName = "None";
    [SerializeField] private string _targetName = "None";

    [Header("액션 전환 히스토리")]
    // 최근 액션 전환 기록 (최신이 위)
    [SerializeField] private List<string> _actionHistory = new();  
    [SerializeField] private int _maxHistoryCount = 20;

    [Header("디버그 옵션")]
    // 액션 전환 시 로그 출력
    [SerializeField] private bool _logActionChanges = true;
    
    private ControllableObject _controllableObject;
    private BaseAction _lastTrackedAction;

    private void Awake()
    {
        _controllableObject = GetComponent<ControllableObject>();

        if (_controllableObject == null)
        {
            Debug.LogWarning($"[ActionDebugTracker] {gameObject.name}에 ControllableObject가 없습니다.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (_controllableObject == null)
            return;

        UpdateActionState();
    }

    /// <summary>
    /// 액션 상태 갱신 및 전환 감지
    /// </summary>
    private void UpdateActionState()
    {
        var currentAction = _controllableObject.m_CurrentAction;

        // 액션 전환 감지
        if (currentAction != _lastTrackedAction)
        {
            OnActionChanged(_lastTrackedAction, currentAction);
            _lastTrackedAction = currentAction;
        }

        // Inspector 표시용 정보 갱신
        _currentActionName = GetActionName(currentAction);
        _nextActionName = GetActionName(GetPrivateField<BaseAction>("m_NextAction"));
        _beforeActionName = GetActionName(GetPrivateField<BaseAction>("m_BeforeAction"));
        _commandActionName = "현재 Queue로 변경함.";// GetActionName(_controllableObject.m_command);
        _targetName = _controllableObject.m_Target?.name ?? "None";
    }

    /// <summary>
    /// 액션 전환 이벤트 처리
    /// </summary>
    private void OnActionChanged(BaseAction from, BaseAction to)
    {
        string fromName = GetActionName(from);
        string toName = GetActionName(to);
        string timeInfo = $"[T:{Time.time:F2}]";

        // 히스토리 추가
        string historyEntry = $"{timeInfo} {fromName} → {toName}";
        _actionHistory.Insert(0, historyEntry);

        // 최대 개수 제한
        if (_actionHistory.Count > _maxHistoryCount)
            _actionHistory.RemoveAt(_actionHistory.Count - 1);

        // 로그 출력
        if (_logActionChanges)
        {
            string log = $"[액션 전환] {gameObject.name}: {fromName} → {toName}";
            Debug.Log(log, this);
        }
    }

    private string GetActionName(BaseAction action)
    {
        if (action == null)
            return "None";

        return action.m_actionName;
    }

    private T GetPrivateField<T>(string fieldName) where T : class
    {
        var field = typeof(ControllableObject).GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        var ret = field?.GetValue(_controllableObject) as T;
        return ret;
    }

    [ContextMenu("현재 액션 상태 출력")]
    private void PrintCurrentState()
    {
        if (_controllableObject == null)
        {
            Debug.LogWarning("ControllableObject가 없습니다.", this);
            return;
        }

        string ret = $"=== {gameObject.name} 액션 상태 ===\n" +
                     $"현재 액션: {_currentActionName}\n" +
                     $"다음 액션: {_nextActionName}\n" +
                     $"이전 액션: {_beforeActionName}\n" +
                     $"커맨드 액션: {_commandActionName}\n" +
                     $"타겟: {_targetName}\n" +
                     $"프레임: {Time.frameCount}\n" +
                     $"시간: {Time.time:F2}초";

        Debug.Log(ret, this);
    }

    [ContextMenu("히스토리 초기화")]
    private void ClearHistory()
    {
        _actionHistory.Clear();
        Debug.Log($"[ActionDebugTracker] {gameObject.name}의 히스토리를 초기화했습니다.", this);
    }

    [ContextMenu("히스토리 전체 출력")]
    private void PrintFullHistory()
    {
        if (_actionHistory.Count == 0)
        {
            Debug.Log($"[ActionDebugTracker] {gameObject.name}의 히스토리가 비어있습니다.", this);
            return;
        }

        string ret = $"=== {gameObject.name} 액션 히스토리 ({_actionHistory.Count}개) ===\n";
        ret += string.Join("\n", _actionHistory);

        Debug.Log(ret, this);
    }
}

