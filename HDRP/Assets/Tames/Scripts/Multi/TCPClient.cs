using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class TCPClient : MonoBehaviour
{

    public static TcpClient socketConnection;
    private Thread clientReceiveThread;
    public static TCPClient Singleton;
    public static Markers.PublishProject publish;

    const byte RegisterRequest = 12;
    const byte EmailRequest = 14;
    // Use this for initialization 	
    void Start()
    {
        Singleton = this;
    }
    public static async void InitiateMail(string text, string ip, string port)
    {
      //  if(publish ==null)publish =gameObject.GetComponent<Markers.PublishProject>();
        await System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                socketConnection = new TcpClient(ip, int.Parse(port));
                SendEmail(text);
                Debug.Log("sending ended");
            }
            catch (SocketException socketException)
            {
                Debug.Log("Socket exception: " + socketException);
            }
        });


    }
    /// <summary> 	
    /// Send message to server using socket connection. 	
    /// </summary> 	
    public static void SendEmail(string text)
    {
        Debug.Log("sending began ... "+text);
        char[] c = text.ToCharArray();
        byte[] buffer = new byte[c.Length * 2 + 3];
        buffer[0] = EmailRequest;
        buffer[1] = (byte)(c.Length / 256);
        buffer[2] = (byte)(c.Length % 256);
        for (int i = 0; i < c.Length; i++)
        {
            buffer[(i + 1) * 2 + 1] = (byte)(((ushort)c[i]) / 256);
            buffer[(i + 1) * 2 + 2] = (byte)(((ushort)c[i]) % 256);
        }
        // Get a stream object for writing. 			
        NetworkStream stream = socketConnection.GetStream();
        if (stream.CanWrite)
            stream.Write(buffer, 0, buffer.Length);
        Debug.Log("added to stream "+stream.CanWrite+" "+buffer.Length);

    }

    public static async void InitiateRegister(string text, string ip, string port)
    {
      //  if (publish == null) publish = gameObject.GetComponent<Markers.PublishProject>();
        await System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                socketConnection = new TcpClient(ip, int.Parse(port));
                SendRegister(text);
                Debug.Log("sending ended");
            }
            catch (SocketException socketException)
            {
                Debug.Log("Socket exception: " + socketException);
            }
        });


    }
    public static void SendRegister(string text)
    {
        Debug.Log("sending began ... " + text);
        char[] c = text.ToCharArray();
        byte[] buffer = new byte[c.Length * 2 + 3];
        buffer[0] = RegisterRequest;
        buffer[1] = (byte)(c.Length / 256);
        buffer[2] = (byte)(c.Length % 256);
        for (int i = 0; i < c.Length; i++)
        {
            buffer[(i + 1) * 2 + 1] = (byte)(((ushort)c[i]) / 256);
            buffer[(i + 1) * 2 + 2] = (byte)(((ushort)c[i]) % 256);
        }
        // Get a stream object for writing. 			
        NetworkStream stream = socketConnection.GetStream();
        if (stream.CanWrite)
            stream.Write(buffer, 0, buffer.Length);
        Debug.Log("added to stream " + stream.CanWrite + " " + buffer.Length);
    }
}
