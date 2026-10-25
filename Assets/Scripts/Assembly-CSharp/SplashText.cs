using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class SplashText : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]
    public TextAsset Datas;
    private string[] splashTexts;
    private Text Text;
    private string[] Secrets = new[]
    {
        "\"This message will never appear in the game even if its in the code for the game, isnt that weird?\"",
        "You should not be reading this!",
    };
    private bool isInitalized = false;
    void Start()
    {
        Text = GetComponent<Text>();
        splashTexts = Datas.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Text.text = splashTexts[Random.Range(0, splashTexts.Length - 1)];
        isInitalized = true;
    }

    private void Awake()
    {
        if (!isInitalized)
        {
            return;
        }

        Text.text = splashTexts[Random.Range(0, splashTexts.Length - 1)];
    }
}