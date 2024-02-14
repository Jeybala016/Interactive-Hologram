using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuControll : MonoBehaviour
{
    public static MenuControll instance;
    private void Awake()
    {
        if (instance == null)
            instance = this;
    }
    void Start()
    {
        
    }

    public void ChangeScene(string name)
    {
        SceneManager.LoadScene(name);
    }
}
