using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class Hologram : MonoBehaviour
{
    [Header("Menu")]
    public GameObject MenuArea;


    [Header("Model Handling")]
    public GameObject ModelArea;
    public Transform Camera;
    public bool isAutoRotate = false;
    public bool isPressed = false;
    float y;
    float z;
    public float AutoRotateSpeed;
    public List<GameObject> Models;
    public GameObject LoadedGameObject = null;
    public int LoadedGameobjectId;

    [Header("Video Handling")]
    public GameObject VideoArea;
    public VideoPlayer VideoPlayer;
    public AudioSource AudioPlayer;
    public List<VideoClip> Videos;
    public int LoadedVideoId;
    public bool isPlay = true;
    public bool isMute = false;
    public float Volumelevel = 1;

    public static Hologram instance;
    private void Awake()
    {
        instance = this;
    }
    private void Start()
    {
        
    }
    #region Models Functions
    public string Modellist()
    {
        MenuArea.SetActive(false);
        VideoArea.SetActive(false);
        ModelArea.SetActive(true);
        List<string> items = new List<string>();
        for (int i = 0; i < Models.Count; i++)
        {
            items.Add(Models[i].name);
        }
        // for dropdown value
        items.Add(LoadedGameobjectId.ToString());
        // for autorotate
        items.Add(isAutoRotate.ToString());
        string names = string.Join(",", items);
        return names;
    }

    public void LoadModel(int value)
    {
        if (value <= Models.Count)
        {
            if (LoadedGameObject != null)
                LoadedGameObject.SetActive(false);
            LoadedGameobjectId = value;
            LoadedGameObject = Models[LoadedGameobjectId];
            LoadedGameObject.SetActive(true);
        }
            
    }

    public void ResetModel()
    {
        LoadedGameObject.transform.rotation = Quaternion.identity;
    }

    public void Zoom(int value)
    {
        Camera.position = new Vector3(0, 0, Camera.position.z + value);
    }
    public void RotateModel(string direction)
    {
        if (!isAutoRotate && LoadedGameObject != null)
        {
            switch (direction)
            {
                case "Right":
                    isPressed = true;
                    y = 1;
                    z = 0;
                    break;
                case "Left":
                    isPressed = true;
                    y = -1;
                    z = 0;
                    break;
                case "Top":
                    isPressed = true;
                    y = 0;
                    z = 1;
                    break;
                case "Bottom":
                    isPressed = true;
                    y = 0;
                    z = -1;
                    break;
                case "Released":
                    isPressed = false;
                    y = 0;
                    z = 0;
                    break;
                default:
                    isPressed = false;
                    break;
            }
            //LoadedGameObject.transform.Rotate(0, Time.deltaTime + vect.x, Time.deltaTime + vect.y);
        }
    }
    public void AutoRotate(bool value)
    {
        if (value)
        {
            isAutoRotate = true;
        }
        else
        {
            isAutoRotate = false;
        }
    }
    
    private void Update()
    {
        if (isAutoRotate && LoadedGameObject != null)
        {
            LoadedGameObject.transform.Rotate(0, AutoRotateSpeed*0.1f + Time.deltaTime, 0);

        }
        else if (isPressed && LoadedGameObject != null)
        {
            if (z == 0)
                LoadedGameObject.transform.Rotate(0, Time.deltaTime + y, 0);
            else if (y == 0)
                LoadedGameObject.transform.Rotate(0, 0, Time.deltaTime + z);
        }
    }
    #endregion

    #region Video Functons
    public string Videolist()
    {
        MenuArea.SetActive(false);
        ModelArea.SetActive(false);
        VideoArea.SetActive(true);
        List<string> items = new List<string>();
        for (int i = 0; i < Videos.Count; i++)
        {
            items.Add(Videos[i].name);
        }
        //for dropdown value
        items.Add(LoadedVideoId.ToString());
        //for loop
        items.Add(VideoPlayer.isLooping.ToString());
        //for mute
        items.Add(AudioPlayer.mute.ToString());
        //for play/pause
        items.Add(isPlay.ToString());

        string names = string.Join(",", items);
        VideoPlayer.loopPointReached -= OnVideoEnded;
        VideoPlayer.loopPointReached += OnVideoEnded;
        return names;
    }
    void OnVideoEnded(VideoPlayer vp)
    {
        if (!vp.isLooping)
        {
            bool isSend = TCPServer.instance.SendVideoCompletedSignal();
            Debug.Log(isSend);
            if(!isSend)
                LoadVideo(LoadedVideoId+1);
        }
    }

    public void LoadVideo(int value)
    {
        if (value < Videos.Count)
        {
            LoadedVideoId = value;
        }
        else
        {
            LoadedVideoId = 0;
        }
        VideoPlayer.clip = Videos[LoadedVideoId];
        VideoPlayer.SetTargetAudioSource(0, AudioPlayer);
        if (isPlay)
            VideoPlayer.Play();
        else
            VideoPlayer.Pause();
    }

    public void Play(bool value)
    {
        if (value)
        {
            isPlay = true;
            VideoPlayer.Play();
        }
        else
        {
            isPlay = false;
            VideoPlayer.Pause();
        }
    }
    public void Loop(bool value)
    {
        VideoPlayer.isLooping = value;
    }
    
    public void Mute(bool value)
    {
        if (isMute)
        {
            AudioPlayer.mute = false;
            isMute = false;
            //VideoPlayer.SetDirectAudioMute(0, isMute);
        }
        else
        {
            AudioPlayer.mute = true;
            isMute = true;
            //VideoPlayer.SetDirectAudioMute(0, isMute);
        }
    }

    public void Volume(float value)
    {
        Volumelevel = Mathf.Clamp(Volumelevel + value, 0, 1);
        AudioPlayer.volume = Volumelevel;
        //VideoPlayer.SetDirectAudioVolume(0, Volumelevel);
    }

    #endregion

}
