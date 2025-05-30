using System;
using UnityEngine;
using Fusion;
using UnityEngine.InputSystem;
using System.Collections.Generic;

[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(Rigidbody2D))]
public class NetworkPlayerController : NetworkBehaviour
{
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private Transform view;

    [Header("Shoot")]
    [SerializeField] private InputActionReference shootAction;
    [SerializeField] private Transform shootPoint;
    [SerializeField] private float _shootColdDown = 1.5f;
    [SerializeField] private NetworkPrefabRef bulletPrefab;

    [Header("Parameters")]
    [SerializeField] private float maxLife = 10f;
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D _rigidbody2D;
    private bool isMoving;
    private float currentLife;

    private Gun _gun;

    public event Action OnMovementStarted;
    public event Action OnMovementStopped;

    private void OnEnable()
    {
        shootAction.action.performed += HandleShoot;
    }

    private void OnDisable()
    {
        shootAction.action.performed -= HandleShoot;
    }

    private void Awake()
    {
        _gun = new(_shootColdDown);
        currentLife = maxLife;
        _rigidbody2D = GetComponent<Rigidbody2D>();
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
            Vector2 newPosition = _rigidbody2D.position + moveDir * moveSpeed * Runner.DeltaTime;
            _rigidbody2D.MovePosition(newPosition);

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
        view.rotation = Quaternion.Euler(0f, 0f, rotationZ - 90f); //This magic number is the offset view
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

    public bool Damage(float damage)
    {
        currentLife -= damage;

        if(currentLife <= 0)
        {
            Debug.Log("Player is death");
        }

        return currentLife <= 0;
    }
}