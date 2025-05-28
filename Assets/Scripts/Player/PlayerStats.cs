using Fusion;
using System;
using UnityEngine;

public class PlayerStats : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnKillsChanged))]
    public int Kills { get; set; }

    public static event Action<PlayerRef, int> OnKillsUpdated;

    private void OnKillsChanged()
    {
        OnKillsUpdated?.Invoke(Object.InputAuthority, Kills);
    }

    public void AddKill()
    {
        if (Object.HasStateAuthority)
            Kills++;
    }
}