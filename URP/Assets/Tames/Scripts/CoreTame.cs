using HandAsset;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using Multi;
using System;
using Tames;
using Records;
using System.IO;
using System.Reflection;
using UnityEngine.XR.Management;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

public class CoreTame : MonoBehaviour
{
    public const int TamesVersion = 1000;

    public static bool VRMode;
    public static Vector2Int VRSize;
    public static GameObject VRAnchor;
    public static Light Sun;
    public static bool multiPlayer = false;
    public static GameObject HeadObject;
    public static List<Person> people;
    public static ITameEffect[] ies;
    public static List<TameElement> tes;
    //  List<Person> people;
    public static Person localPerson;
    public static Markers.ExportOption exportOption;
    public static int WheelDirection;
    public static int MouseButton;
    // objects
    public static Camera mainCamera;
    //  public Text text;
    public static Vector2Int screenSize;
    // mirror
    // door
    // timing
    public TMPro.TextMeshProUGUI uitext;
    float averageFPS;
    int counter;
    Quaternion rotationDefault;
    Vector3 positionDefault;
    public static HandModel[] hand;
    Vector3 handAng = Vector3.zero;
    TameManager manager;
    //  string audioFolder;
    private bool savingTexture = false;
    private TameArea closestGrip;
    private TameObject grippedObject = null;
    public static string fingerHeader = "finger";
    public static bool replayMode = false;
    public static GameObject torch;
    public RenderTexture renderTexture;
    public static Material baseCanvasMaterial;
    Vector2Int lastScreen;
    VoiceCommands voiceCommand;
    public static Vector3 lastPosition = Vector3.zero;
    public static Quaternion lastRotation = Quaternion.identity;
    public enum LoadStatus { Begun, XRChecked, AddressableLoaded, GettingPassword, LoadingScene, Connecting, ConnectionChecked, Ready }
    public static LoadStatus loadStatus = LoadStatus.Begun;
    static bool FirstInstance = false;
    public static bool IsWebGL = false;
    void Start()
    {
        TCPClient.publish = gameObject.GetComponent<Markers.PublishProject>();
#if UNITY_WEBGL
        IsWebGL = true;
#endif
        if (FirstInstance) return;
        else FirstInstance = true;
        ProcessArgs();
        DLCManager.GetBundleFolder();
        if (ProjectID == "") ProjectID = TCPClient.publish.id;
        if (Address == "") Address = TCPClient.publish.serverIP;
        if (Port == "") Port = Address = TCPClient.publish.serverPort;
        if (AltPort == "") AltPort = Address = TCPClient.publish.altPort;

        if (IsWebGL)
        {
            VRMode = false;
            multiPlayer = false;
            loadStatus = LoadStatus.ConnectionChecked;
        }
        else
            StartCoroutine(CheckXR());
        //    loadStatus = LoadStatus.XRChecked;
    }
    string actlod = "";
    float xrCheckBegan = 0;
    bool xrCheckExpired = false;
    public IEnumerator CheckXR()
    {
        xrCheckBegan = Time.time;
        Debug.Log("Initializing...");
        yield return XRGeneralSettings.Instance.Manager.InitializeLoader();

        if (XRGeneralSettings.Instance.Manager.activeLoader == null)
        {
            Debug.Log("Initializing XR Failed. Check Editor or Player log for details.");
            if (!xrCheckExpired)
            {
                VRMode = false;
                loadStatus = LoadStatus.XRChecked;
            }
        }
        else if (XRGeneralSettings.Instance.Manager.activeLoader.name.Length > 1)
        {
            actlod = XRGeneralSettings.Instance.Manager.activeLoader.name + " - ";
            Debug.Log("Starting XR...");
            if (!xrCheckExpired)
            {
                XRGeneralSettings.Instance.Manager.StartSubsystems();
                VRMode = true;
                VRSize = new Vector2Int(1, 1);
                loadStatus = LoadStatus.XRChecked;
            }
        }
        else
        {
            actlod = " x ";
            if (!xrCheckExpired)
            {
                VRMode = false;
                loadStatus = LoadStatus.XRChecked;
            }
        }

    }
    void LoadManager()
    {
        counter = 0;
        Debug.Log("start success");
        Cursor.visible = false;
        LoadLibraries();
        Utils.SetPipelineLogics();
        Utils.SetPOCO();
        //   panel.SetActive(false);
        mainCamera = Camera.main;
        renderTexture = new RenderTexture(Screen.width * 3, Screen.height * 3, 24);
        //   rendCam.targetTexture = renderTexure;
        //   rendCam.enabled = false;
        Transform t = mainCamera.transform;
        VRAnchor = new GameObject("vranchor");
        VRAnchor.transform.parent = t;
        VRAnchor.transform.localPosition = Vector3.forward;
        while (t != null)
        {
            TameCamera.cameraTransform = t;
            t = t.parent;
        }
        TameCamera.camera = mainCamera;
        lastScreen = screenSize = new Vector2Int(Screen.width, Screen.height);
        PrepareLoadScene();
        TameCamera.ZKey = TameInputControl.FindKey("z", false);
        TameCamera.XKey = TameInputControl.FindKey("x", false);
        TameCamera.CKey = TameInputControl.FindKey("c", false);
        //   Debug.Log(TameCamera.ZKey + " " + TameCamera.XKey + " " + TameCamera.CKey);
        TameInputControl.FindKey("n-");
        TameInputControl.FindKey("n+");

        //   text.text = messages[0];
        people = Person.people;
        // audioFolder = "Audio/";

        counter = -1;
        averageFPS = 0;
        ManifestKeys.SetKeywords();
        manager = new TameManager();
        manager.Initialize();
        if (TameManager.settings != null)
            torch = TameManager.settings.torch;
        tes = TameManager.tes;
        if (TameManager.settings != null)
            replayMode = TameManager.settings.replay;

        Updater.AllUpdaters = new Updater[TameManager.tes.Count];
        ies = ITameEffect.AllEffects = new ITameEffect[TameManager.tes.Count];
        ITameEffect.Initialize();
        //  text.text = "";

        Person.localPerson = localPerson = Person.Add(ushort.MaxValue);
        TameInputControl.keyMap = localPerson.keyMap;
        foreach (Markers.MarkerTeleport mt in TameManager.teleport)
            if (mt.randomInitial)
            {
                Debug.Log("tep " + mt.name);
                mt.Random();
                break;
            }
        //  ProcessArgs(new string[] { "address:194.32.79.172", "port:7777" });
        //          ProcessArgs(new string[] { "address:127.0.0.1", "port:7777" });
        //     ProcessArgs(new string[] { "server", "port:7777" });
        if (IsServer)
        {
            //      ReadyToGo = true;
            Player.project = new RemoteProject()
            {
                users = new List<PersonClient>() { new PersonClient(Player.ServerID) { person = localPerson } }
            };
            Player.users.Add(Player.project.users[0]);
            localPerson.client = Player.project.users[0];
        }
        //    if (!multiPlayer) ReadyToGo = true;
        TameElement.isPaused = true;
        if (!IsWebGL)
        {
            voiceCommand = new VoiceCommands();
            voiceCommand.Initialize();
        }//    Debug.Log("cam " + TameCamera.cameraTransform.position.ToString());
        Time.fixedDeltaTime = 0.02f;

    }
    public static bool gettingPassword = false;
    public static bool passwordMatched = false;
    public void ShowPasswordDialog()
    {
        gettingPassword = true;
        TameElement.isPaused = true;

    }
    public static string Address = "";
    public static string Port = "";
    public static string AltPort = "";
    public static bool IsServer = false;
    public static string Nickname = "";
    public static string ProjectID = "";
    public static string ProjectPath = "";
    enum Arguments { None, Address, Port, Server, Password, Default, Name, ID, Path, Offline }
    void ProcessArgs(string[] a = null)
    {
        var args = a ?? Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            ProcessArg(args[i], out Arguments type, out string value);

            switch (type)
            {
                case Arguments.Offline:
                    multiPlayer = false;
                    AltPort = "";
                    break;
                case Arguments.Address:
                    Address = value;
                    multiPlayer = true;
                    break;
                case Arguments.Port:
                    string[] s = value.Split(',');
                    if (s.Length > 1)
                    {
                        Port = s[0];
                        AltPort = s[1];
                    }
                    else
                        Port = AltPort = value;
                    break;
                case Arguments.Server:
                    IsServer = true;
                    multiPlayer = true;
                    break;
                case Arguments.Default:
                    multiPlayer = true;
                    break;
                case Arguments.Name: Nickname = value; break;
                case Arguments.ID: ProjectID = value; break;
                case Arguments.Path: ProjectPath = value; break;
            }
        }
    }
    private static void ProcessArg(string arg, out Arguments type, out string value)
    {
        type = Arguments.None;
        value = "";
        int p = arg.IndexOf(":");
        if (p > 0)
        {
            string pre = arg.Substring(0, p);
            if (pre == "address")
            { type = Arguments.Address; value = arg.Substring(p + 1); }
            else if (pre == "port")
            {
                type = Arguments.Port; value = arg.Substring(p + 1);
            }
            else if (pre == "password")
            {
                type = Arguments.Password; value = arg.Substring(p + 1);
            }
            else if (pre == "id")
            { type = Arguments.ID; value = arg.Substring(p + 1); }
            else if (pre == "name") { type = Arguments.Name; value = arg.Substring(p + 1); }
            else if (pre == "path") { type = Arguments.Path; value = arg.Substring(p + 1).Replace("*", " "); Debug.Log("path " + value); }
        }
        else if (arg == "server") type = Arguments.Server;
        else if (arg == "address") type = Arguments.Default;


    }
    void LoadLibraries()
    {
        string activePipeline = "";

#if UNITY_2019_3_OR_NEWER
        if (GraphicsSettings.renderPipelineAsset != null)
        {
            activePipeline = GraphicsSettings.renderPipelineAsset.GetType().Name;
        }
        else
        {
            activePipeline = "Built-in Render Pipeline";
        }
#else
        activePipeline = "Built-in Render Pipeline";
#endif
        if (activePipeline == "HDRenderPipelineAsset")
            TameMaterial.Pipeline = 1;

    }
    void PrepareLoadScene()
    {
        baseCanvasMaterial = (Material)Resources.Load("Tames models\\BaseCanvasMaterial");
        Utils.rig = TameCamera.cameraTransform.gameObject;
        XRController xrcl = null, xrcr = null;
        XRController[] all = Utils.rig.GetComponentsInChildren<XRController>();
        if (all.Length > 0)
            for (int i = 0; i < all.Length; i++)
                if (all[i].controllerNode == XRNode.LeftHand) xrcl = all[i];
                else if (all[i].controllerNode == XRNode.RightHand) xrcr = all[i];
        GameObject go = (GameObject)Resources.Load("Tames models\\leftHand");
        Utils.left = GameObject.Instantiate(go);
        go = (GameObject)Resources.Load("Tames models\\rightHand");
        Utils.right = GameObject.Instantiate(go);
        if (xrcl == null)
        {
            xrcl = Utils.left.AddComponent<XRController>();
            xrcl.controllerNode = XRNode.LeftHand;
        }
        if (xrcr == null)
        {
            xrcr = Utils.right.AddComponent<XRController>();
            xrcr.controllerNode = XRNode.RightHand;
        }
        go = (GameObject)Resources.Load("Tames models\\external head");
        HeadObject = Utils.head = GameObject.Instantiate(go);
        hand = Utils.Inputs(xrcl, xrcr, fingerHeader);
    }

    void FixedUpdate()
    {//if (FirstInstance) return;
        if (!replayMode)
            CheckUpdate();
        //      else             UpdateReplay();
    }
    public static int lastFrameIndex = -1;
    public FrameShot lastFrameShot = null;

    void CheckUpdate()
    {
        if (loadStatus != LoadStatus.Ready)
        {
            switch (loadStatus)
            {
                case LoadStatus.Begun:
                    if (Time.time - xrCheckBegan > 3)
                    {
                        xrCheckExpired = true;
                        loadStatus = LoadStatus.LoadingScene;
                        Time.fixedDeltaTime = 1;
                        DLCManager.GetDefaultObjects();
                        DLCManager.LoadScene(this);
                    }
                    break;
                case LoadStatus.XRChecked:
                    uitext.text = actlod + " XR device " + (VRMode ? " detected." : " not detected.");
                    loadStatus = LoadStatus.LoadingScene;
                    Time.fixedDeltaTime = 1;
                    DLCManager.GetDefaultObjects();
                    DLCManager.LoadScene(this);
                    break;
                case LoadStatus.LoadingScene:
                    uitext.text = actlod + " Loading scene ... " + counter;
                    counter++;
                    Debug.Log("Loading scene");
                    return;
                case LoadStatus.AddressableLoaded:
                    uitext.text = actlod + " Scene loading is finished.";
                    Debug.Log("Scene loaded");
                    LoadManager();
                    Connect(); break;
                case LoadStatus.Connecting:
                    uitext.text = actlod + " Connecting ... " + counter;
                    counter++;
                    //             Debug.Log("Connecting");
                    return;
                case LoadStatus.ConnectionChecked:
                    uitext.text = actlod + " " + (!multiPlayer ? "Not using network" : (IsServer ? " Server running is " + NetworkManager.Singleton.Server.IsRunning : " Client connection is " + NetworkManager.Singleton.Client.IsConnected));
                    Debug.Log("Connection checked");
                    loadStatus = LoadStatus.Ready;
                    TameElement.isPaused = false;
                    counter = 0;
                    break;
            }
        }
        else UpdateSolo();
    }


    void Connect()
    {
        if (gettingPassword)
        {
            if (passwordMatched)
            {
                TameElement.isPaused = false;
                gettingPassword = false;
                if (multiPlayer & !IsServer) NetworkManager.Singleton.Connect();
            }
        }
        else
        {
            Debug.Log("Connection started");

            if (multiPlayer & !IsServer)
            {
                loadStatus = LoadStatus.Connecting;
                NetworkManager.Singleton.Connect();
            }
            else
            {
                TameElement.isPaused = false;
                loadStatus = LoadStatus.ConnectionChecked;
            }
        }
    }
    void UpdateSolo()
    {
        if (counter < 200)
        {
            counter++;
        }
        else if (counter == 200)
        {
            counter++;
            uitext.transform.parent.gameObject.SetActive(false);
            Debug.Log("tame paused = " + TameElement.isPaused);
        }


        TameElement.PassTime();
        ///   Debug.Log("OK?");
        if (!TameElement.isPaused)
        {
            //     Debug.Log("hmm?");
            //       Debug.Log("why?");
            if (VRMode)
            {
                localPerson.Update();
                localPerson.EncodeLocal();
            }
            else
            {
                localPerson.UpdateHeadOnly();
            }
            //  inputRecords.Clear();
            FrameShot f = CheckInput();

            for (int i = 0; i < TameManager.tes.Count; i++)
                tes[i].CheckParentChange();
            TameArea.CheckInteraction(TameManager.area);
            int n = TameElement.GetAllParents(Updater.AllUpdaters, tes);
            for (int i = 0; i < n; i++)
                Updater.AllUpdaters[i].Apply();

            for (int i = 0; i < TameManager.altering.Count; i++)
                TameManager.altering[i].Update();
            for (int i = 0; i < TameManager.alteringMaterial.Count; i++)
                TameManager.alteringMaterial[i].Update();
            TameManager.UpdateScores();
            foreach (Markers.MarkerTeleport mt in TameManager.teleport) mt.Check();
            TameCamera.UpdateCamera();
            TameManager.UpdatePeoploids();
            if (ResolutionChanged()) TameManager.RecalculateInfo();
            if (TameManager.settings != null)
                if (TameManager.settings.sendBy.mono.Count > 0)
                    foreach (TameInputControl tci in TameManager.settings.sendBy.mono)
                        if (tci.Pressed())
                        {
                            SendEmail();
                            break;
                        }

            f.GetSpatial(localPerson);
            if (IsServer) localPerson.client.SetChanged(lastFrameShot, f);

            lastPosition = mainCamera.transform.position;
            lastRotation = mainCamera.transform.rotation;
            for (int i = 0; i < 2; i++)
            {
                localPerson.lastPosition[i] = localPerson.position[i];
                localPerson.lastRotation[i] = localPerson.rotation[i];
            }
            if (multiPlayer)
            {
                if (IsServer)
                {
                    Player.project.users[0].SendFrameAsServer();
                    //       Player.project.SendUpdate();
                }
                else localPerson.SendFrameAsClient(lastFrameShot, f);
            }
            lastFrameShot = f;
            FlushPressed();
            // if (inputRecords.Count > 0) Player.SendRecord(inputRecords);

        }
        else
        {

        }
    }
    float changedTime = 0;
    bool ResolutionChanged()
    {
        if (lastScreen.x != Screen.width || lastScreen.y != Screen.height)
        {
            changedTime = Time.time;
            lastScreen = new Vector2Int(Screen.width, Screen.height);
        }
        else if (lastScreen.x != screenSize.x || lastScreen.y != screenSize.y)
            if (Time.time - changedTime > 1)
            {
                screenSize = new Vector2Int(Screen.width, Screen.height);
                return true;
            }
        return false;
    }
    void FlushPressed()
    {
        for (int i = 1; i < people.Count; i++)
            if (people[i] != null)
            {
                people[i].keyMap.UPressed = people[i].keyMap.gpMap.UPressed = people[i].keyMap.vrMap.UPressed = 0;
                //      Player.frames[i].KBPressed = Player.frames[i].GPPressed = Player.frames[i].VRPressed = 0;
            }
    }
    // Update is called once per frame
    IEnumerator SavePNG()
    {
        // We should only read the screen buffer after rendering is complete
        yield return new WaitForEndOfFrame();

        RenderTexture.active = renderTexture;
        // Create a texture the size of the screen, RGB24 format
        int width = renderTexture.width;
        int height = renderTexture.height;
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);

        // Read screen contents into the texture
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        // Encode texture into PNG
        byte[] bytes = tex.EncodeToPNG();
        Destroy(tex);

        // For testing purposes, also write to a file in the project folder
        File.WriteAllBytes(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) + "\\" + DateTime.Now.ToString("yyyy.MM.dd HH.mm.ss") + ".png", bytes);
        TameElement.isPaused = false;

    }
    private FrameShot CheckInput(int index = -1)
    {
        for (int i = 1; i < people.Count; i++)
            people[i].FlushKeyMap();
        TameKeyMap km = TameInputControl.CheckKeys(index);
        TameInputControl.ChoiceHoover();
        //   if (km.UPressed != 0) Debug.Log("pressed != 0" +" "+TameElement.Tick);
        //  inputRecords.AddRange(InputRecord.FromKeyMap(km));
        if (Keyboard.current.f2Key.wasPressedThisFrame)
        {
            //   TameElement.isPaused = true;
            mainCamera.targetTexture = renderTexture;
            mainCamera.Render();
            StartCoroutine(SavePNG());
            mainCamera.targetTexture = null;
        }
        TameFullRecord.allRecords.Capture(TameElement.ActiveTime, index < 0 ? null : km);
        if (Keyboard.current.escapeKey.isPressed)
        {
            Application.Quit();
        }

        string path;
        if (Keyboard.current.ctrlKey.isPressed && Keyboard.current.sKey.wasPressedThisFrame)
        {
            DateTime now = DateTime.Now;
            bool saved = false;
#if UNITY_EDITOR
            if (exportOption != null)
                if (exportOption.folder != "")
                {
                    path = exportOption.folder;
                    if ("/\\".IndexOf(path[^1]) < 0) path += "\\";
                    saved = TameFullRecord.allRecords.Save(path + now.ToString("yyyy.MM.dd HH.mm.ss") + ".tfr");
                }

            if (!saved)
            {
                path = UnityEditor.EditorUtility.OpenFolderPanel("Select a directory", "Assets", "");
                if ("/\\".IndexOf(path[^1]) < 0) path += "\\";
                if (path == "") saved = true;
                else
                    TameFullRecord.allRecords.Save(path + now.ToString("yyyy.MM.dd HH.mm.ss") + ".tfr");
            }

#endif
        }
        if (VRMode) return null;
        //    Debug.Log("bef " + TameCamera.cameraTransform.position.ToString());
        //   Debug.Log("aft " + TameCamera.cameraTransform.position.ToString());

        bool cga = CheckGripAndSwitch();
        FrameShot f = km.ToFrameShot();
        if (f.grip == 0)
            f.grip = (byte)(cga ? 1 : 0);
        return f;
    }
    bool GripInputActive()
    {
        if (Keyboard.current != null)
            if (Keyboard.current.spaceKey.isPressed || Keyboard.current.qKey.isPressed || Keyboard.current.eKey.isPressed)
                return true;
        if (Gamepad.current != null)
            if (Gamepad.current.xButton.isPressed || Gamepad.current.bButton.isPressed)
                return true;
        return false;
    }
    int GripMoveDirection()
    {
        if (MouseButton != 0)
            return MouseButton;
        if (Gamepad.current != null)
        {
            if (Gamepad.current.xButton.isPressed) return -1;
            if (Gamepad.current.bButton.isPressed) return 1;
        }
        if (Keyboard.current != null)
        {
            if (Keyboard.current.qKey.isPressed) return -1;
            if (Keyboard.current.eKey.isPressed) return 1;
        }
        return 0;
    }
    bool SwitchInput()
    {
        if (Keyboard.current != null)
            if (Keyboard.current.backquoteKey.wasPressedThisFrame || Mouse.current.middleButton.wasPressedThisFrame) return true;
        if (Gamepad.current != null)
            if (Gamepad.current.startButton.wasPressedThisFrame) return true;
        return false;
    }
    bool CheckGripAndSwitch()
    {
        bool r = false;
        TameArea sa;
        float gmd;
        if (grippedObject != null)
        {
            if (GripInputActive())
            {
                r = localPerson.keyMap.grip[0] = true;
                gmd = GripMoveDirection();
                if ((gmd > 0 && grippedObject.progress.progress < 1f) || ((gmd < 0 && grippedObject.progress.progress > 0f)))
                    localPerson.UpdateGrip(grippedObject, localPerson.nextArea.GripDelta(TameElement.deltaTime) * gmd);
                localPerson.action = Person.ActionUpdateGrip;
            }
            else
            {
                r = localPerson.keyMap.grip[0] = false;
                grippedObject = null;
                localPerson.action = 0;
            }
        }
        else
        {
            if (SwitchInput())
            {
                sa = TameArea.ClosestSwitch(tes, TameCamera.cameraTransform, 2.1f, out TameObject to);
                //if (sa != null)                        sa.Switch(true);
                if (sa != null)
                {
                    localPerson.nextArea = sa;
                    localPerson.action = Person.ActionSwitch;
                }
                Debug.Log("switch: " + (sa == null ? "null" : sa.element.name));
            }
            else if (localPerson.action != Person.ActionUpdateSwitch)
            {
                if (GripInputActive())
                {
                    closestGrip = TameArea.ClosestGrip(tes, TameCamera.cameraTransform, 2.1f, 70, out TameObject to);
                    if (closestGrip != null)
                    {
                        r = localPerson.keyMap.grip[0] = true;
                        Debug.Log("grip: " + closestGrip.element.name);
                        grippedObject = to;
                        localPerson.nextArea = closestGrip;
                        localPerson.action = Person.ActionGrip;
                    }
                }
            }
        }
        return r;
    }
    void PrepareSave()
    {
        savingTexture = true;
        RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24);
        mainCamera.targetTexture = rt;
    }
    void SaveTexture()
    {
        savingTexture = false;
        RenderTexture.active = mainCamera.targetTexture;
        Texture2D t = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        t.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        t.Apply();
        Destroy(t);
        byte[] b = t.EncodeToPNG();

        File.WriteAllBytes(Environment.SpecialFolder.MyPictures + "screenshot" + DateTime.Now.ToString("yyyy.MM.dd HH.mm.ss") + ".png", b);
        mainCamera.targetTexture = null;
    }

    public void SendEmail()
    {
        string[] lines = TameManager.EmailBody();
        string body = ProjectID + ":";
        for (int i = 0; i < lines.Length; i++)
            body += lines[i] + "\n\r";
        Debug.Log("sending: " + body);
        if (IsWebGL)
            TCPClient.InitiateMail(body, Address, AltPort);
        else
                if (multiPlayer)
        {
            WebMessage wm = WebMessage.Create(RiptideNetworking.MessageSendMode.reliable, Player.C_EmailToOwner);
            wm.AddString(TameManager.publish.email);
            wm.AddString(body);
            byte[] buffer = wm.GetByteData();
            StartCoroutine(Send(buffer));

        }
    }
    IEnumerator Send(byte[] buff)
    {
        UnityWebRequest www = UnityWebRequest.Put(Address + ":" + Port, buff);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log(www.error);
        }
        else
        {
            Debug.Log("Upload complete!");
        }
    }
}
