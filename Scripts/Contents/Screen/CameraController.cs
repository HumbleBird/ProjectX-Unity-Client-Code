using UnityEngine;
using Unity.Cinemachine;
using System;
using static Define;

public class CameraController : MonoBehaviour, ICameraRig, ICameraInfoProvider, ICameraShakeSettings
{
    private Define.ICameraInput _cameraInput;
    private IInputQuery _input;

    public EventHandler<int> OnChangeLookFloor;

    private CinemachineCamera m_CM;
    public Transform m_Follow;
    private Vector3 targetFollowOffset;

    public Camera m_UICamera;

    [Header("Main Cinemachine")]
    private CinemachineRotationComposer m_CMRotationComposer;
    private CinemachineInputAxisController m_CMInputAxisController;
    private CinemachineImpulseListener m_CMImpulseListener;
    private event EventHandler<int> _onChangeLookFloor;

    event EventHandler<int> ICameraRig.OnChangeLookFloor
    {
        add { _onChangeLookFloor += value; }
        remove { _onChangeLookFloor -= value; }
    }

    private void ChangeFloor(int floor)
    {
        _onChangeLookFloor?.Invoke(this, floor);
    }

    private void Awake()
    {
        Managers.SceneServices.Register<ICameraRig>(this);
        Managers.SceneServices.Register<ICameraInfoProvider>(this);
        Managers.SceneServices.Register<ICameraShakeSettings>(this);

        m_CM =  GetComponentInChildren<CinemachineCamera>();
        m_CMRotationComposer =  GetComponentInChildren<CinemachineRotationComposer>();
        m_CMInputAxisController =  GetComponentInChildren<CinemachineInputAxisController>();
        m_CMImpulseListener =  GetComponentInChildren<CinemachineImpulseListener>();
    }

    private void Start()
    {
        targetFollowOffset = m_CM.Target.TrackingTarget.transform.position;

        _cameraInput = Managers.SceneServices.CameraInput;
        _input = Managers.SceneServices.InputQuery;
    }

    private void Update()
    {
        HandleMovement();
        HandleEnableCMController();
    }

    private void HandleMovement()
    {
        Vector2 inputMoveDir = _cameraInput.GetCameraMoveVector();

        float moveSpeed = 10f;

        // Cinemachine 카메라의 방향 기준
        Vector3 forward = m_CM.transform.forward;
        Vector3 right = m_CM.transform.right;

        // 수직 방향은 제거 (y축 이동 방지)
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        // 입력 벡터 기반으로 이동
        Vector3 moveVector = forward * inputMoveDir.y + right * inputMoveDir.x;
        m_Follow.position += moveVector * moveSpeed * Time.deltaTime;
    }

    // 마우스 우클릭 시에만 작동하게
    private void HandleEnableCMController()
    {
        bool rightHold = _input.IsActive(E_InputEvent.RightHold);

        if (rightHold)
        {
            m_CMInputAxisController.Controllers[0].Enabled = true; // X
            m_CMInputAxisController.Controllers[1].Enabled = true; // Y
        }
        else
        {
            m_CMInputAxisController.Controllers[0].Enabled = false; // X
            m_CMInputAxisController.Controllers[1].Enabled = false; // Y
        }
    }

    public float GetCameraHeight()
    {
        return targetFollowOffset.y;
    }

    public void SetPositionAndRotation(Vector3 position, Quaternion rotation) => m_Follow.transform.SetPositionAndRotation(position, rotation);

    public Vector3 Position => m_Follow.transform.position;
    public Quaternion Rotation => m_Follow.transform.rotation;

    // TODO
    public int CurrentLookFloor => 0;

    public void SetImpulseReactionDuration(float duration)
    {
        if (m_CMImpulseListener != null)
            m_CMImpulseListener.ReactionSettings.Duration = duration;
    }
}