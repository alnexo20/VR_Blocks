using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Networking.Transport;

public class NetworkSpawnPlayers : MonoBehaviour
{
    public GameObject playerPrefab;

    void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    void OnClientConnected(ulong clientId)
    {
        Vector3 spawnPosition = new Vector3(NetworkManager.Singleton.ConnectedClients.Count, 0, 0);
        GameObject player = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
    }
}


    

