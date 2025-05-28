using System;
using UnityEngine;
using Fusion;

[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(Rigidbody2D))]
public class NetworkPlayerController : NetworkBehaviour
{
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D _rigidbody2D;
    private bool isMoving;

    public event Action OnMovementStarted;
    public event Action OnMovementStopped;

    private void Awake()
    {
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
}