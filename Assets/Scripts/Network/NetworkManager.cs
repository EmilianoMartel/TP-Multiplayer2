using Fusion.Sockets;
using Fusion;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetworkManager : MonoBehaviourSingleton<NetworkManager>, INetworkRunnerCallbacks
{
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private NetworkPrefabRef playerPrefab;
    [SerializeField] private Transform[] spawnPositions;

    private readonly Dictionary<PlayerRef, NetworkObject> spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();
    private NetworkRunner networkRunner;
    private Camera _camera;

    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<string> OnNewPlayerJoined;
    public event Action<string> OnJoinedPlayerLeft;

   public NetworkPlayerController LocalPlayer { get; set; }

    async void Start()
    {
        _camera = Camera.main;
        moveAction.action.Enable();
        bool sessionStarted = await StartGameSession();

        if (!sessionStarted)
            Debug.LogError("Could not start game session!");
    }

    void OnApplicationQuit()
    {
        Shutdown();
    }

    private async Task<bool> StartGameSession()
    {
        GameObject networkRunnerObject = new GameObject(typeof(NetworkRunner).Name, typeof(NetworkRunner));

        networkRunner = networkRunnerObject.GetComponent<NetworkRunner>();
        networkRunner.AddCallbacks(this);

        StartGameArgs startGameArgs = new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient,
            SceneManager = networkRunner.gameObject.AddComponent<NetworkSceneManagerDefault>(),
            PlayerCount = spawnPositions.Length
        };

        Task<StartGameResult> startTask = networkRunner.StartGame(startGameArgs);
        await startTask;

        return startTask.Result.Ok;
    }

    private void Shutdown()
    {
        if (networkRunner)
            networkRunner.Shutdown();
    }

    private void SpawnNewPlayer(NetworkRunner runner, PlayerRef player)
    {
        Vector3 spawnPosition = spawnPositions[spawnedPlayers.Count].position;
        NetworkObject networkPlayerObject = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);

        spawnedPlayers.Add(player, networkPlayerObject);
    }

    private void DespawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        if (spawnedPlayers.ContainsKey(player))
        {
            runner.Despawn(spawnedPlayers[player]);
            spawnedPlayers.Remove(player);
        }
    }

    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner)
    {
        if (networkRunner.IsClient)
            OnConnected?.Invoke();
    }

    void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        if (networkRunner.IsClient)
            Shutdown();
    }

    void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        if (shutdownReason == ShutdownReason.GameNotFound)
            return;

        if (networkRunner.IsServer)
            spawnedPlayers.Clear();

        networkRunner = null;

        OnDisconnected?.Invoke();
    }

    void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
            SpawnNewPlayer(runner, player);

        OnNewPlayerJoined?.Invoke("Player_" + player.PlayerId);
    }

    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            DespawnPlayer(runner, player);

            if (spawnedPlayers.Count == 0)
                Shutdown();
        }

        OnJoinedPlayerLeft?.Invoke("Player_" + player.PlayerId);
    }

    void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (!LocalPlayer)
            return;

        NetworkInputData networkInput = new NetworkInputData();

        Vector2 moveInput = moveAction.action.ReadValue<Vector2>();

        networkInput.Direction = moveInput.normalized;

        Vector2 mouseScreenPos = lookAction.action.ReadValue<Vector2>();

        Vector3 mouseWorldPos = _camera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 0f));

        Vector2 playerPos = LocalPlayer.transform.position;
        Vector2 lookDir = (mouseWorldPos - (Vector3)playerPos).normalized;

        networkInput.LookDirection = lookDir;

        if (moveInput.y > 0f)
            networkInput.AddInput(NetworkInputType.MoveForward);
        else if (moveInput.y < 0f)
            networkInput.AddInput(NetworkInputType.MoveBackwards);
        if (moveInput.x < 0f)
            networkInput.AddInput(NetworkInputType.MoveLeft);
        else if (moveInput.x > 0f)
            networkInput.AddInput(NetworkInputType.MoveRight);


        input.Set(networkInput);
    }

    void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
}