using UnityEngine;
using Unity.Netcode;

public class CubeSpawner : NetworkBehaviour
{
    public GameObject cubesPrefab; // Reference to the prefab containing all 9 cubes
    public Vector3 spawnPosition = Vector3.zero; // Position to spawn the prefab
    public GameObject scoreboard;
    public Vector3 spawnPositionScoreBoard = Vector3.zero;
    private int minPlayers = 2;

    public override void OnNetworkSpawn()
    {
        if (IsServer || IsHost)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId) { 
        if (NetworkManager.Singleton.ConnectedClients.Count >= minPlayers) { 
            SpawnCubes(); 
        } 
    }

    void SpawnCubes()
    {
        GameObject cubes = Instantiate(cubesPrefab, spawnPosition, Quaternion.identity);
        cubes.GetComponent<NetworkObject>().Spawn();
        cubes.SetActive(true);
        cubesPrefab.SetActive(true);
        GameObject newscoreboard = Instantiate(scoreboard, spawnPositionScoreBoard, Quaternion.identity);
        newscoreboard.GetComponent<NetworkObject>().Spawn();
        scoreboard.SetActive(true);
    }

    public override void OnDestroy() { 
        if (NetworkManager.Singleton != null) { 
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected; 
        } 
    }
}
