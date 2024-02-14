using System;
using System.IO;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Threading;
using System.Net;

public class TCPClient : MonoBehaviour
{
    [Header("Make Connection")]
    public GameObject ConnectionPanel;
    public TMP_InputField IpaddressInput;

    [Header("Menu")]
    public GameObject MenuPanel;
    public TextMeshProUGUI ConnectionStatus;
    public Image ConnectionImageStatus;
    public GameObject ErrorMsg;

    [Header("Model Player")]
    public GameObject ModelPlayerPanel;
    public TMP_Dropdown ModelDropdown;
    [Space(10)]
    public bool isAutoRotate = false;
    public Image AutoRotateBtn;
    public Sprite AutoRotateUnclicked;
    public Sprite AutoRotateClicked;

    [Header("Video Player")]
    public GameObject VideoPlayerPanel;
    public TMP_Dropdown VideoDropdown;
    [Space(10)]
    public bool isPlay = true;
    public Image PlayBtn;
    public Sprite PauseSprite;
    public Sprite PlaySprite;
    [Space(10)]
    public bool isLoop = false;
    public Image LoopBtn;
    public Sprite LoopUnclicked;
    public Sprite LoopClicked;
    [Space(10)]
    public bool isMute = false;
    public Image MuteBtn;
    public Sprite MuteSprite;
    public Sprite UnmuteSprite;


    public TMP_InputField messageInput;
    public TextMeshProUGUI receivedText;

    private TcpClient client;
    private NetworkStream stream;
    private bool isServerConnected = false;
    private const string EncryptionKey = "JeyHolo";
    string Code;

    private void Start()
    {
        if (PlayerPrefs.HasKey("HoloCode"))
        {
            Code = PlayerPrefs.GetString("HoloCode");
            Connect();
        }
        else
        {
            PlayerPrefs.DeleteKey("HoloCode");
        }
    }
    public void IPOnValueChange(string codeNumber)
    {
        Code = codeNumber;
    }
    public void Connect()
    {
        // Connect to the server

        ConnectToServer();
    }
    string DecryptIP(string encryptedNumber)
    {
        // Pad the input with '=' if needed for Base64 decoding
        while (encryptedNumber.Length % 4 != 0)
        {
            encryptedNumber += "=";
        }

        // Convert the Base64 string back to bytes
        byte[] ipBytes = Convert.FromBase64String(encryptedNumber);

        // Decrypt the IP bytes using XOR
        for (int i = 0; i < ipBytes.Length; i++)
        {
            ipBytes[i] = (byte)(ipBytes[i] ^ (byte)EncryptionKey[i % EncryptionKey.Length]);
        }

        // Convert the decrypted bytes back to an IP address
        IPAddress decryptedIP = new IPAddress(ipBytes);
        return decryptedIP.ToString();
    }
    private async void ConnectToServer()
    {
        try
        {
            PlayerPrefs.SetString("HoloCode", Code);
            // IP address and port of the server to connect to
            string serverIP = DecryptIP(Code);
            int port = 8888;

            while (!isServerConnected)
            {
                client = new TcpClient();
                await client.ConnectAsync(serverIP, port);
                
                Debug.Log("Connected to server.");
                isServerConnected = true;
                ConnectionStatus.text = "Connected";
                ConnectionImageStatus.color = Color.green;

                ConnectionPanel.SetActive(false);
                MenuPanel.SetActive(true);

                stream = client.GetStream();

                // Start receiving messages asynchronously
                _ = ReceiveMessagesAsync();
            }

        }
        catch (Exception e)
        {
            Debug.LogError("Error connecting to server: " + e.Message);
            isServerConnected = false;
            ErrorMsg.SetActive(true);
        }
    }
    private async Task ReceiveMessagesAsync()
    {
        byte[] buffer = new byte[1024];
        int bytesRead;

        while (isServerConnected)
        {
            try
            {
                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                if (bytesRead == 0)
                {
                    Debug.Log("Server disconnected.");
                    PlayerPrefs.DeleteKey("HoloCode");
                    MenuControll.instance.ChangeScene("Menu");
                    isServerConnected = false;
                    //HandleServerDisconnection();
                }
                    // Process received message on the main thread
                    ReceiveMessages(message);
            }
            catch (Exception e)
            {
                Debug.LogError("Error receiving message: " + e.Message);
                HandleServerDisconnection();
                break;
            }
        }
    }

