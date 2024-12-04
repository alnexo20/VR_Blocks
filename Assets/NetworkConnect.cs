using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class NetworkConnect : MonoBehaviour
{

    public void Create() { 
        NetworkManager.Singleton.StartHost();
    }

    public void Join() {
        NetworkManager.Singleton.StartClient(); 
    }
}
