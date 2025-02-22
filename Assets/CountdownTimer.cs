using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;

public class CountdownTimer : MonoBehaviour
{
    public GameObject countdownCanvas;
    public TMP_Text countdownText;
    private float currentTime = 0f;
    private float startingTime = 5f;

    // Start is called before the first frame update
    void Start()
    {
        currentTime = startingTime;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartTimer(){
        countdownCanvas.SetActive(true);
        while (currentTime > 0){
            currentTime -= 1 * Time.deltaTime;
            countdownText.text = currentTime.ToString("0");
        }
        countdownCanvas.SetActive(false);
    }
}
