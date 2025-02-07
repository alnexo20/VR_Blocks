using System.Collections;
using System.Collections.Generic;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameStartMenu : MonoBehaviour
{
    [Header("UI Pages")]
    public GameObject mainMenu;
    public GameObject title;

    [Header("Main Menu Buttons")]
    public Button startButton;
    public Button joinButton;
    public Button serverButton;
    public Button quitButton;
    public TMP_Text myIPAddress;

    // Start is called before the first frame update
    void Start()
    {
        EnableMainMenu();

        myIPAddress.text = "Your IP: " + GetLocalIPAddress();

        //Hook events
        startButton.onClick.AddListener(HideAll);
        joinButton.onClick.AddListener(HideAll);
        serverButton.onClick.AddListener(HideAll);
        quitButton.onClick.AddListener(QuitGame);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void HideAll()
    {
        mainMenu.SetActive(false);
        title.SetActive(false);
    }

    public void EnableMainMenu()
    {
        mainMenu.SetActive(true);
        title.SetActive(true);
    }

    private string GetLocalIPAddress()
    {
        string localIP = "";
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                localIP = ip.ToString();
                break;
            }
        }
        return localIP;
    }
}
