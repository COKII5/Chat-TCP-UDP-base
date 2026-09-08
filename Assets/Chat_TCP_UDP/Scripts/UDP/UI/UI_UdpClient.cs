using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UdpClientUI: MonoBehaviour
{
    public int serverPort = 5555;
    public string serverAddress = "127.0.0.1";
    [SerializeField] private UDPClient clientReference;
    [SerializeField] private TMP_InputField messageInput;

    private IClient _client;
    void Awake()
    {
        _client = clientReference;
    }
    void Start()
    {
        _client.OnMessageReceived += HandleMessageReceived;
        _client.OnConnected += HandleConnection;
        _client.OnDisconnected += HandleDisconnection;
    }
    public void ConnectClient()
    {
        _client.ConnectToServer(serverAddress, serverPort);
    }
    public void SendClientMessage()
    {
        if(!_client.isConnected)
        {
            Debug.Log("The client is not connected");
            return;
        }

        if(messageInput.text == ""){
            Debug.Log("The chat entry is empty");
            return;
        }

        string message = messageInput.text;
        _client.SendMessageAsync(message);
    }

    void HandleMessageReceived(string text)
    {
        Debug.Log("[UI-Client] Message received from server: " + text);
    }

    void HandleConnection()
    {
        Debug.Log("[UI-Client] Client Connected to Server");
    }
    void HandleDisconnection()
    {
        Debug.Log("[UI-Client] Client Disconnect from Server");
    }
}
