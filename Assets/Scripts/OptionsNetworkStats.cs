using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class OptionsNetworkStats : NetworkBehaviour
{
    public TextMeshProUGUI latencyText;
    public TextMeshProUGUI packetLossText;
    public TextMeshProUGUI jitterText;
    public NetworkVariable<float> latency = new NetworkVariable<float>(0f);
    public NetworkVariable<int> packetLoss = new NetworkVariable<int>(0);
    public NetworkVariable<int> jitter = new NetworkVariable<int>(0);
    private Stopwatch stopwatch;
    private int sentPackets;
    private int receivedPackets;
    private Dictionary<ulong, string> clientFiles = new Dictionary<ulong, string>();

    // Start is called before the first frame update
    void Start()
    {
        this.gameObject.SetActive(true);
        stopwatch = new Stopwatch();
        sentPackets = 0;
        receivedPackets = 0;
        latency.Value = 0f;
        packetLoss.Value = 0;
        jitter.Value = 0;
    }

    void Update()
    {
        //UnityEngine.Debug.Log((NetworkManager.LocalTime - NetworkManager.ServerTime).TimeAsFloat); // tickrate must be positive error because it tries to do this before network is spawned
        SendPingClientRpc();
        UpdateNetworkStatsText();
    }

    [ClientRpc]
    void SendPingClientRpc(ClientRpcParams rpcParams = default) { 
        stopwatch.Start(); 
        SendPingServerRpc(); 
    }

    [ServerRpc]
    void SendPingServerRpc(ServerRpcParams rpcParams = default)
    {
        SendPongClientRpc();
    }

    [ClientRpc]
    void SendPongClientRpc(ClientRpcParams rpcParams = default)
    {
        stopwatch.Stop();
        float calculatedLatency = stopwatch.ElapsedMilliseconds;
        UnityEngine.Debug.Log($"Latency: {calculatedLatency}ms");
        stopwatch.Reset();
        UpdateClientFile(NetworkManager.LocalClientId, calculatedLatency);
    }

    private void UpdateClientFile(ulong clientId, float latencyValue)
    {
        if (clientFiles.TryGetValue(clientId, out string filePath))
        {
            File.AppendAllText(filePath, latencyValue.ToString() + "ms\n");
        }
    }

    public override void OnNetworkSpawn()
    {
        latency.Value = 0;
        packetLoss.Value = 0;
        jitter.Value = 0;
        latency.OnValueChanged += OnLatencyChanged;
        packetLoss.OnValueChanged += OnPacketLossChanged;
        jitter.OnValueChanged += OnJitterChanged;

        foreach (var clientId in NetworkManager.ConnectedClientsIds)
        {
            CreateClientFile(clientId);
        }

        NetworkManager.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        CreateClientFile(clientId);
    }

    private void CreateClientFile(ulong clientId)
    {
        string filePath = $"NetworkStats/Client_{clientId}_Latency.txt";
        clientFiles[clientId] = filePath;
        File.WriteAllText(filePath, "Latency Values:\n");
    }

    private void OnLatencyChanged(float oldValue, float newValue)
    {
        latencyText.text = "Latency: " + newValue.ToString() + "ms";
    }

    private void OnPacketLossChanged(int oldValue, int newValue)
    {
        packetLossText.text = "Packet Loss: " + newValue.ToString() + "%";
    }

    private void OnJitterChanged(int oldValue, int newValue)
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
}
