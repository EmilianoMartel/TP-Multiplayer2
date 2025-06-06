using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Fusion;
using UnityEngine.InputSystem;
using System;

public class PlayerStatsUI : MonoBehaviour
{
    [SerializeField] private InputActionReference _tabAction;
    [SerializeField] private Transform _playerStatsContainer;
    [SerializeField] private GameObject _playerStatsItemPrefab;

    private Dictionary<PlayerRef, PlayerStatsUIItem> _playerUIItems = new();

    private void OnEnable()
    {
        _tabAction.action.performed += HandleOpenPanel;
        PlayerStats.OnKillsUpdated += UpdateKillCount;
        NetworkManager.Instance.OnNewPlayerJoined += AddPlayerUI;
        NetworkManager.Instance.OnJoinedPlayerLeft += RemovePlayerUI;
    }

    private void OnDisable()
    {
        _tabAction.action.performed -= HandleOpenPanel;
        PlayerStats.OnKillsUpdated -= UpdateKillCount;
        NetworkManager.Instance.OnNewPlayerJoined -= AddPlayerUI;
        NetworkManager.Instance.OnJoinedPlayerLeft -= RemovePlayerUI;
    }

    private void AddPlayerUI(string playerName)
    {
        PlayerRef playerRef = FindPlayerRefByName(playerName);
        if (playerRef == null || _playerUIItems.ContainsKey(playerRef)) return;

        GameObject item = Instantiate(_playerStatsItemPrefab, _playerStatsContainer);
        PlayerStatsUIItem uiItem = item.GetComponent<PlayerStatsUIItem>();
        uiItem.SetPlayerName(playerName);
        uiItem.SetKills(0);

        _playerUIItems[playerRef] = uiItem;
    }

    private void RemovePlayerUI(string playerName)
    {
        PlayerRef playerRef = FindPlayerRefByName(playerName);
        if (playerRef == null || !_playerUIItems.ContainsKey(playerRef)) return;

        Destroy(_playerUIItems[playerRef].gameObject);
        _playerUIItems.Remove(playerRef);
    }

    private void UpdateKillCount(PlayerRef playerRef, int kills)
    {
        if (_playerUIItems.TryGetValue(playerRef, out var uiItem))
        {
            uiItem.SetKills(kills);
        }
    }

    private void HandleOpenPanel(InputAction.CallbackContext obj)
    {
        _playerStatsContainer.gameObject.SetActive(!_playerStatsContainer.gameObject.activeSelf);
    }

    private PlayerRef FindPlayerRefByName(string name)
    {
        foreach (var kvp in NetworkManager.Instance.Players)
        {
            if ($"Player_{kvp.Key.PlayerId}" == name)
                return kvp.Key;
        }

        return default;
    }
}
