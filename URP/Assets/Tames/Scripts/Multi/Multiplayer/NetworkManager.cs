using RiptideNetworking;
using RiptideNetworking.Utils;
using System;
using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
using UnityEditor;
#endif
public class NetworkManager : MonoBehaviour
{
    private static bool Registering = false;
    public static string RegisterEmail;
    public static string RegisterID;
    public static string Token;
    private static NetworkManager _singleton;
    public static NetworkManager Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
                _singleton = value;
            else if (_singleton != value)
            {
                Debug.Log($"{nameof(NetworkManager)} instance already exists, destroying duplicate!");
                Destroy(value);
            }
        }
    }
    public static void EditorConnection(NetworkManager manager, string address, string port)
    {
#if UNITY_EDITOR
        Debug.Log("connect pressed " + (Singleton == null));
        if (Singleton == null) Singleton = manager;

        Registering = true;
        if (Singleton.Client == null)
            Singleton.CreateClient();
        if (Singleton.Client.IsConnected)
        {
            Debug.Log("already connected");
            Registering = false;
            Player.SendRegister();
     //       Singleton = null;
        }
        else
        {
            Singleton.Connect(address, port);
            EditorCoroutineUtility.StartCoroutine(TickUntil(), Singleton);
        }
#endif
    }
 #if UNITY_EDITOR
    public static IEnumerator TickUntil()
    {
        var waitForOneSecond = new EditorWaitForSeconds(1f);

        int counter = 0;
        while (true)
        {
            if (Singleton.Client.IsConnected)
            {
                Debug.Log("connection approved");
                Player.SendRegister();
                yield break;
            }
            else
            {
                counter++;
                if(counter == 10)yield break;
                Debug.Log("waiting for connection");
                yield return waitForOneSecond;
            }
        }
    }
#endif
    public Client Client { get; private set; }
    public Server Server { get; private set; }

    //   public static string commandIP;
    //   [SerializeField] private string ip;
    //   [SerializeField] private ushort port;

    private void Awake()
    {
        Singleton = this;
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);
        //  Debug.Log("starete");
        if (CoreTame.IsServer)
        {
            Server = new Server();
            Server.ClientDisconnected += PlayerLeft;
            Server.Start(ushort.Parse(CoreTame.Port), 32);
        }
        else if (CoreTame.multiPlayer)
        {
            CreateClient();
        }
    }
    private void CreateClient()
    {
        Client = new Client();
        Client.Connected += DidConnect;
        Client.ConnectionFailed += FailedToConnect;
        Client.ClientDisconnected += PlayerLeft;
        Client.Disconnected += DidDisconnect;
        Debug.Log("client created");
    }
    private void FixedUpdate()
    {
        if (CoreTame.multiPlayer)
        {
            if (CoreTame.IsServer) Server.Tick();
            else if (Client!=null) Client.Tick();
        }
    }

    private void OnApplicationQuit()
    {
        if (CoreTame.multiPlayer)
        {
            if (!CoreTame.IsServer)
                Client.Disconnect();
            else
                Server.Stop();
        }
    }

    public void Connect()
    {
        Connect(CoreTame.Address, CoreTame.Port);
    }

    public void Connect(string address, string port)
    {
        Debug.Log("trying to connect " + (Client == null));
        if (port != "")

            Client.Connect($"{address}:{port}");
        else
            Client.Connect($"{address}");
        Client.Tick();
    }
    private void DidConnect(object sender, EventArgs e)
    {
        if (Registering)
        {
            Debug.Log("connected");
            Registering = false;
            Player.SendRegister();
            Singleton = null;
        }
        else
        {
            Player.SendName();
            Debug.Log("NM: connected");
        }
    }

    private void FailedToConnect(object sender, EventArgs e)
    {
        CoreTame.multiPlayer = false;
        CoreTame.loadStatus = CoreTame.LoadStatus.ConnectionChecked;
        Debug.Log("failed to connect. Solo user activated");
    }

    private void PlayerLeft(object sender, ClientDisconnectedEventArgs e)
    {
        Debug.Log("disconnected: " + e.Id);
        Player.Disconnect(e.Id);
        //        Server.
    }

    private void DidDisconnect(object sender, EventArgs e)
    {
        Debug.Log("NM: disconnected");

    }
}
