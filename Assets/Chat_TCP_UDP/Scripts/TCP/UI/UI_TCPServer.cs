using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class TCPServerUI : MonoBehaviour
{
    public int serverPort = 5555;
    [SerializeField] private TCPServer serverReference;
    [SerializeField] private TMP_InputField messageInput;
    [SerializeField] private TextMeshProUGUI chatDisplay;

    private IServer _server;
    void Awake()
    {
        _server = serverReference;
    }
    void Start()
    {
        _server.OnMessageReceived += HandleMessageReceived;
        _server.OnConnected += HandleConnection;
        _server.OnDisconnected += HandleDisconnection;
    }
    public void StartServer()
    {
        _server.StartServer(serverPort);
    }
    public void SendServerMessage()
    {
        if(!_server.isServerRunning){
            Debug.Log("The server is not running");
            return;
        }

        if(messageInput.text == ""){
            Debug.Log("The chat entry is empty");
            return;
        }

        string message = messageInput.text;
        _server.SendMessageAsync(message);
        AppendToChat("Yo: " + message);
        messageInput.text = "";
    }

    void HandleMessageReceived(string text)
    {
        Debug.Log("[UI-Server] Message received from client: " + text);
        AppendToChat("Cliente: " + text);
    }

    void HandleConnection()
    {
        Debug.Log("[UI-Server] Client Connected to Server");
        AppendToChat("<i>Cliente conectado.</i>");
    }
    void HandleDisconnection()
    {
        Debug.Log("[UI-Server] Client Disconnect from Server");
        AppendToChat("<i>Cliente desconectado.</i>");
    }

    void AppendToChat(string line)
    {
        if (chatDisplay == null) return;
        chatDisplay.text += line + "\n";
    }
}
