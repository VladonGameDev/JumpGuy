using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUiController : MonoBehaviour
{
    private bool isGameStarted;

    
    void Awake()
    {
        PlayerPrefs.SetInt("isGameStarted", 0);
        isGameStarted = false;
    }
    public void StartGame()
    {
        PlayerPrefs.SetInt("isGameStarted", 1);
        isGameStarted = true;
    }
}
