using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    [SerializeField] private Transform ShootPoint;

    private List<Bullet> _activeBullets = new();
    private List<Bullet> _bulletDisablePool = new();

    private void HandleShoot()
    {

    }
}
