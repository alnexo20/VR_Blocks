using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using Newtonsoft.Json;

public class OptionsNetworkStats : NetworkBehaviour
{
    public GameObject optionsMenu;
    public TextMeshProUGUI latencyText;
    public TextMeshProUGUI packetLossText;
    public TextMeshProUGUI jitterText;
    private Dictionary<ulong, float> latencies = new Dictionary<ulong, float>();
    private Dictionary<ulong, float> packetLosses = new Dictionary<ulong, float>();
    private Dictionary<ulong, float> jitters = new Dictionary<ulong, float>();
    private Dictionary<ulong, float> previousLatencies = new Dictionary<ulong, float>();
    private Dictionary<ulong, int> sentPackets = new Dictionary<ulong, int>();
    private Dictionary<ulong, int> receivedPackets = new Dictionary<ulong, int>();
    private Dictionary<ulong, List<float>> packetDelays = new Dictionary<ulong, List<float>>();
    private Stopwatch stopwatch;
    private string filePath;
    private const long MaxFileSize = 512 * 1024 * 1024; // 500 MB size limit
    ScoreboardManager scoreboardManager;
    CubeSpawner cubeSpawner;


    [Serializable]
    public class ClientStats
    {
        public string latency;
        public string packetLoss;
        public string jitter;
        public int score;
        // public List<InputData> inputs = new List<InputData>();
    }

    [Serializable]
    public class InputData
    {
        public string timestamp;
        public int selectedCube;
    }

    [Serializable]
    public class NetworkStats
    {
        public Dictionary<string, Dictionary<string, ClientStats>> serverTimestamps = new Dictionary<string, Dictionary<string, ClientStats>>();
    }

    private NetworkStats networkStats;

    // Start is called before the first frame update
    void Start()
    {
        networkStats = new NetworkStats();
        scoreboardManager = GameObject.FindGameObjectWithTag("Scoreboard").GetComponent<ScoreboardManager>();
    }

    public override void OnNetworkSpawn()
    {
        optionsMenu.SetActive(true);

        if (IsServer){
            stopwatch = new Stopwatch();
            networkStats = new NetworkStats();
            filePath = "./NetworkStats.json"; // Path for the JSON file
            CreateFile();

            // Initialize dictionaries for each connected client
            foreach (var clientId in NetworkManager.ConnectedClients.Keys)
            {
                latencies[clientId] = 0f;
                packetLosses[clientId] = 0f;
                jitters[clientId] = 0f;
                previousLatencies[clientId] = 0f;
                sentPackets[clientId] = 0;
                receivedPackets[clientId] = 0;
                packetDelays[clientId] = new List<float>();
            }

            // Start sending pings and updating network stats
            StartCoroutine(PingRoutine());
        }
    }

    private IEnumerator PingRoutine()
    {
        while (true)
        {
            stopwatch.Start(); 

            // Each connected client will answer this, so we are sending as many packets/calls as clients and not 1 packet
            SendPingClientRpc();
            foreach (var clientId in NetworkManager.ConnectedClients.Keys)
            {
                sentPackets[clientId]++;
            }

            yield return new WaitForSeconds(1/2); // Adjust the interval as needed
        }
    }

    [ClientRpc]
    void SendPingClientRpc(ClientRpcParams rpcParams = default) { 
        SendPongServerRpc(); 
    }

    [ServerRpc(RequireOwnership = false)]
    void SendPongServerRpc(ServerRpcParams rpcParams = default)
    {
        var clientId = rpcParams.Receive.SenderClientId;

        // Calculate Latency, packet loss and jitter
        stopwatch.Stop();
        latencies[clientId] = stopwatch.ElapsedMilliseconds;
        receivedPackets[clientId]++;
        packetDelays[clientId].Add(latencies[clientId]);
        CalculatePacketLoss(clientId);
        CalculateJitter(clientId);
        stopwatch.Reset();

        // Update UI text in game
        UpdateNetworkStatsTextClientRpc(clientId);

        // Store calculated values
        string clientKey = $"Client_{clientId}";
        string currentTimestamp = DateTime.UtcNow.ToString("o");

        networkStats.serverTimestamps[currentTimestamp] = new Dictionary<string, ClientStats>();
        networkStats.serverTimestamps[currentTimestamp][clientKey] = new ClientStats();

        var clientStats = networkStats.serverTimestamps[currentTimestamp][clientKey];
        clientStats.latency = $"{latencies[clientId]}ms";
        clientStats.packetLoss = $"{packetLosses[clientId]}%";
        clientStats.jitter = $"{jitters[clientId]}ms";
        clientStats.score = scoreboardManager.getScores()[clientId];

        // // Example input data
        // clientStats.inputs.Add(new InputData
        // {
        //     timestamp = currentTimestamp,
        //     selectedCube = -1 // Replace with actual selected cube value
        // });

        // Write in JSON file
        UpdateStatsFile();
    }

    private void CalculatePacketLoss(ulong clientId)
    {
        if (sentPackets[clientId] > 0)
        {
            packetLosses[clientId] = (float)(sentPackets[clientId] - receivedPackets[clientId]) / sentPackets[clientId] * 100;
        }
    }

    private void CalculateJitter(ulong clientId)
    {
        if (previousLatencies[clientId] > 0)
        {
            jitters[clientId] = Mathf.Abs(latencies[clientId] - previousLatencies[clientId]);
        }
        previousLatencies[clientId] = latencies[clientId];
    }

    private void UpdateStatsFile()
    {
        if (filePath == ""){
            throw new FileNotFoundException("filepath for networkStats is: "+filePath);
        }

        if (new FileInfo(filePath).Length > MaxFileSize)
        {
            TruncateFile();
        }else{
            File.WriteAllText(filePath, JsonConvert.SerializeObject(networkStats, Formatting.Indented));
        }       
    }

    private void TruncateFile()
    {
        UnityEngine.Debug.Log("The max size limit of 500 MB has been reached for file NetworkStats.json");
    }

    private void CreateFile()
    {
        File.WriteAllText(filePath, JsonConvert.SerializeObject(networkStats, Formatting.Indented));
    }

    [ClientRpc]
    private void UpdateNetworkStatsTextClientRpc(ulong clientId, ClientRpcParams clientRpcParams = default)
    {
        if (NetworkManager.LocalClientId == clientId){
            latencyText.text = "Latency: " + latencies[clientId].ToString() + "ms";
            packetLossText.text = "Packet Loss: " + packetLosses[clientId].ToString() + "%";
            jitterText.text = "Jitter: " + jitters[clientId].ToString() + "ms";
        }
    }

    public void BackToMainMenu(){
        GameObject.Find("Cube Spawner").GetComponent<CubeSpawner>().BackToMainMenu();
    }
}
