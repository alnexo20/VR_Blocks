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
    private float latency = 0f;
    private float packetLoss = 0f;
    private float jitter = 0f;
    private Stopwatch stopwatch;
    private int sentPackets;
    private int receivedPackets;
    private List<float> packetDelays = new List<float>();
    private string filePath;
    private const long MaxFileSize = 512 * 1024 * 1024; // 500 MB size limit
    ScoreboardManager scoreboardManager;


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
        sentPackets = 0;
        receivedPackets = 0;
        networkStats = new NetworkStats();
        scoreboardManager = GameObject.FindGameObjectWithTag("Scoreboard").GetComponent<ScoreboardManager>();
    }

    public override void OnNetworkSpawn()
    {
        optionsMenu.SetActive(true);
        latency = 0;
        packetLoss = 0;
        jitter = 0;

        if (IsServer || IsHost){
            stopwatch = new Stopwatch();
            networkStats = new NetworkStats();
            filePath = "./NetworkStats.json"; // Path for the JSON file
            CreateFile();
            // Start sending pings and updating network stats
            StartCoroutine(PingRoutine());
        }
    }

    private IEnumerator PingRoutine()
    {
        while (true)
        {
            SendPingServerRpc();
            yield return new WaitForSeconds(1/2); // Adjust the interval as needed
        }
    }

    [ClientRpc]
    void SendPingClientRpc(ClientRpcParams rpcParams = default) { 
        SendPongServerRpc(); 
    }

    [ServerRpc(RequireOwnership = false)]
    void SendPingServerRpc(ServerRpcParams rpcParams = default)
    {
        stopwatch.Start(); 
        SendPingClientRpc();
        sentPackets++;
    }

    [ServerRpc(RequireOwnership = false)]
    void SendPongServerRpc(ServerRpcParams rpcParams = default)
    {
        // Calculate Latency, packet loss and jitter
        stopwatch.Stop();
        latency = stopwatch.ElapsedMilliseconds;
        receivedPackets++;
        packetDelays.Add(latency);
        CalculatePacketLoss();
        CalculateAvgJitter();
        stopwatch.Reset();

        // Update UI text in game
        UpdateNetworkStatsText();

        // Store calculated values
        string clientKey = $"Client_{rpcParams.Receive.SenderClientId}";
        string currentTimestamp = DateTime.UtcNow.ToString("o");

        networkStats.serverTimestamps[currentTimestamp] = new Dictionary<string, ClientStats>();
        networkStats.serverTimestamps[currentTimestamp][clientKey] = new ClientStats();

        var clientStats = networkStats.serverTimestamps[currentTimestamp][clientKey];
        clientStats.latency = $"{latency}ms";
        clientStats.packetLoss = $"{packetLoss}%";
        clientStats.jitter = $"{jitter}ms";
        clientStats.score = scoreboardManager.getScores()[rpcParams.Receive.SenderClientId];

        // // Example input data
        // clientStats.inputs.Add(new InputData
        // {
        //     timestamp = currentTimestamp,
        //     selectedCube = -1 // Replace with actual selected cube value
        // });

        // Write in JSON file
        UpdateStatsFile();
    }

    private void CalculatePacketLoss()
    {
        if (sentPackets > 0)
        {
            packetLoss = (float)(sentPackets - receivedPackets) / sentPackets * 100;
        }
    }

    private void CalculateAvgJitter()
    {
        if (packetDelays.Count > 1)
        {
            float jitterSum = 0f;
            for (int i = 1; i < packetDelays.Count; i++)
            {
                jitterSum += Mathf.Abs(packetDelays[i] - packetDelays[i - 1]);
            }
            jitter = jitterSum / (packetDelays.Count - 1);
        }
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
            File.AppendAllText(filePath, JsonConvert.SerializeObject(networkStats, Formatting.Indented));
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

    private void UpdateNetworkStatsText()
    {
        latencyText.text = "Latency: " + latency.ToString() + "ms";
        packetLossText.text = "Packet Loss: " + packetLoss.ToString() + "%";
        jitterText.text = "Jitter: " + jitter.ToString() + "ms";
    }
}
