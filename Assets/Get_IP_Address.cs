using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using TMPro;

public class Get_IP_Address : MonoBehaviour
{
    public TMP_Text IPAddress;

    void Start()
    {
        IPAddress.text = GetLocalIPAddress();
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