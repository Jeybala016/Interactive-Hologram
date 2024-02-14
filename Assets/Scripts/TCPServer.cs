using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.IO;
using System.Collections;

public class TCPServer : MonoBehaviour
{
    public TextMeshProUGUI ipAddressText;
    private TcpListener listener;
    private bool isServerRunning = false;
    private List<TcpClient> connectedClients = new List<TcpClient>();

    public static TCPServer instance;

    private const string EncryptionKey = "JeyHolo";
    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        
        StartServer();
    }
    public string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                //ipAddress.text = ip.ToString();
                return ip.ToString();
            }
        }
        throw new System.Exception("No network adapters with an IPv4 address in the system!");
    }
    string Encrypt(string input)
    {
        // Parse the IP address
        IPAddress ip = IPAddress.Parse(input);
        byte[] ipBytes = ip.GetAddressBytes();

        // Encrypt the IP bytes using XOR
        for (int i = 0; i < ipBytes.Length; i++)
        {
            ipBytes[i] = (byte)(ipBytes[i] ^ (byte)EncryptionKey[i % EncryptionKey.Length]);
        }

        // Convert the encrypted bytes to a Base64 string
        string base64Encoded = Convert.ToBase64String(ipBytes);

        // Remove padding characters
        base64Encoded = base64Encoded.TrimEnd('=');

        // Truncate to 16 characters
        return base64Encoded.Substring(0, Mathf.Min(base64Encoded.Length, 16));
    }
    private async void StartServer()
    {
        try
        {
            IPAddress ipAddress = IPAddress.Any;
            int port = 8888;

            listener = new TcpListener(ipAddress, port);
            listener.Start();

            Debug.Log("Server is listening...");
            isServerRunning = true;

            ipAddressText.text = Encrypt( GetLocalIPAddress());


            // Accept incoming client connections asynchronously
            while (isServerRunning)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                Debug.Log("Client connected: " + ((IPEndPoint)client.Client.RemoteEndPoint).Address);

                connectedClients.Add(client);

               

                _ = HandleClientAsync(client);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error starting server: " + e.Message);
        }
    }
    
    private async System.Threading.Tasks.Task HandleClientAsync(TcpClient client)
    {

        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];
        int bytesRead;
        while (isServerRunning && client.Connected)
        {
            try
            {
                
                //read message
                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                if (bytesRead == 0)
                {
                    // No bytes read; the client might be disconnected
                    Debug.Log("Client disconnected.");
                    client.Close();
                    connectedClients.Remove(client);
                    break;
                }
                Debug.Log("Received message from client: " + message);

                // Process received message or perform actions based on the message
                ReceiveMessages(client,message);

                // If needed, you can send a response back to the client
                //byte[] responseData = Encoding.ASCII.GetBytes(responseMessage);
                //await stream.WriteAsync(responseData, 0, responseData.Length);

            }
            catch (SocketException e)
            {
                Debug.LogError("Error receiving or sending message: " + e.Message);
                break;
            }
        }

    }
    public void loadModel()
    {
        SendToAllClients( Hologram.instance.Modellist());
    }
    private void ReceiveMessages(TcpClient client, string message)
    {
        string[] data = message.Split(",");
        if (data[0] == "M")
        {
            //Model control
            switch (data[1])
            {
                case "Press":
                    Hologram.instance.RotateModel(data[2]);
                    break;
                case "List":
                    SendToClient(client,"Modellist:" + Hologram.instance.Modellist());
                    break;
                case "Reset":
                    Hologram.instance.ResetModel();
                    break;
                case "AutoRotate":
                    bool value = bool.Parse(data[2]);
                    Hologram.instance.AutoRotate(value);
                    break;
                case "Select":
                    int number = int.Parse(data[2]);
                    Hologram.instance.LoadModel(number);
                    break;
                case "Zoom":
                    int Zoom = int.Parse(data[2]);
                    Hologram.instance.Zoom(Zoom);
                    break;
            }
            
        }
        else if (data[0] == "V")
        {
            //Video control
            switch (data[1])
            {
                case "List":
                    SendToClient(client, "Videolist:" + Hologram.instance.Videolist());
                    break;
                case "isPlay":
                    bool value = bool.Parse(data[2]);
                    Hologram.instance.Play(value);
                    break;
                case "isLoop":
                    bool Loopvalue = bool.Parse(data[2]);
                    Hologram.instance.Loop(Loopvalue);
                    break;
                case "isMute":
                    bool Mutevalue = bool.Parse(data[2]);
                    Hologram.instance.Mute(Mutevalue);
                    break;
                case "Volume":
                    float volume = float.Parse(data[2]);
                    Hologram.instance.Volume(volume);
                    break;
                case "Select":
                    int number = int.Parse(data[2]);
                    Hologram.instance.LoadVideo(number);
                    break;
            }
        }
        data = null;

    }

    public bool SendVideoCompletedSignal()
    {
        Debug.Log(connectedClients.Count);
        if (connectedClients.Count != 0)
        {
            SendToAllClients("Change:");
            return true;
        }
        else
            return false;
    }

    // Method to send a message to all connected clients
    public void SendToAllClients(string message)
    {
        byte[] data = Encoding.ASCII.GetBytes(message);
        foreach (TcpClient client in connectedClients)
        {
            NetworkStream stream = client.GetStream();
            _ = stream.WriteAsync(data, 0, data.Length);
        }
    }
    public void SendToClient(TcpClient client,string message)
    {
        byte[] data = Encoding.ASCII.GetBytes(message);        
        NetworkStream stream = client.GetStream();
        _ = stream.WriteAsync(data, 0, data.Length);
        Debug.Log("Sent message to client ");
    }

    

    private void OnDestroy()
    {
        isServerRunning = false; // Stop the server loop

        foreach (TcpClient client in connectedClients)
        {
            client.Close();
        }

        if (listener != null)
        {
            listener.Stop();
        }
    }
}
