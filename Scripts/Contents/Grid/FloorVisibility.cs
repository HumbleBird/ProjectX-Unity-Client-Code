using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

public class FloorVisibility : MonoBehaviour
{
    // 객체의 현재 층을 움직일 때마다 다시 계산할지 여부
    [SerializeField] private bool dynamicFloorPosition;

    // 숨김/보임 처리에서 제외할 Renderer 목록
    [SerializeField] private List<Renderer> ignoreRendererList;

    // 이 오브젝트의 모든 자식 Renderer 캐싱
    private Renderer[] rendererArray;

    // 이 오브젝트가 위치한 층
    private int floor;

    private void Awake()
    {
        // 비활성화된 Renderer 포함 전체 수집
        rendererArray = GetComponentsInChildren<Renderer>(true);
    }

    private void Start()
    {
        // 시작 시 위치 기반으로 층 계산
        floor = Managers.SceneServices.Grid.GetFloor(transform.position);

        // 0층이고, 고정 층이면 스크립트 필요 없음 → 삭제
        if (floor == 0 && !dynamicFloorPosition)
        {
            Destroy(this);
        }
    }

    private void Update()
    {
        // 오브젝트가 이동할 수 있는 구조라면 매 프레임 층 다시 계산
        if (dynamicFloorPosition)
        {
            floor = Managers.SceneServices.Grid.GetFloor(transform.position);
        }

        // 카메라의 현재 높이 가져오기
        float cameraHeight = Managers.SceneServices.CameraRig.GetCameraHeight();

        // 층별로 어느 높이에서 보일지 설정하는 오프셋
        float floorHeightOffset = 2f;

        // 카메라가 해당 층보다 충분히 위에 있으면 보이도록 설정
        bool showObject = cameraHeight > FLOOR_HEIGHT * floor + floorHeightOffset;

        // 0층은 항상 보임
        if (showObject || floor == 0)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    // Renderer 보이기
    private void Show()
    {
        foreach (Renderer renderer in rendererArray)
        {
            if (ignoreRendererList.Contains(renderer)) continue;
            renderer.enabled = true;
        }
    }

    // Renderer 숨기기
    private void Hide()
    {
        foreach (Renderer renderer in rendererArray)
        {
            if (ignoreRendererList.Contains(renderer)) continue;
            renderer.enabled = false;
        }
    }
}
