using UnityEngine;
using Unity.Netcode;

public class CubeSpawner : NetworkBehaviour
{
    public GameObject cubesPrefab; // Reference to the prefab containing all 9 cubes
    private GameObject cubes; // Actual object spawned with network
    public Vector3 spawnPosition = Vector3.zero; // Position to spawn the prefab
    public GameObject scoreboardPrefab;
    private GameObject scoreboard;
    public Vector3 spawnPositionScoreBoard = Vector3.zero;
    private int minPlayers = 2;
    public GameObject WinnerMenu;
    private GameObject optionsMenu;

    void Start(){
        optionsMenu = GameObject.FindGameObjectWithTag("Options Menu").GetComponent<GameObject>();
    }

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
        cubes = Instantiate(cubesPrefab, spawnPosition, Quaternion.identity);
        cubes.GetComponent<NetworkObject>().Spawn();
        cubes.SetActive(true);
        cubesPrefab.SetActive(true);
        scoreboard = Instantiate(scoreboardPrefab, spawnPositionScoreBoard, Quaternion.identity);
        scoreboard.GetComponent<NetworkObject>().Spawn();
        scoreboard.SetActive(true);
    }

    public override void OnDestroy() { 
        if (NetworkManager.Singleton != null) { 
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected; 
        } 
    }

    public void WinnerScreen(){
        // Despawn cubes
        cubes.GetComponent<NetworkObject>().Despawn();
        cubes.SetActive(false);
        // Hide Scoreboard
        scoreboard.SetActive(false);
        // Hide options Menu
        optionsMenu.SetActive(false);
        // Show Winner Menu
        WinnerMenu.SetActive(true);
    }
}