    private void ReceiveMessages(string message)
    {
        string[] received = message.Split(":");
        if(received[0]== "Modellist")
        {
            string[] data = received[1].Split(",");
            List<string> List = new List<string>(data);
            List.Remove(data[data.Length-2]);// dropdown value
            List.Remove(data[data.Length-1]);// Autorotate
            ModelDropdown.ClearOptions();
            ModelDropdown.AddOptions(List);
            ModelDropdown.value = int.Parse( data[data.Length-2]); // dropdown value
            SetAutorotate(bool.Parse(data[data.Length - 1]));// isautorotate
            ModelMenu(true);
        }
        else if (received[0] == "Videolist")
        {
            string[] data = received[1].Split(",");
            List<string> List = new List<string>(data);
            List.Remove(data[data.Length - 4]);// dropdown value
            List.Remove(data[data.Length - 3]);// isloop
            List.Remove(data[data.Length - 2]);// ismute
            List.Remove(data[data.Length - 1]);// isplaying
            VideoDropdown.ClearOptions();
            VideoDropdown.AddOptions(List);
            VideoDropdown.value = int.Parse(data[data.Length - 4]);
            SetLoop(bool.Parse(data[data.Length - 3]));
            SetMute(bool.Parse(data[data.Length - 2]));
            SetPlay(bool.Parse(data[data.Length - 1]));
            VideoMenu(true);
        }
        else if(received[0] == "Change")
        {
            NextVideo();
        }
    }
    public void SetLoop(bool value)
    {
        if (value)
        {
            //button pressed
            LoopBtn.sprite = LoopClicked;
        }
        else
        {
            LoopBtn.sprite = LoopUnclicked;
        }
        isLoop = value;
    }

    public void SetMute(bool value)
    {
        if (value)
        {
            MuteBtn.sprite = MuteSprite;
        }
        else
        {
            MuteBtn.sprite = UnmuteSprite;
        }
        isMute = value;
    }

    public void SetPlay(bool value)
    {
        //PlayBtn.sprite = PauseSprite;
        if (value)
        {
            //button pressed
            PlayBtn.sprite = PauseSprite;
        }
        else
        {
            PlayBtn.sprite = PlaySprite;
        }
        isPlay = value;
    }
    public void SetAutorotate(bool value)
    {
        if (value)
        {
            AutoRotateBtn.sprite = AutoRotateClicked;
        }
        else
        {
            AutoRotateBtn.sprite = AutoRotateUnclicked;
        }
        isAutoRotate = value;
    }

    public void ModelMenu(bool IsOn)
    {
        ModelPlayerPanel.SetActive(IsOn);
        MenuPanel.SetActive(!IsOn);
    }
    public void VideoMenu(bool IsOn)
    {
        VideoPlayerPanel.SetActive(IsOn);
        MenuPanel.SetActive(!IsOn);
    }

    public async void SendMessage(string message)
    {
        try
        {
            //string message = messageInput.text;

            byte[] data = Encoding.ASCII.GetBytes(message);
            await stream.WriteAsync(data, 0, data.Length);

            Debug.Log("Sent message: " + message);

        }
        catch (Exception e)
        {
            Debug.LogError("Error sending message: " + e.Message);
        }
    }
    private void HandleServerDisconnection()
    {
        // Perform actions when the server disconnects
        Debug.LogWarning("Server disconnected unexpectedly.");
        isServerConnected = false;

        ConnectionStatus.text = "Disconnected";
        ConnectionImageStatus.color = Color.red;
        // Attempt reconnection after a delay (e.g., 3 seconds)
        Invoke("AttemptReconnection", 3f);
    }

    private async void AttemptReconnection()
    {
        while (!isServerConnected)
        {
            Debug.Log("Attempting reconnection...");
            ConnectToServer();
            await Task.Delay(3000); // Wait for 3 seconds before attempting reconnection again
        }
    }

