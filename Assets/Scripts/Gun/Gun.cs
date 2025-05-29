using Fusion;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Collections.Unicode;

public class Gun : MonoBehaviour
{
    [SerializeField] private Transform _shootPoint;
    [SerializeField] private NetworkPrefabRef _bulletPrefab;

    private List<Bullet> _activeBullets = new();
    private List<Bullet> _bulletDisablePool = new();

    private void OnEnable()
    {
        
    }

    private void OnDisable()
    {
        
    }

    public Bullet GetBullet()
    {
        if (_bulletDisablePool.Count <= 0) return null;

        Bullet temp = _bulletDisablePool[0];
        _bulletDisablePool.RemoveAt(0);

        return temp;
    }

    private void HandleShoot()
    {

    }

    private void HandleDisableBullet(Bullet bullet)
    {

    }
}
