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
    public NetworkVariable<float> latency = new NetworkVariable<float>(0f);
    public NetworkVariable<float> packetLoss = new NetworkVariable<float>(0f);
    public NetworkVariable<float> jitter = new NetworkVariable<float>(0f);
    private Stopwatch stopwatch;
    private int sentPackets;
    private int receivedPackets;
    private List<float> packetDelays = new List<float>();
    private string filePath;
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB size limit


    [Serializable]
    public class ClientStats
    {
        public string latency;
        public string packetLoss;
        public string jitter;
        public int score;
        public List<InputData> inputs = new List<InputData>();
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
    }

    public override void OnNetworkSpawn()
    {
        optionsMenu.SetActive(true);
        latency.Value = 0;
        packetLoss.Value = 0;
        jitter.Value = 0;
        latency.OnValueChanged += OnLatencyChanged;
        packetLoss.OnValueChanged += OnPacketLossChanged;
        jitter.OnValueChanged += OnJitterChanged;

        if (IsServer || IsHost){
            stopwatch = new Stopwatch();
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
            CalculatePacketLoss();
            CalculateAvgJitter();
            UpdateNetworkStatsText();
            yield return new WaitForSeconds(1/90); // Adjust the interval as needed
        }
    }

    [ClientRpc]
    void SendPingClientRpc(ClientRpcParams rpcParams = default) { 
        SendPongServerRpc(); 
    }

    [ServerRpc]
    void SendPingServerRpc(ServerRpcParams rpcParams = default)
    {
        stopwatch.Start(); 
        SendPingClientRpc();
        sentPackets++;
    }

    [ServerRpc]
    void SendPongServerRpc(ServerRpcParams rpcParams = default)
    {
        // Calculate Latency, packet loss and jitter
        stopwatch.Stop();
        latency.Value = stopwatch.ElapsedMilliseconds;
        receivedPackets++;
        packetDelays.Add(latency.Value);
        stopwatch.Reset();

        // Store calculated values
        string clientKey = $"Client_{rpcParams.Receive.SenderClientId}";
        string currentTimestamp = DateTime.UtcNow.ToString("o");

        if (!networkStats.serverTimestamps.ContainsKey(currentTimestamp))
        {
            networkStats.serverTimestamps[currentTimestamp] = new Dictionary<string, ClientStats>();
        }

        if (!networkStats.serverTimestamps[currentTimestamp].ContainsKey(clientKey))
        {
            networkStats.serverTimestamps[currentTimestamp][clientKey] = new ClientStats();
        }

        var clientStats = networkStats.serverTimestamps[currentTimestamp][clientKey];
        clientStats.latency = $"{latency.Value}ms";
        clientStats.packetLoss = $"{packetLoss.Value}%";
        clientStats.jitter = $"{jitter.Value}ms";
        clientStats.score = 0; // Replace with actual score value

        // Example input data
        clientStats.inputs.Add(new InputData
        {
            timestamp = currentTimestamp,
            selectedCube = -1 // Replace with actual selected cube value
        });

        // Write in JSON file
        UpdateStatsFile();
    }

    private void CalculatePacketLoss()
    {
        if (sentPackets > 0)
        {
            packetLoss.Value = (float)(sentPackets - receivedPackets) / sentPackets * 100;
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
            jitter.Value = jitterSum / (packetDelays.Count - 1);
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
        UnityEngine.Debug.Log("The max size limit of 5MB has been reached for file NetworkStats.json");
    }

    private void CreateFile()
    {
        networkStats = new NetworkStats();
        File.WriteAllText(filePath, JsonConvert.SerializeObject(networkStats, Formatting.Indented));
    }

    private void OnLatencyChanged(float oldValue, float newValue)
    {
        latencyText.text = "Latency: " + newValue.ToString() + "ms";
    }

    private void OnPacketLossChanged(float oldValue, float newValue)
    {
        packetLossText.text = "Packet Loss: " + newValue.ToString() + "%";
    }

    private void OnJitterChanged(float oldValue, float newValue)
    {
        jitterText.text = "Jitter: " + newValue.ToString() + "ms";
    }

    private void UpdateNetworkStatsText()
    {
        latencyText.text = "Latency: " + latency.Value.ToString() + "ms";
        packetLossText.text = "Packet Loss: " + packetLoss.Value.ToString() + "%";
        jitterText.text = "Jitter: " + jitter.Value.ToString() + "ms";
        UpdateNetworkStatsTextClientRpc();
    }

    [ClientRpc]
    private void UpdateNetworkStatsTextClientRpc()
    {
        latencyText.text = "Latency: " + latency.Value.ToString() + "ms";
        packetLossText.text = "Packet Loss: " + packetLoss.Value.ToString() + "%";
        jitterText.text = "Jitter: " + jitter.Value.ToString() + "ms";
    }

    public void BackToMainMenu()
    {
        optionsMenu.SetActive(false);
        NetworkManager.Singleton.Shutdown();
    }
}
