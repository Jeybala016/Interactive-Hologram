
using UnityEngine;
using TMPro;
using System;

public class Digitalclock : MonoBehaviour
{
    public TextMeshProUGUI clockText;

    void Update()
    {
        DateTime currentTime = DateTime.Now;
        string timeString = currentTime.ToString("HH:mm:ss");
        clockText.text = timeString;
    }
}
