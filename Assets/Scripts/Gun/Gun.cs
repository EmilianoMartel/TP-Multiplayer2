using Fusion;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.VisualScripting.Antlr3.Runtime.Misc;

public class Gun
{
    private float _coldDownShoot = 0;
    private bool _canShoot = true;

    private PlayerStats _stats;

    private List<Bullet> _activeBullets = new();
    private List<Bullet> _bulletDisablePool = new();

    public bool CanShoot { get { return _canShoot; } }

    public Action EnemyKilled;

    public Gun(float coldDown, PlayerStats stats)
    {
        _coldDownShoot = coldDown;
        _stats = stats;
    }

    public void DisableGun()
    {
        foreach (var bullet in _activeBullets)
        {
            _activeBullets.Remove(bullet);
            bullet.BulletDisable -= HandleDisableBullet;
            bullet.DespawnObject();
        }

        foreach (var bullet in _bulletDisablePool)
        {
            _bulletDisablePool.Remove(bullet);
            bullet.BulletDisable -= HandleDisableBullet;
            bullet.DespawnObject();
        }
    }

    public Bullet GetBullet(Transform shootPoint)
    {
        if (_bulletDisablePool.Count <= 0) return null;

        Bullet temp = _bulletDisablePool[0];
        _bulletDisablePool.RemoveAt(0);
        temp.Rpc_Respawn(shootPoint.position,shootPoint.rotation);
        temp.StartShoot();
        return temp;
    }

    public void NewBulletStarted(Bullet bullet)
    {
        _activeBullets.Add(bullet);
        bullet.BulletDisable += HandleDisableBullet;
        bullet.EnemyKilled += () => _stats.AddKill();
    }

    private void HandleDisableBullet(Bullet bullet)
    {
        if(_activeBullets.Contains(bullet)) _activeBullets.Remove(bullet);

        _bulletDisablePool.Add(bullet);
        bullet.gameObject.SetActive(false);
    }

    public IEnumerator ShootColdDown()
    {
        _canShoot = false;
        yield return new WaitForSeconds(_coldDownShoot);
        _canShoot = true;
    }
}
