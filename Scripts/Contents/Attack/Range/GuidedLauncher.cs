using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Define;

/// <summary>
/// 유도형 발사체 런
/// - 장애물 존재 시 포물선 상승 후 유도 추적
/// - 실시간 타겟 추적, 타겟 사망 시 직선형으로 전환
/// </summary>
public class GuidedLauncher : IProjectileLauncher
{
    public E_Projectile ProjectileType => E_Projectile.Guided;

    // 곡사 높이 계수
    private const float ARC_HEIGHT_MULTIPLIER = 1.2f;
    // 곡사 지속 시간 비율
    private const float ARC_PHASE = 0.4f;

    public void Launch(Projectile projectile, GameEntity attacker, GameEntity target, LaunchContext launchContext)
    {
        if (projectile == null)
        {
            Debug.LogError($"{attacker}의 프로젝일이 존재하지 않습니다.");
            return;
        }

        if (launchContext.ObstacleHeight >= 1)
            attacker.StartCoroutine(LaunchParabola(projectile, attacker, target, launchContext));
        else
            attacker.StartCoroutine(LaunchTrackingOrStraight(projectile, attacker, target, launchContext));

    }

    private Vector3 GetTargetPosition(GameEntity target)
    {
        Vector3 baseCenter = target.m_HitCollider.bounds.center;
        float height = target.m_HitCollider.bounds.size.y;
        return baseCenter + Vector3.up * (height * (1f / 6f));
    }


    /// <summary>
    /// ① 장애물이 있을 때, 발사체를 위로 부드럽게 포물선 형태로 이동시킨 후  
    /// ② 최고점에 도달하면 유도 혹은 직선 이동(LaunchTrackingOrStraight)으로 전환한다.
    /// </summary>
    private IEnumerator LaunchParabola(Projectile projectile, GameEntity attacker, GameEntity target, LaunchContext launchContext)
    {
        // 🔹 시작 위치(발사 지점)와 목표 위치(타겟 중심) 계산
        Vector3 startPos = projectile.transform.position;
        Vector3 targetPos = GetTargetPosition(target);
        float speed = projectile.m_fStraightSpeed;

        // 🔹 전체 거리 및 포물선 구간의 지속 시간 계산 (ARC_PHASE = 포물선 비율)
        float totalDist = Vector3.Distance(startPos, targetPos);
        float arcDuration = totalDist / speed * ARC_PHASE;
        float elapsed = 0f;

        // ✅ 최고 높이 = Collider 길이 + 장애물 높이
        float arcHeight = Mathf.Max(launchContext.ColliderLength + launchContext.ObstacleHeight, 0.5f);

        // 🔹 포물선의 최고점(중간지점 + y축 상승)을 계산
        Vector3 arcPeak = startPos + (targetPos - startPos) * 0.5f + Vector3.up * arcHeight;

        // ================================
        // 1️ 포물선 상승 구간
        // ================================
        while (elapsed < arcDuration)
        {
            if (projectile == null) yield break;

            if (projectile.m_IsHit)
            {
                Debug.Log("Guided에서 오브젝트 충돌로 이동 멈춤");
                yield break;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / arcDuration);

            // 🔸 시작점 → 최고점까지 부드럽게 보간 (상승 곡선)
            Vector3 pos = Vector3.Lerp(startPos, arcPeak, t);
            pos.y += Mathf.Sin(t * Mathf.PI * 0.5f) * arcHeight * 0.2f; // 살짝 곡선 부드럽게

            // 🔸 Rigidbody를 이용해 이동
            projectile.m_Rigidbody.MovePosition(pos);

            // 🔸 이동 방향을 현재 궤적 방향으로 맞춤
            projectile.transform.rotation = Quaternion.LookRotation((arcPeak - pos).normalized);

            yield return new WaitForFixedUpdate(); // FixedUpdate 주기(물리 업데이트 단위)마다 이동
        }

        // ================================
        // 2️⃣ 포물선 최고점 도달 후 → 유도 이동 전환
        // ================================
        yield return LaunchTrackingOrStraight(projectile, attacker, target, launchContext);
    }




    private IEnumerator LaunchTrackingOrStraight
        (Projectile projectile, GameEntity attacker, GameEntity target, LaunchContext launchContext)
    {
        Vector3 startPos = projectile.transform.position;

        while (projectile != null)
        {

            if (projectile.m_IsHit)
            {
                Debug.Log("Guided에서 오브젝트 충돌로 이동 멈춤");
                yield break;
            }


            // 타겟이 사망시 직선형 전환
            if (target == null || target.m_AttributeSystem.m_IsDead)
            {
                Vector3 dir = projectile.m_Rigidbody.velocity.normalized;
                //if (dir == Vector3.zero)
                //    dir = (target.transform.position - projectile.transform.position).normalized;

                yield return LaunchStraight(projectile, dir);
                yield break;
            }

            // 1. 목표 위치 계산
            Vector3 currentTargetPos = GetTargetPosition(target);

            // 2. 목표를 향하는 방향 벡터 계산
            Vector3 directionToTarget = (currentTargetPos - projectile.transform.position).normalized;

            // 3. 발사체가 목표를 바라보도록 부드럽게 회전
            // 추적 미사일은 부드럽게 방향을 전환해야 자연스러움. (여기서는 바로 회전)
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);

            // 부드러운 회전을 원한다면 Slerp 사용 (예: 5f는 회전 속도)
            projectile.transform.rotation = Quaternion.Slerp(projectile.transform.rotation, targetRotation, Time.fixedDeltaTime * projectile.ParabolaSpeed);

            // 4. 다음 위치 계산 (속도와 방향을 기반으로 이동)
            // MovePosition을 사용하되, 현재 방향으로 이동합니다.
            Vector3 nextPosition = projectile.transform.position + directionToTarget * projectile.m_fStraightSpeed * Time.fixedDeltaTime;

            // 5. Rigidbody 이동 명령
            projectile.m_Rigidbody.MovePosition(nextPosition);

            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator LaunchStraight(Projectile projectile, Vector3 direction)
    {
        Vector3 initialDirection = direction;
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

            // 발사체가 이동 방향을 바라보도록 Rotation 업데이트
            if (initialDirection != Vector3.zero)
            {
                projectile.transform.rotation = Quaternion.LookRotation(initialDirection);
            }

            // 3. 현재 위치 업데이트 (다음 반복을 위해)
            currentPos = nextPosition;

            yield return new WaitForFixedUpdate();
        }
    }
}

