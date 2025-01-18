using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ScoreboardManager : NetworkBehaviour
{
    public TextMeshProUGUI player1ScoreText; // TextMeshPro Text to display Player 1's score
    public TextMeshProUGUI player2ScoreText; // TextMeshPro Text to display Player 2's score
    public TextMeshProUGUI winnerText; // TextMeshPro Text to display the winner
    public NetworkVariable<int> player1Score = new NetworkVariable<int>(0); 
    public NetworkVariable<int> player2Score = new NetworkVariable<int>(0);
    private int maxScore = 30;

    // Start is called before the first frame update
    void Start()
    {
        this.gameObject.SetActive(true);
    }

    public override void OnNetworkSpawn() { 
        player1Score.Value = 0;
        player2Score.Value = 0;
        player1Score.OnValueChanged += OnPlayer1ScoreChanged; 
        player2Score.OnValueChanged += OnPlayer2ScoreChanged; 

    } 
    
    private void OnPlayer1ScoreChanged(int oldValue, int newValue) { 
        player1ScoreText.text = "Player 1 Score: " + newValue.ToString(); 
    } 
    
    private void OnPlayer2ScoreChanged(int oldValue, int newValue) { 
        player2ScoreText.text = "Player 2 Score: " + newValue.ToString(); 
    }

    private void UpdateScoreText()
    {
        player1ScoreText.text = "Player 1 Score: " + player1Score.Value.ToString();
        player2ScoreText.text = "Player 2 Score: " + player2Score.Value.ToString();
        UpdateScoreTextClientRpc();
    }

    [ClientRpc]
    private void UpdateScoreTextClientRpc(){
        player1ScoreText.text = "Player 1 Score: " + player1Score.Value.ToString();
        player2ScoreText.text = "Player 2 Score: " + player2Score.Value.ToString();
    }

    public void UpdatePlayerScore(int playerId){
        if (playerId == 1){
            player1Score.Value++;
        }else if (playerId == 2){
            player2Score.Value++;
        }

        UpdateScoreText();
        
        if (player1Score.Value >= maxScore || player2Score.Value >= maxScore) { 
            DisplayWinner(); 
        }
    }

    private void DisplayWinner()
    {
        if (player1Score.Value >= maxScore)
        {
            winnerText.text = "Player 1 Wins!";
        }
        else if (player2Score.Value >= maxScore)
        {
            winnerText.text = "Player 2 Wins!";
        }
        DisplayWinnerClientRpc();
    }

    [ClientRpc]
    private void DisplayWinnerClientRpc()
    {
        if (player1Score.Value >= maxScore)
        {
            winnerText.text = "Player 1 Wins!";
        }
        else if (player2Score.Value >= maxScore)
        {
            winnerText.text = "Player 2 Wins!";
        }
         
    }
}