    #region Model functionalities

    public void LoadModel()
    {
        string Code = "M,List";
        SendMessage(Code);
    }

    public void ChangeModel(Int32 model)
    {
        string Code = "M,Select,";
        string DataToSend = Code + model.ToString();
        SendMessage(DataToSend);
    }

    public void Zoom(int model)
    {
        string Code = "M,Zoom,";
        string DataToSend = Code + model.ToString();
        SendMessage(DataToSend);
    }
    public void ResetModel()
    {
        string Code = "M,Reset";
        SendMessage(Code);
    }

    public void AutoRotate()
    {
        if (isAutoRotate)
        {
            isAutoRotate = false;
            AutoRotateBtn.sprite = AutoRotateUnclicked;
        }
        else
        {
            isAutoRotate = true;
            AutoRotateBtn.sprite = AutoRotateClicked;
        }
        string Code = "M,AutoRotate,";
        string DataToSend = Code + isAutoRotate;
        SendMessage(DataToSend);
    }
    public void Pressed(string key)
    {
        string Code = "M,Press,";
        string DataToSend = Code + key;
        SendMessage(DataToSend);
    }
  

    public void NextModel()
    {
        if (ModelDropdown.value < ModelDropdown.options.Count - 1)
        {
            ModelDropdown.value= ModelDropdown.value + 1;
        }
        else if (ModelDropdown.value == ModelDropdown.options.Count - 1)
        {
            ModelDropdown.value = 0;
        }
    }
    public void PreviousModel()
    {
        if (ModelDropdown.value == 0)
        {
            ModelDropdown.value = ModelDropdown.options.Count - 1;
        }
        else if (ModelDropdown.value > 0)
        {
            ModelDropdown.value = ModelDropdown.value - 1;
        }
    }

    #endregion

    #region Video functionalities

    public void LoadVideo()
    {
        string Code = "V,List";
        SendMessage(Code);
    }

    public void PlayPause()
    {
        if (isPlay)
        {
            isPlay = false;
            PlayBtn.sprite = PlaySprite;
        }
        else
        {
            isPlay = true;
            PlayBtn.sprite = PauseSprite;
        }
        string Code = "V,isPlay,";
        string DataToSend = Code + isPlay.ToString();
        SendMessage(DataToSend);
    }

    public void ChangeVideo(Int32 model)
    {
        string Code = "V,Select,";
        string DataToSend = Code + model.ToString();
        SendMessage(DataToSend);
    }
    public void NextVideo()
    {
        if (VideoDropdown.value < VideoDropdown.options.Count - 1)
        {
            VideoDropdown.value = VideoDropdown.value + 1;
        }
        else if (VideoDropdown.value == VideoDropdown.options.Count - 1)
        {
            VideoDropdown.value = 0;
        }
    }
    public void PreviousVideo()
    {
        if (VideoDropdown.value == 0)
        {
            VideoDropdown.value = VideoDropdown.options.Count - 1;
        }
        else if (VideoDropdown.value > 0)
        {
            VideoDropdown.value = VideoDropdown.value - 1;
        }
    }
    public void Loop()
    {
        if (isLoop)
        {
            isLoop = false;
            LoopBtn.sprite = LoopUnclicked;
        }
        else
        {
            isLoop = true;
            LoopBtn.sprite = LoopClicked;
        }
        string Code = "V,isLoop,";
        string DataToSend = Code + isLoop.ToString();
        SendMessage(DataToSend);
    }

    public void Mute()
    {
        if (isMute)
        {
            isMute = false;
            MuteBtn.sprite = UnmuteSprite;
        }
        else
        {
            isMute = true;
            MuteBtn.sprite = MuteSprite;
        }
        string Code = "V,isMute,";
        string DataToSend = Code + isMute.ToString();
        SendMessage(DataToSend);
    }
    public void Volume(float sound)
    {
        string Code = "V,Volume,";
        string DataToSend = Code + sound.ToString();
        SendMessage(DataToSend);
    }

    #endregion

    private void OnDestroy()
    {
        if (client != null)
        {
            client.Close();
        }
    }
}
