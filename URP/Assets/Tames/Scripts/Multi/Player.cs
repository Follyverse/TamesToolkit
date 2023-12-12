using Multi;
using RiptideNetworking;
using System;
using System.Collections.Generic;
using Tames;
using UnityEngine;
using UnityEngine.InputSystem;


public class Player : MonoBehaviour
{
    //  public static Records.FrameShot[] frames = new Records.FrameShot[CoreTame.people.Length];
    public static ushort LocalID = ushort.MaxValue - 1;
    public static ushort ServerID = ushort.MaxValue - 2;
    public static ushort NoID = ushort.MaxValue - 3;
    public static RemoteProject project;
    public static List<PersonClient> users = new List<PersonClient>();

    public const ushort Name = 255;

    public const ushort S_FrameData = 1;
    public const ushort C_FrameData = 101;
    public const ushort S_ID = 2;
    public const ushort C_ID = 102;
    public const ushort S_Disconnect = 3;
    public const ushort C_Disconnect = 103;
    public const ushort S_Capacity = 4;
    public const ushort S_ReadyToGo = 5;
    public const ushort C_ReadyToGo = 105;
    public const ushort C_EmailToOwner = 6;
    public const ushort C_Properties = 107;
    public const ushort S_Properties = 7;
    public const ushort C_Nickname = 108;
    public const ushort S_Nickname = 8;
    public const ushort C_RequestStatus = 109;
    public const ushort C_SendStatus = 110;
    public const ushort S_RequestStatus = 9;
    public const ushort S_SendStatus = 10;
    public const ushort C_Register = 120;
    /// <summary>
    /// Finds a Person by its network ID. This is only used when <see cref="CoreTame.IsServer"/> is true.
    /// </summary>
    /// <param name="id">The requested ID</param>
    /// <param name="index">The index of that person in <see cref="users"/></param>
    /// <returns>The found Person or null</returns>
    private static PersonClient FindByID(ushort id, out int index)
    {
        index = -1;
        for (int i = 0; i < users.Count; i++)
            if (id == users[i].id)
            {
                index = i;
                return users[i];
            }
        return null;
    }
    #region Client
    /// <summary>
    /// Receives the ID of the local person in the server"/>
    /// </summary>
    /// <param name="mr"></param>
    public static void SendName()
    {
        Message message = Message.Create(MessageSendMode.reliable, Name);
        //        message.AddString(usernameField.text);
        NetworkManager.Singleton.Client.Send(message);
    }
    [MessageHandler(S_ID)]
    private static void ReceiveID(Message mr)
    {
        if (CoreTame.IsServer) return;
        ushort Id = mr.GetUShort();
        Person.people[0].id = Id;
        Debug.Log("Received " + Id);
        SendProperties();
    }
    /// <summary>
    /// Sends the Project properties of this person, so that the server can assign a project to this client.
    /// </summary>
    public static void SendProperties()
    {
        if (CoreTame.IsServer) return;
        Message m = Message.Create(MessageSendMode.reliable, C_Properties);
        //      m.AddUShort(Person.people[0].id);
        m.AddString(TameManager.publish.id);
        m.AddString(TameManager.publish.title);
        m.AddString(CoreTame.Nickname);
        m.AddString(TameManager.publish.email);
        NetworkManager.Singleton.Client.Send(m);
    }
    /// <summary>
    /// Server asks this client to disconnect
    /// </summary>
    /// <param name="mr"></param>
    [MessageHandler(S_Disconnect)]
    private static void ReceiveDisconnect(Message mr)
    {
        if (CoreTame.IsServer) return;
        ushort id = mr.GetUShort();
        for (int i = 1; i < Person.people.Count; i++)
            if (Person.people[i].id == id)
            {
                Person.people.RemoveAt(i);
                break;
            }
    }
    /// <summary>
    /// Server asks this client to disconnect because the server is full.
    /// </summary>
    /// <param name="mr"></param>
    [MessageHandler(S_Capacity)]
    private static void ReceiveCapacity(Message mr)
    {
        if (CoreTame.IsServer) return;
        Debug.Log("Capacity");
        NetworkManager.Singleton.Client.Disconnect();
    }
    /// <summary>
    /// Receives the nickname of a person. This iasds received in response to <see cref="AskNickname(ushort)"/> 
    /// </summary>
    /// <param name="m"></param>
    [MessageHandler(S_Properties)]
    private static void ReceiveProperties(Message m)
    {
        if (CoreTame.IsServer) return;
        ushort id = m.GetUShort();
        string nickname = m.GetString();
        Person p = Person.Find(id);
        if (p == null)
            p = Person.Add(id);
        p.nickname = nickname;
        Debug.Log("Props " + id + " " + nickname);
    }
    /// <summary>
    /// The server asks for the current status of dynamic and altering elements in the project, so to keep a newly joint client updated.
    /// </summary>
    /// <param name="msg"></param>
    [MessageHandler(S_RequestStatus)]
    private static void SendProgress(Message msg)
    {
        if (CoreTame.IsServer) return;
        Debug.Log("Request ");
        SendProgress(msg.GetUShort(), false);

    }
    /// <summary>
    /// The current status of the project sent by the server. See <see cref="SendProgress(Message)"/>
    /// </summary>
    /// <param name="m"></param>
    [MessageHandler(S_SendStatus)]
    private static void ReceiveStatus(Message m)
    {
        if (CoreTame.IsServer) return;
        int cf = m.GetInt();
        int ci = m.GetInt();
        Debug.Log("SendStat ");
        for (int i = 0; i < cf; i++)
            TameManager.tes[i].progress.GetStatus(m);
        int ca = TameManager.altering.Count;
        int cm = TameManager.alteringMaterial.Count;
        for (int i = 0; i < ca; i++)
            TameManager.altering[i].GetStatus(m);
        for (int i = 0; i < cm; i++)
            TameManager.alteringMaterial[i].GetStatus(m);
        SendReady();
    }
    /// <summary>
    /// The server allows this client to join and send frame updates. This is received either after <see cref="SendReady"/> or if the client is the first to join (and create) a project on the server.
    /// </summary>
    /// <param name="m"></param>
    [MessageHandler(S_ReadyToGo)]
    private static void ReadyToGo(Message m)
    {
        if (CoreTame.IsServer) return;
        Debug.Log("Ready to Go ");
        CoreTame.loadStatus = CoreTame.LoadStatus.ConnectionChecked;
    }
    /// <summary>
    /// Tells the server that this client has received updates and is ready.
    /// </summary>
    static void SendReady()
    {
        Message m = Message.Create(MessageSendMode.reliable, C_ReadyToGo);
        //  m.AddUShort(Person.people[0].id);
        NetworkManager.Singleton.Client.Send(m);
    }
    /// <summary>
    /// As a client, sends changes in the inputs and positions in the current frame to the server.
    /// </summary>
    /// <param name="m">The message containing the frame, created in <see cref="Person.SendFrameAsClient(Records.FrameShot, Records.FrameShot)"/></param>
    public static void SendFrame(Message m)
    {
        NetworkManager.Singleton.Client.Send(m);
    }
    /// <summary>
    /// As a client, receives all changes in the frames of all other clients.
    /// </summary>
    /// <param name="m"></param>
    [MessageHandler(S_FrameData)]
    private static void ReceiveFrame(Message m)
    {
        if (CoreTame.IsServer) return;
        //    ushort id = m.GetUShort();
        int userCount = m.GetByte();
        //  Debug.Log("Frame receive " + n);
        for (int i = 0; i < userCount; i++)
            try
            {
                ushort id = m.GetUShort();
                if (id != NoID)
                {
                    if (id == CoreTame.localPerson.id) PersonClient.FakeReceive(m);
                    else
                    {
                        Person p = Person.Find(id);
                        if (p == null)
                        {
                            Debug.Log("not found " + id);
                            p = Person.Add(id);
                            AskNickname(id);
                        }
                        p.ReceiveFrameAsClient(m);
                    }
                }
            }
            catch (Exception e)
            {
            }

        //       Person.UpdateAll(frames);
    }

