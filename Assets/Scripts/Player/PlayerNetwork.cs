using Fusion;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    public float speed = 5f;
    public GameObject bulletPrefab;
    public Transform firePoint;

    public override void FixedUpdateNetwork()
    {
        if (GetInput<NetworkInputData>(out var input))
        {
            Vector2 move = input.Direction * speed * Runner.DeltaTime;
            transform.position += new Vector3(move.x, move.y, 0);

            if (input.Shoot)
            {
                Runner.Spawn(bulletPrefab, firePoint.position, firePoint.rotation, Object.InputAuthority);
            }
        }
    }
}