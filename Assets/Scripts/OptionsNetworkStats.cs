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
    private string filePath;
    private string currentTimestamp;
    private const long MaxFileSize = 50 * 1024 * 1024; // 50 MB size limit
    ScoreboardManager scoreboardManager;

    [Serializable]
    public class ClientStats
    {
        public string latency;
        public string packetLoss;
        public string jitter;
        public int score;
        public int packetsSent;
        public int packetsRecieved;
        public List<InputData> inputs = new List<InputData>();
    }

    [Serializable]
    public class InputData
    {
        public int id;
        public string timestamp;
        public string selectedCube;
        public string correctClientCube;
        public string correctServerCube;
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
            networkStats = new NetworkStats();
            filePath = "./NetworkStats.json"; // Path for the JSON file
            CreateFile();

            InitializeDicts();

            // Start sending pings and updating network stats
            StartCoroutine(PingRoutine());
        }
    }

    private void InitializeDicts(){
        // Initialize dictionaries for each connected client
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            latencies[clientId] = 0f;
            packetLosses[clientId] = 0f;
            jitters[clientId] = 0f;
            previousLatencies[clientId] = 0f;
            sentPackets[clientId] = 0;
            receivedPackets[clientId] = -1;
            packetDelays[clientId] = new List<float>();
        }
    }

    private IEnumerator PingRoutine()
    {
        while (true)
        {
            // Check if there are connected clients
            if (NetworkManager.ConnectedClients.Count > 0){
                // Each connected client will answer this, so we are sending as many packets/calls as clients and not 1 packet
                SendPingClientRpc();
                foreach (var clientId in NetworkManager.ConnectedClients.Keys)
                {
                    sentPackets[clientId]++;
                }
                yield return new WaitForSeconds(1/3); // Adjust the interval as needed
            }
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

        latencies[clientId] = NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(clientId);
        UnityEngine.Debug.Log($"{(NetworkManager.LocalTime - NetworkManager.ServerTime).TimeAsFloat}");
        receivedPackets[clientId]++;
        packetDelays[clientId].Add(latencies[clientId]);
        CalculatePacketLoss(clientId);
        CalculateJitter(clientId);

        // Update UI text in game
        UpdateNetworkStatsTextClientRpc(clientId);

        // Store calculated values
        string clientKey = $"Client_{clientId}";
        currentTimestamp = DateTime.UtcNow.ToString("o");

        networkStats.serverTimestamps[currentTimestamp] = new Dictionary<string, ClientStats>();
        networkStats.serverTimestamps[currentTimestamp][clientKey] = new ClientStats();

        var clientStats = networkStats.serverTimestamps[currentTimestamp][clientKey];
        clientStats.latency = $"{latencies[clientId]}ms";
        clientStats.packetLoss = $"{packetLosses[clientId]}%";
        clientStats.jitter = $"{jitters[clientId]}ms";
        clientStats.score = scoreboardManager.getScores()[clientId];
        clientStats.packetsSent = sentPackets[clientId];
        clientStats.packetsRecieved = receivedPackets[clientId];

        // Write in JSON file
        UpdateStatsFile();
    }

    public void AddClientData(InputData clientData, ulong clientId){
        string clientKey = $"Client_{clientId}";
        networkStats.serverTimestamps[currentTimestamp][clientKey].inputs.Add(clientData);
    }

    public void FinalScore(){
        currentTimestamp = DateTime.UtcNow.ToString("o");
        networkStats.serverTimestamps[currentTimestamp]["Client_0"].score = scoreboardManager.getScores()[0];
        networkStats.serverTimestamps[currentTimestamp]["Client_1"].score = scoreboardManager.getScores()[1];
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
        UnityEngine.Debug.Log("The max size limit of 50 MB has been reached for file NetworkStats.json");
    }

    private void CreateFile()
    {
        File.WriteAllText(filePath, JsonConvert.SerializeObject(networkStats, Formatting.Indented));
    }

    [ClientRpc]
    private void UpdateNetworkStatsTextClientRpc(ulong clientId, ClientRpcParams clientRpcParams = default)
    {
        if (NetworkManager.LocalClientId == clientId){
            // latencyText.text = "Latency: " + latencies[clientId].ToString() + "ms";
            // packetLossText.text = "Packet Loss: " + packetLosses[clientId].ToString() + "%";
            // jitterText.text = "Jitter: " + jitters[clientId].ToString() + "ms";
        }
    }

    public void BackToMainMenu(){
        GameObject.Find("Cube Spawner").GetComponent<CubeSpawner>().BackToMainMenu();
    }
}
