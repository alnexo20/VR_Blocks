using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NetworkConnect : MonoBehaviour
{
    public TMPro.TMP_InputField ipAddressInput; 
    private ushort port = 7777; 
    private UnityTransport transport;

    void Start(){
        transport = NetworkManager.Singleton.GetComponent<UnityTransport>(); 
        ipAddressInput.onValueChanged.AddListener(OnIpAddressChanged); 
    }

    public void StartServer(){
        NetworkManager.Singleton.StartServer();
    }

    public void Create() { 
        NetworkManager.Singleton.StartHost();
    }

    public void Join() {
        NetworkManager.Singleton.StartClient(); 
    }

    public void StopConnections(){
        NetworkManager.Singleton.Shutdown();
    }

    void OnIpAddressChanged(string ipAddress) { 
        transport.SetConnectionData(ipAddress, port); 
    }
}
