using System;
using UnityEngine;
using Fusion;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(Rigidbody2D))]
public class NetworkPlayerController : NetworkBehaviour
{
    [SerializeField] private Transform _cameraTarget;
    [SerializeField] private GameObject _view;

    [SerializeField] private PlayerStats _stats;

    [Header("Shoot")]
    [SerializeField] private InputActionReference shootAction;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float _shootColdDown = 1.5f;
    [SerializeField] private NetworkPrefabRef bulletPrefab;

    [Header("Parameters")]
    [SerializeField] private float _respawnColdDown = 1.5f;
    [SerializeField] private float _maxLife = 10f;
    [SerializeField] private float _moveSpeed = 5f;

    //Movement
    private Rigidbody2D _rigidbody2D;
    private bool isMoving;
    private Vector3 _topLeftLimit;
    private Vector3 _bottomRightLimit;
    private float _halfWidth;
    private float _halfHeight;

    //Life Variables
    private float _currentLife;
    private bool _canReciveDamage = true;

    //Collition
    private Collider2D _collider;

    private Gun _gun;

    public event Action OnDeath;
    public event Action OnKilledEnemy;
    public event Action OnMovementStarted;
    public event Action OnMovementStopped;

    private void OnEnable()
    {
        shootAction.action.performed += HandleShoot;

        if (gameObject.TryGetComponent(out Collider2D collider))
            _collider = collider;
        else
            Debug.LogWarning($"{name}: Collider is null");

        if (_collider != null)
        {
            Bounds bounds = _collider.bounds;
            _halfWidth = bounds.extents.x;
            _halfHeight = bounds.extents.y;
        }
    }

    private void OnDisable()
    {
        shootAction.action.performed -= HandleShoot;
    }

    private void Awake()
    {
        _gun = new(_shootColdDown,_stats);
        _currentLife = _maxLife;
        _rigidbody2D = GetComponent<Rigidbody2D>();

        SetCameraLimits();
    }

    public override void Spawned()
    {
        if (!Object.HasInputAuthority)
            return;

        NetworkManager.Instance.LocalPlayer = this;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (!Object.HasInputAuthority)
            return;

        NetworkManager.Instance.LocalPlayer = null;
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData networkInput))
            return;

        Vector2 moveDir = GetMoveDirection(networkInput);
        bool wasMoving = isMoving;
        isMoving = moveDir != Vector2.zero;

        if (isMoving)
        {
            Vector2 nextPosition = _rigidbody2D.position + moveDir * _moveSpeed * Runner.DeltaTime;

            nextPosition.x = Mathf.Clamp(
                nextPosition.x,
                _topLeftLimit.x + _halfWidth,
                _bottomRightLimit.x - _halfWidth);

            nextPosition.y = Mathf.Clamp(
                nextPosition.y,
                _bottomRightLimit.y + _halfHeight,
                _topLeftLimit.y - _halfHeight);

            transform.position = new Vector3(nextPosition.x, nextPosition.y, 0);
            if (!wasMoving)
                OnMovementStarted?.Invoke();
        }
        else if (wasMoving)
        {
            OnMovementStopped?.Invoke();
        }

        RotateAimTarget(networkInput.LookDirection);
    }

    private Vector2 GetMoveDirection(NetworkInputData networkInput)
    {
        Vector2 moveDirection = Vector2.zero;

        if (networkInput.IsInputDown(NetworkInputType.MoveForward))
            moveDirection += Vector2.up;

        if (networkInput.IsInputDown(NetworkInputType.MoveBackwards))
            moveDirection += Vector2.down;

        if (networkInput.IsInputDown(NetworkInputType.MoveLeft))
            moveDirection += Vector2.left;

        if (networkInput.IsInputDown(NetworkInputType.MoveRight))
            moveDirection += Vector2.right;

        return moveDirection.normalized;
    }

    private void RotateAimTarget(Vector2 lookDir)
    {
        float rotationZ = Mathf.Atan2(lookDir.y, lookDir.x) * Mathf.Rad2Deg;
        _view.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ - 90f); //This magic number is the offset view
    }

    private void HandleShoot(InputAction.CallbackContext ctx)
    {
        if (!Object.HasInputAuthority || !_gun.CanShoot)
            return;


        StartCoroutine(_gun.ShootColdDown());

        if (_gun.GetBullet(shootPoint))
            return;
        
        Rpc_SpawnBullet(shootPoint.position, shootPoint.rotation);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsHostPlayer)]
    private void Rpc_SpawnBullet(Vector3 position, Quaternion rotation)
    {
        NetworkObject bulletObject = Runner.Spawn(bulletPrefab, position, rotation, Object.InputAuthority);
        Bullet bullet = bulletObject.GetComponent<Bullet>();
        bullet.SetPlayer(this);
        _gun.NewBulletStarted(bullet);
    }

    public bool CanReciveDamage()
    {
        return _canReciveDamage;
    }

    public bool Damage(float damage)
    {
        if(!_canReciveDamage) return false;

        _currentLife -= damage;

        if(_currentLife <= 0)
        {
            Debug.Log("Player is death");
            StartCoroutine(Die());
        }

        return _currentLife <= 0;
    }

    public void Respawn(Transform positionToRespawn)
    {
        if (_collider != null) _collider.enabled = true;
        _currentLife = _maxLife;
        _view.gameObject.SetActive(true);
        transform.position = positionToRespawn.position;
        _canReciveDamage = true;
    }

    private IEnumerator Die()
    {
        if (_collider != null) _collider.enabled = false;

        _view.gameObject.SetActive(false);
        _canReciveDamage = false;
        OnDeath?.Invoke();

        yield return new WaitForSeconds(_respawnColdDown);

        Transform respawnPos = NetworkManager.Instance.GetRandomSpawnPosition();
        Respawn(respawnPos);
    }

    [ContextMenu("Kill")]
    private void Kill()
    {
        StartCoroutine(Die());
    }

    private void SetCameraLimits()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float camHeight = cam.orthographicSize * 2f;
        float camWidth = camHeight * cam.aspect;

        Vector2 camCenter = cam.transform.position;

        _topLeftLimit = new Vector3(
            camCenter.x - camWidth / 2f,
            camCenter.y + camHeight / 2f,
            0f
        );

        _bottomRightLimit = new Vector3(
            camCenter.x + camWidth / 2f,
            camCenter.y - camHeight / 2f,
            0f
        );
    }
}