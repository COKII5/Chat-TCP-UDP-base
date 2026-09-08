using System.Net.Sockets;
using System.Net;
using System;
using UnityEngine;

public class UdpVideoClient : MonoBehaviour
{
    private UdpClient udpClient;
    private IPEndPoint remoteEndPoint;
    public bool isServerConnected = false;


    public Action<byte[]> OnImageReceived;

    public void StartUDPClient(string ipAddress, int port)
    {
        udpClient = new UdpClient();
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
        udpClient.BeginReceive(ReceiveImage, null);
        SendHandshake();
        isServerConnected = true;
    }

    private void ReceiveImage(IAsyncResult result)
    {
        if (udpClient == null) return;

        byte[] receivedBytes;
        try
        {
            receivedBytes = udpClient.EndReceive(result, ref remoteEndPoint);
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        if (receivedBytes != null && receivedBytes.Length > 0)
        {
            OnImageReceived?.Invoke(receivedBytes);
        }

        if (udpClient != null)
            udpClient.BeginReceive(ReceiveImage, null);
    }

    public void SendHandshake()
    {
        byte[] sendBytes = System.Text.Encoding.UTF8.GetBytes("Hi");
        udpClient.Send(sendBytes, sendBytes.Length, remoteEndPoint);
    }

    public void CloseClient()
    {
        isServerConnected = false;
        udpClient?.Close();
        udpClient = null;
    }

    private void OnDestroy() => CloseClient();
}
