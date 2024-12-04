using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Netcode;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class CubeManager : NetworkBehaviour
{
    public GameObject[] cubes; // Array to hold the 9 cubes
    public float changeInterval = 5f; // Interval between color changes
    private float changeTimer;
    private Color originalColor;
    private NetworkVariable<bool> hasScored = new NetworkVariable<bool>(false); // Flag to track if a player has scored
    public ScoreboardManager scoreboardManager;

    void Start()
    {
        changeTimer = changeInterval;
        if (cubes.Length > 0)
        {
            originalColor = cubes[0].GetComponent<Renderer>().material.color;
        }
        hasScored.Value = false;
        this.gameObject.SetActive(true);
        
        // Find the ScoreboardManager instance at runtime 
        scoreboardManager = FindObjectOfType<ScoreboardManager>(); 
        if (scoreboardManager == null) { 
            Debug.LogError("ScoreboardManager not found!"); 
        }
    }

    void Update()
    {
        if (!IsServer && !IsHost) return;
        
        changeTimer -= Time.deltaTime;

        if (changeTimer <= 0)
        {
            ResetCubeColors();
            ChangeCubeColorToGreen();
            changeTimer = changeInterval;
            hasScored.Value = false; // Reset the flag when the color changes
        }       
    }

    void ResetCubeColors()
    {
        foreach (GameObject cube in cubes)
        {
            cube.GetComponent<Renderer>().material.color = originalColor;
        }
    }

    void ChangeCubeColorToGreen()
    {
        int randomIndex = Random.Range(0, cubes.Length);
        Renderer cubeRenderer = cubes[randomIndex].GetComponent<Renderer>();
        cubeRenderer.material.color = Color.green;
        ChangeCubeColorClientRpc(randomIndex);
    }

    [ClientRpc] 
    void ChangeCubeColorClientRpc(int index) { 
        if (!IsServer && !IsHost) { 
            ResetCubeColors(); 
            Renderer cubeRenderer = cubes[index].GetComponent<Renderer>(); 
            cubeRenderer.material.color = Color.green; 
        } 
    }

    public void CheckCubeSelection(GameObject selectedCube)
    {
        // Check if a point has been scored
        if (selectedCube.GetComponent<Renderer>().material.color == Color.green && !hasScored.Value){
            // if host or server scored a point then update
            if (IsHost || IsServer)
            {
                Debug.Log("Player 1 point");
                scoreboardManager.UpdatePlayerScore(1);
                hasScored.Value = true; // Set the flag to true after scoring
            }
            else
            {
                //if client scored call server to update score as this is not server autoritative
                RequestScoreUpdateServerRpc();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestScoreUpdateServerRpc(){
        Debug.Log("Player 2 point");
        scoreboardManager.UpdatePlayerScore(2);
        hasScored.Value = true; // Set the flag to true after scoring
    }
}
