using System.Net.Sockets;
using System.Net;
using System;
using UnityEngine;

public class UdpVideoClient : MonoBehaviour
{
    private UdpClient udpClient; // UDP client to handle network communication
    private IPEndPoint remoteEndPoint; // Endpoint to identify the remote server
    public bool isServerConnected = false; // Flag to check if the client is connected to the server
 

    public Action<byte[]> OnImageReceived; // Event triggered when a new image is received

    public void StartUDPClient(string ipAddress, int port)
    {
        udpClient = new UdpClient(); // Initializes the UDP client without binding to any local port
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port); // Sets the remote server endpoint using the given IP address and port
        udpClient.BeginReceive(ReceiveImage, null); // Starts receiving data from the server asynchronously
        SendHandshake();
        isServerConnected = true; // Sets the client connected flag to true
    }

    private void ReceiveImage(IAsyncResult result)
    {
        if (udpClient == null) return; // socket ya cerrado (OnDestroy)

        byte[] receivedBytes;
        try
        {
            receivedBytes = udpClient.EndReceive(result, ref remoteEndPoint);
        }
        catch (ObjectDisposedException)
        {
            return; // el socket se cerro mientras esta recepcion estaba pendiente
        }

        if (receivedBytes != null && receivedBytes.Length > 0)
        {
            OnImageReceived?.Invoke(receivedBytes);
        }

        if (udpClient != null)
            udpClient.BeginReceive(ReceiveImage, null); // seguir escuchando
    }

    public void SendHandshake()
    {
        byte[] sendBytes = System.Text.Encoding.UTF8.GetBytes("Hi"); // Converts the message into a byte array
        udpClient.Send(sendBytes, sendBytes.Length, remoteEndPoint); // Sends the bytes to the remote server using UDP
    }

    public void CloseClient()
    {
        isServerConnected = false;
        udpClient?.Close();
        udpClient = null;
    }

    private void OnDestroy() => CloseClient();
}
