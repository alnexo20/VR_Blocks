using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class OptionsNetworkStats : NetworkBehaviour
{
    public TextMeshProUGUI latencyText; 
    public TextMeshProUGUI packetLossText; 
    public TextMeshProUGUI jitterText; 
    public NetworkVariable<int> latency = new NetworkVariable<int>(0); 
    public NetworkVariable<int> packetLoss = new NetworkVariable<int>(0);
    public NetworkVariable<int> jitter = new NetworkVariable<int>(0);

    // Start is called before the first frame update
    void Start()
    {
        this.gameObject.SetActive(true);
    }

    void Update(){
        // Update network stats each tick
        latency.Value = (int)NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.Singleton.NetworkConfig.NetworkTransport.ServerClientId);
    }

    public override void OnNetworkSpawn() { 
        latency.Value = 0;
        packetLoss.Value = 0;
        jitter.Value = 0;
        latency.OnValueChanged += OnLatencyChanged; 
        packetLoss.OnValueChanged += OnPacketLossChanged; 
        jitter.OnValueChanged += OnJitterChanged;
    } 
    
    private void OnLatencyChanged(int oldValue, int newValue) { 
        latencyText.text = "Latency: " + newValue.ToString() + "ms";
    } 
    
    private void OnPacketLossChanged(int oldValue, int newValue) { 
        packetLossText.text = "Packet Loss: " + newValue.ToString() + "%"; 
    }

    private void OnJitterChanged(int oldValue, int newValue) { 
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
    private void UpdateNetworkStatsTextClientRpc(){
        latencyText.text = "Latency: " + latency.Value.ToString() + "ms";
        packetLossText.text = "Packet Loss: " + packetLoss.Value.ToString() + "%";
        jitterText.text = "Jitter: " + jitter.Value.ToString() + "ms";
    }
}
