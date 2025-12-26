using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Define;

// 플레이이어 유닛은 플레이어가 직접 지정할 때까지 파괴 불가.
// 적 유닛은 아직 미정
public class PassiveObject : GameEntity
{
    [SerializeField] private GameObject crateDestroyedPrefab;

    public override void DeSpawnComplete()
    {
        var crateDestroyedTransform = Managers.Resource.Instantiate(crateDestroyedPrefab, transform.position, transform.rotation);
        ApplyExplosionToChildren(crateDestroyedTransform, 150f, transform.position, 10f);

        base.DeSpawnComplete();
    }

    private void ApplyExplosionToChildren(GameObject root, float explosionForce, Vector3 explosionPosition, float explosionRange)
    {
        foreach (Transform child in root.transform)
        {
            if (child.TryGetComponent<Rigidbody>(out Rigidbody childRigidbody))
            {
                childRigidbody.AddExplosionForce(explosionForce, explosionPosition, explosionRange);
            }

            ApplyExplosionToChildren(child.gameObject, explosionForce, explosionPosition, explosionRange);
        }
    }
}