    /// <summary>
    /// Asks the server for characterisitcs of a newly joined person.
    /// </summary>
    /// <param name="id"></param>
    static void AskNickname(ushort id)
    {
        Message m = Message.Create(MessageSendMode.reliable, C_Nickname);
        Debug.Log("send request " + id);
        m.AddUShort(id);
        NetworkManager.Singleton.Client.Send(m);
    }
    /// <summary>
    /// Receives the characteristics of a newly joined person in response to <see cref="AskNickname(ushort)"/>
    /// </summary>
    /// <param name="m"></param>
    [MessageHandler(S_Nickname)]
    private static void ReceiveNickname(Message m)
    {
        if (CoreTame.IsServer) return;
        ushort id = m.GetUShort();
        Person p = Person.Find(id);
        Debug.Log("Nickname ");
        if (p != null)
            p.nickname = m.GetString();
    }
    #endregion
    #region Server -----------------------------------------------------------
    /// <summary>
    /// Receives connection request from a client, including its ID
    /// </summary>
    /// <param name="m"></param>
    [MessageHandler(Name)]
    private static void PersonConnected(ushort id, Message m)
    {
        Debug.Log("Name " + id);
        if (!CoreTame.IsServer) return;
        //ushort id = m.GetUShort();
        SendID(id);
    }
    /// <summary>
    /// It is not used in this (LAN) server 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="s"></param>
    public static void ReachedCapacity(ushort id, string s)
    {
        Debug.Log("capac ");
        Message m = Message.Create(MessageSendMode.reliable, S_Capacity);
        m.AddString(s);
        NetworkManager.Singleton.Server.Send(m, id);
    }
    /// <summary>
    /// Sends back the clients ID to it, to confirm communication (see <see cref="ReceiveID(Message)"/>)
    /// </summary>
    /// <param name="id"></param>
    private static void SendID(ushort id)
    {
        Message m = Message.Create(MessageSendMode.reliable, S_ID);
        m.AddUShort((ushort)id);
        Debug.Log("id sent " + id);
        NetworkManager.Singleton.Server.Send(m, id, false);
    }
    /// <summary>
    /// Receives the properties of a client after sending its ID by <see cref="SendID(ushort)"/>. The client is added to the <see cref="users"/> base (but not the <see cref="project"/> yet).
    /// </summary>
    /// <param name="m"></param>
    [MessageHandler(C_Properties)]
    private static void GetProperties(ushort id, Message m)
    {
        if (!CoreTame.IsServer) return;
        //    ushort id = m.GetUShort();
        m.GetString();
        m.GetString();
        string n = m.GetString();
        m.GetString();

        //   if (project.recipient == "") project.recipient = mail;
        PersonClient person = new PersonClient(id) { connection = DateTime.Now, nickname = n };
        users.Add(person);
        //    person.person = Person.Add(id);

        Debug.Log("Props: " + id + " " + n);
        SendProgress(id, false);
    }
    /// <summary>
    /// After receiving the client's characteristics by <see cref="GetProperties(Message)"/> it sends the current status of the project's dynamic and altering elements. 
    /// </summary>
    /// <param name="id"></param>
    public static void SendProgress(ushort id, bool asServer)
    {
        Message m;
        if (asServer)
            m = Message.Create(MessageSendMode.reliable, S_SendStatus);
        else
        {
            m = Message.Create(MessageSendMode.reliable, C_SendStatus);
            m.AddUShort(id);
        }

        int cf = TameManager.tes.Count;
        int ca = TameManager.altering.Count;
        int cm = TameManager.alteringMaterial.Count;
        m.AddInt(cf);
        m.AddInt(ca + cm);
        for (int i = 0; i < cf; i++)
            TameManager.tes[i].progress.AddStatus(m);
        for (int i = 0; i < ca; i++)
            TameManager.altering[i].AddStatus(m);
        for (int i = 0; i < cm; i++)
            TameManager.alteringMaterial[i].AddStatus(m);

        if (asServer) NetworkManager.Singleton.Server.Send(m, id); else NetworkManager.Singleton.Client.Send(m);
        Debug.Log("progress " + cf + " " + ca + " " + id);
        //   NetworkManager.Singleton.Client.
    }
    /// <summary>
    /// After receiving an updating the project, the client proclaims readiness, so it is added to the <see cref="project"/>.
    /// </summary>
    /// <param name="m"></param>
    [MessageHandler(C_ReadyToGo)]
    private static void ReceiveReady(ushort id, Message m)
    {
        if (!CoreTame.IsServer) return;
        Debug.Log("Ready ");
        PersonClient person = FindByID(id, out int index);
        if (person != null)
        {
            project.users.Add(person);
            person.person = Person.Add(id);
            person.person.client = person;
            SendReady(id);
        }
    }
    public static void SendReady(ushort id)
    {
        Debug.Log("Ready ");
        Message m = Message.Create(MessageSendMode.reliable, S_ReadyToGo);
        NetworkManager.Singleton.Server.Send(m, id);
    }
    /// <summary>
    /// Sends to all other clients that a client is disconnected
    /// </summary>
    /// <param name="project"></param>
    /// <param name="id"></param>
    public static void SendDisconnect(RemoteProject project, ushort id)
    {
        Message m;
        foreach (PersonClient person in project.users)
            if (person.id != ServerID)
            {
                m = Message.Create(MessageSendMode.reliable, S_Disconnect);
                m.AddUShort(id);
                NetworkManager.Singleton.Server.Send(m, person.id);
            }
    }
    /// <summary>
    /// As server, receives the frame updates from a client.
    /// </summary>
    /// <param name="m"></param>
    [MessageHandler(C_FrameData)]
    private static void ReceiveFrameAsServer(ushort id, Message m)
    {
        if (!CoreTame.IsServer) return;
        //    ushort id = m.GetUShort();
        PersonClient person = project.FindByID(id, out int index);
        //      Debug.Log("Frame " + id);
        //    return;
        if (person != null)
            person.RecevieFrameAsServer(m);
    }

