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
    private Dictionary<ulong, float> latencies = new Dictionary<ulong, float>();
    // private Dictionary<ulong, float> packetLosses = new Dictionary<ulong, float>();
    private Dictionary<ulong, float> jitters = new Dictionary<ulong, float>();
    private Dictionary<ulong, float> previousLatencies = new Dictionary<ulong, float>();
    // private Dictionary<ulong, int> sentPackets = new Dictionary<ulong, int>();
    // private Dictionary<ulong, int> receivedPackets = new Dictionary<ulong, int>();
    private Dictionary<ulong, List<float>> packetDelays = new Dictionary<ulong, List<float>>();
    private int moment;
    private string filePath;
    private string currentTimestamp;
    private const long MaxFileSize = 50 * 1024 * 1024; // 50 MB size limit
    ScoreboardManager scoreboardManager;

    [Serializable]
    public class ClientStats
    {
        public string latency;
        // public string packetLoss;
        public string jitter;
        // public int packetsSent;
        // public int packetsRecieved;
    }

    [Serializable]
    public class InputData
    {
        public int clientMoment;
        public int serverMoment;
        public string player;
        public string timestamp;
        public string selectedCube;
        public string correctClientCube;
        public string correctServerCube;
    }

    [Serializable]
    public class ScoresData
    {
        public string timestamp;
        public string playerWhoScored;
        public int player1Points;
        public int player2Points;
    }

    [Serializable]
    public class GameData{
        public string timestamp;
        public int currentGreenCube;
        public int lastGreenCube;
    }

    [Serializable]
    public class NetworkStats
    {
        public Dictionary<string, Dictionary<string, ClientStats>> pings = new Dictionary<string, Dictionary<string, ClientStats>>();
        public List<InputData> inputs = new List<InputData>();
        public List<ScoresData> scores = new List<ScoresData>();
        public List<GameData> cubes = new List<GameData>();
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
            StartCoroutine(GenerateStats());
        }
    }

    private void InitializeDicts(){
        // Initialize dictionaries for each connected client
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            moment = 0;
            latencies[clientId] = 0f;
            // packetLosses[clientId] = 0f;
            jitters[clientId] = 0f;
            previousLatencies[clientId] = 0f;
            // sentPackets[clientId] = 0;
            // receivedPackets[clientId] = -1;
            packetDelays[clientId] = new List<float>();
        }
    }

    private IEnumerator GenerateStats()
    {
        while (true)
        {
            // Check if there are connected clients
            if (NetworkManager.ConnectedClients.Count > 0){
                // Each connected client will answer this, so we are sending as many packets/calls as clients and not 1 packet
                SendPingClientRpc();

                foreach (var clientId in NetworkManager.ConnectedClients.Keys)
                {
                    // sentPackets[clientId]++;
                    latencies[clientId] = NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(clientId);
                    packetDelays[clientId].Add(latencies[clientId]);
                    CalculateJitter(clientId);

                    // Store calculated values
                    string clientKey = $"Player_{clientId+1}";
                    currentTimestamp = DateTime.UtcNow.ToString("o");

                    networkStats.pings[currentTimestamp] = new Dictionary<string, ClientStats>();
                    networkStats.pings[currentTimestamp][clientKey] = new ClientStats();

                    var clientStats = networkStats.pings[currentTimestamp][clientKey];
                    clientStats.latency = $"{latencies[clientId]}ms";
                    clientStats.jitter = $"{jitters[clientId]}ms";
                }

                // Write in JSON file
                UpdateStatsFile();
                
                yield return new WaitForSeconds(1/3); // Wait 33ms for next call
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

        UnityEngine.Debug.Log($"{(NetworkManager.LocalTime - NetworkManager.ServerTime).TimeAsFloat}");
        // receivedPackets[clientId]++;
        // CalculatePacketLoss(clientId);

        // clientStats.packetLoss = $"{packetLosses[clientId]}%";
        // clientStats.score = scoreboardManager.getScores()[clientId];
        // clientStats.packetsSent = sentPackets[clientId];
        // clientStats.packetsRecieved = receivedPackets[clientId];
    }

    public void AddClientData(InputData clientData){
        networkStats.inputs.Add(clientData);
    }

    public void AddScoresData(ulong clientId){
        ScoresData scoresData = new ScoresData {
            timestamp = DateTime.UtcNow.ToString("o"),
            playerWhoScored = $"Player_{clientId+1}",
            player1Points = scoreboardManager.getScores()[0],
            player2Points = scoreboardManager.getScores()[1],
        };
        networkStats.scores.Add(scoresData);
    }

    public void AddGameData(GameData gameData){
        networkStats.cubes.Add(gameData);
    }

    // private void CalculatePacketLoss(ulong clientId)
    // {
    //     if (sentPackets[clientId] > 0)
    //     {
    //         packetLosses[clientId] = (float)(sentPackets[clientId] - receivedPackets[clientId]) / sentPackets[clientId] * 100;
    //     }
    // }

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

    public void BackToMainMenu(){
        GameObject.Find("Cube Spawner").GetComponent<CubeSpawner>().BackToMainMenu();
    }

    public int SetMoment(int newMoment){
        moment = newMoment;
        return moment;
    }

    public int GetMoment(){
        return moment;
    }
}
