using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static Define;

/// <summary>
/// 직선형 발사체 런처
/// - 발사 순간의 타겟 위치를 고정하여 직선으로 이동
/// - 발사 시점의 적 위치를 기준으로 고정된 방향으로 직선 이동
/// </summary>
public class StraightLauncher : IProjectileLauncher
{
    public E_Projectile ProjectileType => E_Projectile.Straight;

    public void Launch(Projectile projectile, GameEntity attacker, GameEntity target, LaunchContext launchContext)
    {
        if (projectile == null)
            return;

        Vector3 targetPos = GetTargetPosition(target);

        // 1 고정 값이 아니라 소환 위치가 장애물보다 낮으면 위로 포물선을 그림 (TODO)
        if (launchContext.ObstacleHeight >= 1)
            attacker.StartCoroutine(LaunchParabola(projectile, targetPos, launchContext.ObstacleHeight));
        else
            attacker.StartCoroutine(LaunchStraight(projectile, targetPos));
    }

    private Vector3 GetTargetPosition(GameEntity target)
    {
        Vector3 baseCenter = target.m_HitCollider.bounds.center;
        float height = target.m_HitCollider.bounds.size.y;
        return baseCenter + Vector3.up * (height * (1f / 6f));
    }

    private IEnumerator LaunchStraight(Projectile projectile, Vector3 targetPos)
    {
        Vector3 initialDirection = (targetPos - projectile.m_Rigidbody.position).normalized;
        projectile.transform.rotation = Quaternion.LookRotation(initialDirection);

        // Rigidbody가 아닌 일반 위치를 사용
        Vector3 currentPos = projectile.m_Rigidbody.position;

        while (true)
        {
            if (projectile.m_IsHit)
            {
                yield break;
            }

            // Rigidbody 이동은 FixedUpdate 주기로 실행되므로 Time.deltaTime이 아닌 fixedDeltaTime을 사용하거나,
            // (MoveTowards를 피하고) 벡터 이동을 명확히 정의합니다.
            float step = projectile.m_fStraightSpeed * Time.fixedDeltaTime;

            // 1. 다음 위치 계산 (Vector3.MoveTowards를 사용하면 낮은 속도에서 정밀도가 떨어질 수 있음)
            Vector3 nextPosition = currentPos + initialDirection * step;

            // 2. Rigidbody 이동 명령
            projectile.m_Rigidbody.MovePosition(nextPosition);

            // 3. 현재 위치 업데이트 (다음 반복을 위해)
            currentPos = nextPosition;

            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator LaunchParabola(Projectile projectile, Vector3 targetPos, float obstacleHeight)
    {
        Vector3 start = projectile.transform.position;
        float duration = Vector3.Distance(start, targetPos) / projectile.m_fStraightSpeed;
        float t = 0;

        Vector3 previousPos = start; // 이전 위치 추적용 (회전 계산을 위해 필요)

        while (t < 1f)
        {
            if (projectile.m_IsHit)
            {
                Debug.Log("Startig에서 오브젝트 충돌로 이동 멈춤");
                yield break;
            }

            t += Time.fixedDeltaTime / duration;
            Vector3 pos = Vector3.Lerp(start, targetPos, t);
            pos.y += obstacleHeight * Mathf.Sin(t * Mathf.PI); // 반원형 곡선

            // 1. Rigidbody 이동
            projectile.m_Rigidbody.MovePosition(pos);

            // 2. Rigidbody의 속도 방향으로 회전 업데이트 (포물선은 회전이 필요)
            Vector3 direction = (pos - previousPos).normalized;
            if (direction != Vector3.zero)
            {
                projectile.m_Rigidbody.MoveRotation(Quaternion.LookRotation(direction));
            }
            previousPos = pos; // 현재 위치 저장

            yield return new WaitForFixedUpdate();
        }
    }
}

