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
        StartShoot();
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData networkInput))
            return;

        transform.position += transform.up * speed * Time.deltaTime;
    }

    private IEnumerator DestroyAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (Object.HasStateAuthority)
            BulletDisable?.Invoke(this);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    public void Rpc_Respawn(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;
        gameObject.SetActive(true);
    }

    public void StartShoot()
    {
        StartCoroutine(DestroyAfterSeconds(2f));
    }

    public void SetPlayer(NetworkPlayerController player)
    {
        _selfPlayer = player;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out NetworkPlayerController player) && player != _selfPlayer)
        {
            Debug.Log("Damaged");
            if (player.CanReciveDamage() && player.Damage(damage)) EnemyKilled?.Invoke();
        }
    }

    public void DespawnObject()
    {
        Runner.Despawn(Object);
    }
}
