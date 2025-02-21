using System;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using UnityEngine.UI;

public class Player : NetworkBehaviour
{
    public NetworkVariable<int> Tokens = new NetworkVariable<int>(100); // Default 100 tokens
    public string Username;
  
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Initialize your player data
            Username = "Player" + OwnerClientId;
          
        }
    }

    public void DeductTokens(int amount)
    {
        if (IsOwner && Tokens.Value >= amount)
        {
            Tokens.Value -= amount;
        }
    }

    public void AddTokens(int amount)
    {
        if (IsOwner)
        {
            Tokens.Value += amount;
        }
    }
}