    [MessageHandler(C_Nickname)]
    private static void ReceiveNicknameRequest(ushort sender, Message m)
    {
        if (!CoreTame.IsServer) return;
        //   ushort sender = m.GetUShort();
        ushort reqID = m.GetUShort();
        Debug.Log("Mickname " + sender + " " + reqID);
        PersonClient person = project.FindByID(reqID, out int index);
        if (person != null)
        {
            Message reply = Message.Create(MessageSendMode.reliable, S_Nickname);
            reply.Add(reqID);
            reply.Add(person.nickname);
            NetworkManager.Singleton.Server.Send(reply, sender);
        }
    }
    /// <summary>
    /// Receives the disconnection message from a client
    /// </summary>
    /// <param name="m"></param>
    public static void Disconnect(ushort id)
    {
        if (!CoreTame.IsServer) return;
        //   ushort id = m.GetUShort();
        PersonClient person = project.FindByID(id, out int index);
        if (person != null)
        {
            project.Disconnect(person);
            users[id] = null;
        }
    }

    [MessageHandler(C_EmailToOwner)]
    private static void SendMail(ushort id, Message m)
    {
        if (!CoreTame.IsServer) return;
        List<string> lines = new List<string>();
        //   ushort id = m.GetUShort();
        PersonClient person = project.FindByID(id, out int index);
        if (person != null)
        {
            string mail = m.GetString();
            string[] body = new string[] { m.GetString() };
            SaveResults(mail, body);
        }
    }
    #endregion
    private static void SaveResults(string mail, string[] body)
    {
        DateTime now = DateTime.Now;
        string dt = now.ToString(TameManager.publish.id + " yyyy-MM-dd HH-mm-ss-" + now.Millisecond);
        string path = Application.dataPath;
        path = path.Substring(0, path.Length - 1);
        int slash = Mathf.Max(path.LastIndexOf("/"), path.LastIndexOf('\\'));
        path = path.Substring(0, slash + 1);
        System.IO.File.WriteAllLines(path + dt + ".txt", body);
    }
    public static void SendRegister()
    {
        Debug.Log("sending register");
        Message m = Message.Create(MessageSendMode.reliable, C_Register);
        m.AddString(NetworkManager.RegisterID);
        m.AddString(NetworkManager.RegisterEmail);
        m.AddString(NetworkManager.Token);
        NetworkManager.Singleton.Client.Send(m);
    }
}
