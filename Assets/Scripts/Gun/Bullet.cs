using Fusion;
using UnityEngine;
using System.Collections;

public class Bullet : NetworkBehaviour
{
    [SerializeField] private float damage = 1f;
    [SerializeField] private float speed = 10f;

    public override void Spawned()
    {
        GetComponent<Rigidbody2D>().linearVelocity = transform.up * speed;

        StartCoroutine(DestroyAfterSeconds(2f));
    }

    private IEnumerator DestroyAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (Object.HasStateAuthority)
            Runner.Despawn(Object);
    }
}
