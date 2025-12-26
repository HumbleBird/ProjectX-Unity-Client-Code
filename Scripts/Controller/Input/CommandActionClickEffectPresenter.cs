using UnityEngine;
using CodeMonkey.Utils;
using static Define;

public class CommandActionClickEffectPresenter : MonoBehaviour
{
    [Header("Click Effect")]
    [SerializeField] private Transform worldUITransform;
    [SerializeField] private GameObject commandActionAtGridEffectPrefab;
    [SerializeField] private float defaultHeight = 4f;

    private GameObject _activeEffect;

    private IGridQuery _grid;
    private CommandManager _command;

    private void Awake()
    {
        _command = Managers.Command; // Managers.Command가 CommandManager 인스턴스인 전제
    }

    private void Start()
    {
        _grid = Managers.SceneServices.Grid;
    }

    private void OnEnable()
    {
        if (_command != null)
            _command.OnCommandAction += HandleCommandAction;
    }

    private void OnDisable()
    {
        if (_command != null)
            _command.OnCommandAction -= HandleCommandAction;
    }

    private void HandleCommandAction(object sender, CommandManager.OnCommandActionEventArgs e)
    {
        // 안전장치
        if (_grid == null || commandActionAtGridEffectPrefab == null || worldUITransform == null)
            return;

        float height = defaultHeight;

        // 공격이면 대상 높이에 맞춰 올려주기
        if (e.action == typeof(CommandAttackAction))
        {
            var target = _grid.GetCellEntity(e.GridPosition);
            if (target == null) return;

            height += target.m_HitCollider.bounds.max.y;
        }

        // 기존 이펙트 제거
        if (_activeEffect != null)
            Managers.Resource.Destroy(_activeEffect);

        // 이펙트 생성
        _activeEffect = Managers.Resource.Instantiate(commandActionAtGridEffectPrefab, worldUITransform);
        _activeEffect.transform.position = _grid.GetWorldPosition(e.GridPosition) + new Vector3(0, height, 0);

        // 5초 후 제거
        FunctionTimer.Create(() =>
        {
            if (_activeEffect != null)
                Managers.Resource.Destroy(_activeEffect.gameObject);
        }, 5f);
    }
}
