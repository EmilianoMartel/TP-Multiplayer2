using Fusion;
using UnityEngine;
using System.Collections;
using System;

public class Bullet : NetworkBehaviour
{
    [SerializeField] private float damage = 1f;
    [SerializeField] private float speed = 10f;

    private Rigidbody2D _rb;
    private NetworkPlayerController _selfPlayer;

    public Action EnemyKilled;
    public Action<Bullet> BulletDisable;

    public override void Spawned()
    {
        _rb = GetComponent<Rigidbody2D>();

        StartShoot();
    }

    private IEnumerator DestroyAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (Object.HasStateAuthority)
            BulletDisable?.Invoke(this);
    }

    public void StartShoot()
    {
        _rb.linearVelocity = transform.up * speed;

        StartCoroutine(DestroyAfterSeconds(2f));
    }

    public void SetPlayer(NetworkPlayerController player)
    {
        _selfPlayer = player;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out NetworkPlayerController player) && player != _selfPlayer)
            if(player.CanReciveDamage() && player.Damage(damage)) EnemyKilled?.Invoke();
    }

    public void DespawnObject()
    {
        Runner.Despawn(Object);
    }
}
