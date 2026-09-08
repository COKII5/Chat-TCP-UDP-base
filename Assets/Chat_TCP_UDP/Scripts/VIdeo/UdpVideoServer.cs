using System;
using System.Net.Sockets;
using System.Net;
using UnityEngine;

public class UdpVideoServer : MonoBehaviour
{
    private UdpClient udpServer; // UDP client to handle network communication
    private IPEndPoint remoteEndPoint; // Endpoint to identify the remote client

    public bool isServerRunning = false; // Flag to check if the server is running
    private bool hasClient = false; // True once a handshake was received from a client

    public void StartUDPServer(int port)
    {
        udpServer = new UdpClient(port); // Initializes the UDP client to listen on the given port
        remoteEndPoint = new IPEndPoint(IPAddress.Any, port); // Configures the endpoint to accept messages from any IP address on the given port.
        Debug.Log("Server started. Waiting for client Handshake");
        udpServer.BeginReceive(ReceiveHandshake, null); // Asynchronous data reception begins
        isServerRunning = true; // Sets the server running flag to true
    }

    private void ReceiveHandshake(IAsyncResult result)
    {
        byte[] receivedBytes = udpServer.EndReceive(result, ref remoteEndPoint); // Completes data reception and gets the received bytes.
        string receivedMessage = System.Text.Encoding.UTF8.GetString(receivedBytes); // Converts received bytes to a string
        Debug.Log("Received handshake from client: " + remoteEndPoint);
        hasClient = true;
    }

    public void SendImage(Texture2D texture, int jpgQuality = 30)
    {
        if (!hasClient) return; // No client has completed the handshake yet

        byte[] jpgBytes = texture.EncodeToJPG(jpgQuality);
        try
        {
            udpServer.Send(jpgBytes, jpgBytes.Length, remoteEndPoint);
            Debug.Log($"[TX] enviado {jpgBytes.Length} bytes");
        }
        catch (SocketException ex)
        {
            Debug.LogError($"Error al enviar UDP: {ex.SocketErrorCode} - {ex.Message}");
        }
    }
}
