using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_TCPClient: MonoBehaviour
{
    public int serverPort = 5555;
    public string serverAddress = "127.0.0.1";
    [SerializeField] private TCPClient clientReference;
    [SerializeField] private TMP_InputField messageInput;
    [SerializeField] private TextMeshProUGUI chatDisplay;
    [SerializeField] private ScrollRect chatScrollRect;

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
        AppendToChat("Yo: " + message);
        messageInput.text = "";
    }

    void HandleMessageReceived(string text)
    {
        Debug.Log("[UI-Client] Message received from server: " + text);
        AppendToChat("Servidor: " + text);
    }

    void HandleConnection()
    {
        Debug.Log("[UI-Client] Client Connected to Server");
        AppendToChat("<i>Conectado al servidor.</i>");
    }
    void HandleDisconnection()
    {
        Debug.Log("[UI-Client] Client Disconnect from Server");
        AppendToChat("<i>Desconectado del servidor.</i>");
    }

    void AppendToChat(string line)
    {
        if (chatDisplay == null) return;
        chatDisplay.text += line + "\n";

        if (chatScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }
}

