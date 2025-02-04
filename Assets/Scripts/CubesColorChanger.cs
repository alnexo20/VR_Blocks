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
    private int lastSelectedCubeIndex = -1;
    private int currentGreenCube = -1;

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
            addOnePoint();
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
        // This implementation is not really efficient but probability of repeating same number more than 5 times is <<<
        do{
            currentGreenCube = Random.Range(0, cubes.Length);
        } while(currentGreenCube == lastSelectedCubeIndex);
        Renderer cubeRenderer = cubes[currentGreenCube].GetComponent<Renderer>();
        cubeRenderer.material.color = Color.green;
        ChangeCubeColorClientRpc(currentGreenCube);
    }

    [ClientRpc] 
    void ChangeCubeColorClientRpc(int index) { 
        if (!IsServer && !IsHost) { 
            ResetCubeColors(); 
            Renderer cubeRenderer = cubes[index].GetComponent<Renderer>(); 
            cubeRenderer.material.color = Color.green; 
        } 
    }

    void addOnePoint(){
        hasScored.Value = true; // Set the flag to true after scoring
        ResetCubeColors();
        ChangeCubeColorToGreen();
        changeTimer = changeInterval;
        hasScored.Value = false; // Reset the flag when the color changes
    }

    public void CheckCubeSelection(GameObject selectedCube)
    {
        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        //Server and other players connected cannot play
        if (IsServer || localClientId >= 2) return;

        //if client scored call server to update score as this is not server autoritative
        RequestScoreUpdateServerRpc(selectedCube.name, cubes[currentGreenCube].name);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestScoreUpdateServerRpc(string selectedCube, string greenCube, ServerRpcParams rpcParams = default){
        // Check if a point has been scored
        if (selectedCube == greenCube && selectedCube == cubes[currentGreenCube].name && !hasScored.Value){
            scoreboardManager.UpdatePlayerScore(rpcParams.Receive.SenderClientId);
            addOnePoint();
        }
    }
}
