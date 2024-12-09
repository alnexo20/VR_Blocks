using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class ShowOptions : MonoBehaviour
{
    public InputActionProperty menuButtonAction;
    private GameObject optionsMenu;
    GameObject startMenu;
    public Transform head;
    public float spawnDist = 2;

    void Awake()
    {
        optionsMenu = gameObject;
    }

    void Start(){

    }

    void Update(){
        if (menuButtonAction.action.WasPressedThisFrame()){
            optionsMenu.SetActive(!optionsMenu.activeSelf);
            optionsMenu.transform.position = head.position + new Vector3(head.forward.x, 0, head.forward.z).normalized * spawnDist;
        }
        optionsMenu.transform.LookAt(new Vector3(head.position.x, optionsMenu.transform.position.y, head.position.z));
        optionsMenu.transform.forward *= -1;
    }

    public void ToggleActive()
    {
        optionsMenu.SetActive(!optionsMenu.activeSelf);
    }

    public void BackToMainMenu()
    {
        startMenu.SetActive(true);
        optionsMenu.SetActive(false);
        NetworkManager.Singleton.DisconnectClient(NetworkManager.Singleton.LocalClientId);
    }
}
