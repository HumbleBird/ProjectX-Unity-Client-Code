//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using static Define;

///// <summary>
///// 다중 발사체 런처
///// - 여러 개의 발사체를 순차적으로 발사
///// </summary>
//public class MultipleLauncher : IProjectileLauncher
//{
//    public E_Projectile ProjectileType => E_Projectile.Multiple;

//    public int GetRequiredProjectileCount() => 3;
//    private int _projectileCount = 3;
//    private E_Projectile _projectileType = E_Projectile.Straight;

//    private WaitForFixedUpdate _waitFixed = new();
//    private WaitForSeconds _waitSeconds = new(0.3f);


//    public void Launch(Projectile projectiles, GameEntity attacker, GameEntity target, LaunchContext launchContext)
//    {
//        if (projectiles == null)
//            return;

//        attacker.StartCoroutine(DelayedLaunch());

//        IEnumerator DelayedLaunch()
//        {
//            for (int i = 0; i < projectiles.Count; i++)
//            {
//                if (projectiles[i] == null)
//                    continue;
//                attacker.StartCoroutine(Co_Launch(projectiles[i], attacker, target));
//                yield return _waitSeconds;
//            }
//        }
//    }

//    private IEnumerator Co_Launch(Projectile projectile, GameEntity attacker, GameEntity target)
//    {
//        // 궤적 타입에 따라 발사 방식 선택
//        if (_projectileType == E_Projectile.Guided)
//        {
//            Vector3 startPos = projectile.transform.position;
//            Vector3 targetPos = GetTargetPosition(target);
//            attacker.StartCoroutine(LaunchParabola(projectile, attacker, target, startPos, targetPos));
//        }
//        else
//        {
//            Vector3 targetPos = GetTargetPosition(target);
//            attacker.StartCoroutine(LaunchStraight(projectile, targetPos));
//        }

//        yield break;
//    }

//    private Vector3 GetTargetPosition(GameEntity target)
//    {
//        Vector3 baseCenter = target.m_HitCollider.bounds.center;
//        float height = target.m_HitCollider.bounds.size.y;
//        return baseCenter + Vector3.up * (height * (1f / 6f));
//    }

//    private IEnumerator LaunchStraight(Projectile projectile, Vector3 targetPos, float speed, bool useDirection = false, float maxDistance = 50f)
//    {
//        float traveledDistance = 0f;
//        Vector3 direction = useDirection ? targetPos.normalized : Vector3.zero;

//        while (true)
//        {
//            if (projectile == null)
//                break;

//            if (useDirection && traveledDistance >= maxDistance)
//                break;

//            if (useDirection == false && Vector3.Distance(projectile.transform.position, targetPos) <= 0.1f)
//                break;

//            // 다음 위치 계산
//            Vector3 nextPos = useDirection
//                ? projectile.transform.position + direction * speed * Time.deltaTime
//                : Vector3.MoveTowards(projectile.transform.position, targetPos, speed * Time.deltaTime);

//            projectile.m_Rigidbody.MovePosition(nextPos);

//            if (useDirection)
//                traveledDistance += speed * Time.deltaTime;

//            if (IsCoroutineOut(projectile.transform.position, targetPos))
//                yield break;

//            yield return _waitFixed;
//        }

//        // 방향 모드일 때만 발사체 파괴
//        if (useDirection)
//            projectile?.Destroy();
//    }

//    /// <summary>
//    /// 포물선 궤적으로 발사체를 발사 (타겟 실시간 추적)
//    /// </summary>
//    private IEnumerator LaunchParabola(Projectile projectile, GameEntity attacker, GameEntity target,
//        Vector3 start, Vector3 end, float speed, float boundHeight)
//    {
//        float elapsedTime = 0f;

//        while (true)
//        {
//            if (projectile == null)
//                break;

//            // 타겟이 사망했는지 확인
//            if (target == null || target.m_AttributeSystem.m_IsDead)
//            {
//                // 직선형으로 전환
//                Vector3 currentDirection = projectile.m_Rigidbody.velocity.normalized;
//                if (currentDirection == Vector3.zero)
//                    currentDirection = (target != null ? target.transform.position - projectile.transform.position : Vector3.forward).normalized;

//                yield return LaunchStraight(projectile, currentDirection, speed, useDirection: true);
//                yield break;
//            }

//            // 타겟의 현재 위치를 실시간으로 가져옴
//            Vector3 targetPos = GetTargetPosition(target);
//            float distance = Vector3.Distance(start, targetPos);
//            float duration = distance / speed;

//            elapsedTime += Time.deltaTime;
//            float t = Mathf.Clamp01(elapsedTime / duration);

//            // 포물선 궤적 계산
//            Vector3 destPos = Vector3.Lerp(start, targetPos, t);
//            destPos.y += boundHeight * Mathf.Sin(t * Mathf.PI);

//            projectile.m_Rigidbody.MovePosition(destPos);

//            if (IsCoroutineOut(projectile.transform.position, targetPos))
//                yield break;

//            yield return _waitFixed;
//        }
//    }

//    private bool IsCoroutineOut(Vector3 start, Vector3 dest)
//    {
//        if (Vector3.Distance(start, dest) <= 0.1f)
//            return true;

//        return false;
//    }
//}

