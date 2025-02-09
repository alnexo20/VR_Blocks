using UnityEngine;
using Unity.Netcode;
using System;

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
    public GameObject optionsMenuPrefab;
    private GameObject optionsMenu;
    public Vector3 spawnPositionOptionsMenu = Vector3.zero;

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
        if (cubesPrefab != null)
        {
            cubes = Instantiate(cubesPrefab, spawnPosition, Quaternion.identity);
            var networkObject = cubes.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn();
                networkObject.ChangeOwnership(NetworkManager.ServerClientId);
            }
            else
            {
                Debug.LogError("NetworkObject component not found on cubesPrefab");
            }
        }
        else
        {
            Debug.LogError("cubesPrefab is not assigned");
        }

        if (scoreboardPrefab != null)
        {
            scoreboard = Instantiate(scoreboardPrefab, spawnPositionScoreBoard, Quaternion.Euler(0,30,0));
            var networkObject = scoreboard.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn();
                networkObject.ChangeOwnership(NetworkManager.ServerClientId);
            }
            else
            {
                Debug.LogError("NetworkObject component not found on scoreboardPrefab");
            }
        }
        else
        {
            Debug.LogError("scoreboardPrefab is not assigned");
        }

        if (optionsMenuPrefab != null){
            optionsMenu = Instantiate(optionsMenuPrefab, spawnPositionOptionsMenu, Quaternion.Euler(0,-30,0));
            var networkObject = optionsMenu.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn();
                networkObject.ChangeOwnership(NetworkManager.ServerClientId);
            }
            else
            {
                Debug.LogError("NetworkObject component not found on optionsMenuPrefab");
            }
        }else
        {
            Debug.LogError("optionsMenuPrefab is not assigned");
        }
    }

    public override void OnDestroy() { 
        if (NetworkManager.Singleton != null) { 
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected; 
        } 
    }

    public void BackToMainMenu(){
        EndGame();
        GameObject.Find("Start Menu").GetComponent<GameStartMenu>().EnableMainMenu();
        BackToMainMenuClientRpc();
    }

    [ClientRpc]
    public void BackToMainMenuClientRpc(){
        GameObject.Find("Start Menu").GetComponent<GameStartMenu>().EnableMainMenu();
    }

    [ServerRpc(RequireOwnership = false)]
    private void EndGameServerRpc()
    {
        if (cubes != null)
        {
            var networkObject = cubes.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Despawn(true);
                Debug.Log("Cubes despawned.");
            }
            else
            {
                Debug.LogError("NetworkObject component not found on cubes");
            }
        }

        if (scoreboard != null)
        {
            var networkObject = scoreboard.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Despawn(true);
            }
        }

        if (optionsMenu != null)
        {
            var networkObject = optionsMenu.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Despawn(true);
            }
            
        }
        NetworkManager.Singleton.Shutdown();
    }

    private void EndGame()
    {
        // ServerRpc is not called if server or host is the one executing this function
        if (IsServer || IsHost){
            if (cubes != null)
            {
                var networkObject = cubes.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    networkObject.Despawn(true);
                }
                else
                {
                    Debug.LogError("NetworkObject component not found on cubes");
                }
            }

            if (scoreboard != null)
            {
                var networkObject = scoreboard.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    networkObject.Despawn(true);
                }
            }

            if (optionsMenu != null)
            {
                var networkObject = optionsMenu.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    networkObject.Despawn(true);
                }
                
            }
            NetworkManager.Singleton.Shutdown();
        }else{
            EndGameServerRpc();
        }
    }

    public void WinnerScreen(string WinnerText)
    {
        EndGame();
        WinnerScreenClientRpc(WinnerText);

        if (WinnerMenu != null)
        {
            WinnerMenu.SetActive(true);
            WinnerMenu.gameObject.GetComponentInParent<WinnerMenuManager>().setWinnerText(WinnerText);
        }
    }

    [ClientRpc]
    private void WinnerScreenClientRpc(string WinnerText)
    {
        if (WinnerMenu != null)
        {
            WinnerMenu.SetActive(true);
            WinnerMenu.gameObject.GetComponentInParent<WinnerMenuManager>().setWinnerText(WinnerText);
        }
    }


}
