using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WinnerMenuManager : MonoBehaviour
{
    public TMP_Text WinnerText;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void hideMenu(){
        this.gameObject.SetActive(false);
    }

    public void setWinnerText(string text){
        WinnerText.text = text;
    }
}
