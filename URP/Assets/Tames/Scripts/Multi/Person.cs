using UnityEngine;
using RiptideNetworking;
using HandAsset;
using System.Collections.Generic;

namespace Multi
{
    /// <summary>
    /// this class contains all the activities of a person, including their id, and transforms and status of head and hands
    /// </summary>
    public class Person
    {
        public static List<Person> people = new List<Person>();
        public string nickname = "";
        public bool nicknameAssigned = false;
        public const int LocalDefault = 0;
        public static Person localPerson;
        public bool initiated = false;
        public bool isLocal = false;
        public GameObject head;
        public HandModel[] hand = new HandModel[2];

        public ushort id;
        public Vector3 headPosition, lastHead, headForward;
        public Quaternion headRotation;
        public Vector3[] position, lastPosition;
        public Quaternion[] rotation, lastRotation;
        //  public Vector3[] localEuler;
        //   public Vector3[] index;
        //   public Vector3[] middle;
        //  public bool[] A, B;
        //  public float[] grip, trigger;
        //  public Vector2[] stick;

        Vector3[] gripVector;
        private int[] gripIndex;
        private int gripForward;
        private int gripSign;
        private int gripUp;
        public Vector3 switchPosition = Vector3.negativeInfinity;
        public int switchCount = 0;
        public Tames.TameArea nextArea = null;
        public int action = 0;
        public const int ActionGrip = 1;
        public const int ActionUpdateGrip = 2;
        public const int ActionSwitch = 3;
        public const int ActionUpdateSwitch = 4;
        public Records.TameKeyMap keyMap, tempKeyMap;
        public bool inputReceived = false;
        public PersonClient client;
        public const byte AUX = 7;
        public const byte POS = 8;
        public const byte ROT = 9;
        public const byte HOLD = 10;
        public const byte PRESS = 11;
        public const byte GRIP = 12;
        public const byte END = 0;


        public Person(ushort id)
        {
            this.id = id;
            position = new Vector3[2];
            lastPosition = new Vector3[2];
            rotation = new Quaternion[2];
            lastRotation = new Quaternion[2];
            lastHead = Vector3.zero;
            //   localEuler = new Vector3[2];
            //  index = new Vector3[2];
            //  middle = new Vector3[2];
            //  A = new bool[2];
            //  B = new bool[2];
            //  grip = new float[2];
            //  trigger = new float[2];
            //  stick = new Vector2[2];
            keyMap = new Records.TameKeyMap(Tames.TameInputControl.checkedKeys.Count);
            tempKeyMap = id != ushort.MaxValue ? new Records.TameKeyMap(Tames.TameInputControl.checkedKeys.Count) : null;
        }
        public void FlushKeyMap()
        {
            if (CoreTame.IsServer)
            {
                if (client != null)
                {
                    keyMap.UPressed = client.receivedInput.pressed[0];
                    if (client.receivedInput.pressed[0] != 0) Debug.Log(client.receivedInput.pressed[0]);
                    keyMap.UHold = client.receivedInput.held[0];
                    keyMap.gpMap.UHold = client.receivedInput.held[1];
                    keyMap.gpMap.UPressed = client.receivedInput.pressed[1];
                    keyMap.vrMap.UHold = client.receivedInput.held[2];
                    keyMap.vrMap.UPressed = client.receivedInput.pressed[2];
                    keyMap.Aux = client.receivedInput.aux;
                    keyMap.gpMap.Aux = client.receivedInput.aux;
                    keyMap.grip[0] = (client.receivedInput.grip & 1) > 0;
                    keyMap.grip[1] = (client.receivedInput.grip & 2) > 0;
                    client.ClearRecord();
                    head.transform.position = headPosition = client.position[0];
                    head.transform.rotation = headRotation = client.rotation[0];
                    for (int i = 0; i < 2; i++)
                        if (hand[i] != null)
                        {
                            hand[i].wrist.transform.position = client.position[i];
                            hand[i].wrist.transform.rotation = client.rotation[i];
                        }
                    EncodeLocal();
                }
            }
            else
            {
                keyMap.UPressed = tempKeyMap.UPressed;
                keyMap.UHold = tempKeyMap.UHold;
                keyMap.gpMap.UHold = tempKeyMap.gpMap.UHold;
                keyMap.gpMap.UPressed = tempKeyMap.gpMap.UPressed;
                keyMap.vrMap.UHold = tempKeyMap.vrMap.UHold;
                keyMap.vrMap.UPressed = tempKeyMap.vrMap.UPressed;
                keyMap.Aux = tempKeyMap.Aux;
                keyMap.gpMap.Aux = tempKeyMap.gpMap.Aux;
                keyMap.grip[0] = tempKeyMap.grip[0];
                keyMap.grip[1] = tempKeyMap.grip[1];
                tempKeyMap.UPressed = 0;
                // tempKeyMap.UHold = 0;
                tempKeyMap.gpMap.UPressed = 0;
                //   tempKeyMap.gpMap.UHold= 0;
                tempKeyMap.vrMap.UPressed = 0;
                //    tempKeyMap.vrMap.UHold = 0;
                tempKeyMap.Aux = 0;
                tempKeyMap.gpMap.Aux = 0;
            }

        }
        public static Person Find(ushort id)
        {
            foreach (Person person in people)
                if (person.id == id)
                    return person;
            return null;
        }
        public static Person Add(ushort id)
        {
            Person p = new Person(id);
            p.isLocal = id == ushort.MaxValue;
            p.head = id == ushort.MaxValue ? CoreTame.mainCamera.gameObject : GameObject.Instantiate(CoreTame.HeadObject);
            if (p.isLocal)
                p.hand = CoreTame.hand;
            else
            {
                p.hand = new HandModel[2];
                p.hand[0] = HandModel.Duplicate(people[0].hand[0]);
                p.hand[1] = HandModel.Duplicate(people[0].hand[1]);
            }
            //    localPerson.head.SetActive(false);
            p.hand[0].wrist.SetActive(CoreTame.VRMode);
            p.hand[1].wrist.SetActive(CoreTame.VRMode);
            people.Add(p);
            return p;
        }
        public void ReceiveFrameAsClient(Message m)
        {
            Vector3 p;
            Quaternion q;
            uint input;
            byte type, sub;
            int changedCount = m.GetByte();
            for (int i = 0; i < changedCount; i++)
            {
                type = m.GetByte();
                sub = m.GetByte();
                switch (type)
                {
                    case POS:
                        p = m.GetVector3();
                        switch (sub)
                        {
                            case 0:
                                lastHead = headPosition;
                                head.transform.position = headPosition = p; break;
                            case 1:
                                lastPosition[0] = position[0];
                                hand[0].wrist.transform.position = position[0] = p; break;
                            case 2:
                                lastPosition[1] = position[1];
                                hand[1].wrist.transform.position = position[1] = p; break;
                        }
                        break;
                    case ROT:
                        q = m.GetQuaternion();
                        switch (sub)
                        {
                            case 0: head.transform.rotation = headRotation = q; break;
                            case 1:
                                lastRotation[0] = rotation[0];
                                hand[0].wrist.transform.rotation = rotation[0] = q; break;
                            case 2:
                                lastRotation[1] = rotation[1];
                                hand[1].wrist.transform.rotation = rotation[1] = q; break;
                        }
                        break;
                    case HOLD:
                        input = m.GetUInt();
                        switch (sub)
                        {
                            case 0: tempKeyMap.UHold = input; break;
                            case 1: tempKeyMap.gpMap.UHoldMasked = input; break;
                            case 2: tempKeyMap.vrMap.UHoldMasked = input; break;
                        }
                        break;
                    case PRESS:
                        Debug.Log("received press "+id);
                        input = m.GetUInt();
                        switch (sub)
                        {
                            case 0: tempKeyMap.UPressed = input; break;
                            case 1: tempKeyMap.gpMap.UPressedMasked = input; break;
                            case 2: tempKeyMap.vrMap.UPressedMasked = input; break;
                        }
                        break;
                    case AUX:
                        tempKeyMap.Aux = sub;
                        tempKeyMap.gpMap.Aux = sub;
                        break;
                    case GRIP:
                        byte g = sub;
                        tempKeyMap.grip[0] = (g & 1) > 0;
                        tempKeyMap.grip[1] = (g & 2) > 0;
                        break;
                }
            }
        }

        bool[] holdSendable = new bool[5];
        bool reliable, sendable;
        public byte sendableCount = 0;
        public void SendFrameAsClient(Records.FrameShot prev, Records.FrameShot current)
        {
            if (prev == null) return;
            for (int i = 0; i < 5; i++) holdSendable[i] = false;
            uint[] hold = new uint[] { 0, 0, 0 };
            byte[] ag = new byte[] { 0, 0 };
            //    bool input = false;
            sendableCount = 0;
            if (NetworkManager.Singleton.Client.IsConnected)
            {
                sendable = false;
                reliable = false;
                if (current.KBPressed != 0) sendableCount++;
                if (current.GPPressed != 0) sendableCount++;
                if (current.VRPressed != 0) sendableCount++;
                if ((current.KBHold != prev.KBHold) || (current.KBHold != 0))
                {
                    hold[0] = current.KBHold;
                    sendable = holdSendable[0] = true;
                    if (current.KBHold == 0) reliable = true;
                    sendableCount++;
                }
                if ((current.GPHold != prev.GPHold) || (current.GPHold != 0))
                {
                    sendable = holdSendable[1] = true;
                    hold[1] = current.GPHold;
                    if (current.GPHold == 0) reliable = true;
                    sendableCount++;
                }
                if ((current.VRHold != prev.VRHold) || (current.VRHold != 0))
                {
                    sendable = holdSendable[2] = true;
                    hold[2] = current.VRHold;
                    if (current.VRHold == 0) reliable = true;
                    sendableCount++;
                }
                if (sendable)
                //          if ( current.aux != 0)
                {
                    holdSendable[3] = true;
                    ag[0] = current.aux;
                    if (current.aux != prev.aux) reliable = true;
                    sendableCount++;
                }
                if (current.grip != prev.grip || current.grip != 0)
                {
                    sendable = holdSendable[4] = true;
                    ag[1] = current.grip;
                    if (current.grip != prev.grip) reliable = true;
                }
                if (prev.crot != current.crot)
                {
                    sendable = true;
                    sendableCount++;
                }
                if (prev.cpos != current.cpos)
                {
                    sendable = true;
                    sendableCount++;
                }
                for (int i = 0; i < 2; i++)
                {
                    if (prev.hpos[i] != current.hpos[i])
                    {
                        sendable = true;
                        sendableCount++;
                    }
                    if (prev.hrot[i] != current.hrot[i])
                    {
                        sendable = true;
                        sendableCount++;
                    }
                }
                Message m = Message.Create(reliable ? MessageSendMode.reliable : MessageSendMode.unreliable, Player.C_FrameData);
                //      bool sendable = false;
                if (sendableCount > 0) m.AddByte(sendableCount);
                if (prev.crot != current.crot)
                {
                    m.AddByte(Person.ROT);
                    m.AddByte(0);
                    m.AddQuaternion(current.crot);
                }
                if (prev.cpos != current.cpos)
                {
                    m.AddByte(Person.POS);
                    m.AddByte(0);
                    m.AddVector3(current.cpos);
                }
                for (int i = 0; i < 2; i++)
                {
                    if (prev.hpos[i] != current.hpos[i])
                    {
                        m.AddByte(Person.POS);
                        m.AddByte((byte)(i + 1));
                        m.AddVector3(current.hpos[i]);
                    }
                    if (prev.hrot[i] != current.hrot[i])
                    {
                        m.AddByte(Person.ROT);
                        m.AddByte((byte)(i + 1));
                        m.AddQuaternion(current.hrot[i]);
                    }
                }
                if (current.KBPressed != 0)
                {
               //     Debug.Log("local pressed " + id);
                    m.AddByte(Person.PRESS);
                    m.AddByte(0);
                    m.AddUInt(current.KBPressed);
                }
                if (current.GPPressed != 0)
                {
                    m.AddByte(Person.PRESS);
                    m.AddByte(1);
                    m.AddUInt(current.GPPressed);
                }
                if (current.VRPressed != 0)
                {
                    m.AddByte(Person.PRESS);
                    m.AddByte(2);
                    m.AddUInt(current.VRPressed);
                }
                for (int i = 0; i < 3; i++)
                    if (holdSendable[i])
                    {
                        m.AddByte(HOLD);
                        m.AddByte((byte)i);
                        m.AddUInt(hold[i]);
                    }
                if (holdSendable[3])
                {
                    m.AddByte(AUX);
                    m.AddByte(ag[0]);
                }
                if (holdSendable[4])
                {
                    m.AddByte(GRIP);
                    m.AddByte(ag[1]);
                }
                //     m.Add(Person.END);
                if (sendable)
                    Player.SendFrame(m);
            }
        }

        public bool Gripped(int handIndex)
        {
            uint v = handIndex == 0 ? Records.VRMap.GL : Records.VRMap.GR;
            return keyMap.vrMap.Held(v);
        }
        public void EncodeLocal()
        {
            lastHead = headPosition;
            headPosition = head.transform.position;
            headRotation = head.transform.rotation;
            headForward = head.transform.forward;
            for (int i = 0; i < 2; i++)
            {
                lastPosition[i] = position[i];
                lastRotation[i] = rotation[i];
                position[i] = hand[i].wrist.transform.position;
                rotation[i] = hand[i].wrist.transform.rotation;

            }
        }
        public static void UpdateAll(Records.FrameShot[] frames)
        {
            for (int i = 0; i < frames.Length; i++)
                if (i != Player.LocalID)
                    if (people[i] != null)
                    {
                        people[i].Update(frames[i]);
                    }
        }
        /// <summary>
        /// creates the hand model based on the bones in the prefab
        /// </summary>
        /// <param name="fingerHeader"></param>
        public void CreateModel(string fingerHeader)
        {
            head = GameObject.Instantiate(CoreTame.HeadObject);
            head.SetActive(true);
            GameObject g0 = GameObject.Instantiate(CoreTame.localPerson.hand[0].wrist);
            GameObject g1 = GameObject.Instantiate(CoreTame.localPerson.hand[1].wrist);
            hand[0] = new HandModel(null, g0, 0);
            hand[0].GetFingers(fingerHeader);
            hand[1] = new HandModel(null, g1, 1);
            hand[1].GetFingers(fingerHeader);
            hand[1].gripDirection = -1;
        }
        public void Update(Records.FrameShot frame)
        {
            if (frame != null)
            {
                hand[0].Grip(15, frame.Grip(0) ? 1 : 0);
                hand[1].Grip(15, frame.Grip(1) ? 1 : 0);
                head.transform.SetPositionAndRotation(frame.cpos, frame.crot);
                hand[0].wrist.transform.position = frame.hpos[0];
                hand[1].wrist.transform.position = frame.hpos[1];
                hand[0].wrist.transform.rotation = frame.hrot[0];
                hand[1].wrist.transform.rotation = frame.hrot[1];
                lastHead = headPosition;
                headPosition = head.transform.position;
                headRotation = head.transform.rotation;
                headForward = head.transform.forward;
                for (int i = 0; i < 2; i++)
                {
                    position[i] = hand[i].wrist.transform.position;
                    //    localEuler[i] = hand[i].wrist.transform.localEulerAngles;
                }

            }
        }
        /// <summary>
        /// updates the status of head and hands
        /// </summary>
        public void Update()
        {
            hand[0].Update();
            hand[1].Update();
            UpdateHeadOnly();
        }
        /// <summary>
        ///  updates only the head position
        /// </summary>
        public void UpdateHeadOnly()
        {
            lastHead = headPosition;
            headPosition = head.transform.position;
            headRotation = head.transform.rotation;
            switch (action)
            {
                case ActionGrip: Grip(nextArea, Tames.TameCamera.cameraTransform); break;
                case ActionUpdateGrip: break;// UpdateGrip(nextArea); break;
                case ActionSwitch: Switch(nextArea, Tames.TameCamera.cameraTransform); break;
                case ActionUpdateSwitch: UpdateSwitch(); break;
                default: Ungrip(); nextArea = null; break;
            }
        }
        /// <summary>
        /// finds the proper grip vectors
        /// </summary>
        /// <param name="t"></param>
        private void GripVectors(Transform t)
        {

            gripVector = new Vector3[]
             {
                gripIndex[0] == 0 ? t.right : (gripIndex[0] == 1 ? t.up : t.forward),
                gripIndex[1] == 0 ? t.right : (gripIndex[1] == 1 ? t.up : t.forward),
                gripIndex[2] == 0 ? t.right : (gripIndex[2] == 1 ? t.up : t.forward)
             };
        }

        /// <summary>
        /// finds the forward and up vector indexes for the hand that fits the grip geometry
        /// </summary>
        /// <param name="area"></param>
        /// <param name="cam"></param>
        private void FU(Tames.TameArea area, Transform cam)
        {
            Transform t = area.relative.transform;
            if (t.localScale.x > t.localScale.y)
                gripIndex = t.localScale.x > t.localScale.z ? new int[] { 0, 1, 2 } : new int[] { 2, 0, 1 };
            else
                gripIndex = t.localScale.y > t.localScale.z ? new int[] { 1, 0, 2 } : new int[] { 2, 0, 1 };
            GripVectors(t);
            int f = 1;
            float d, min = Vector3.Angle(cam.forward, gripVector[1]);
            if (min > (d = Vector3.Angle(cam.forward, -gripVector[1]))) { min = d; f = -1; }
            if (min > (d = Vector3.Angle(cam.forward, gripVector[2]))) { min = d; f = 2; }
            if (min > (d = Vector3.Angle(cam.forward, -gripVector[2]))) { min = d; f = -2; }
            gripForward = f < 0 ? -f : f;
            gripSign = f < 0 ? -1 : 1;
            gripUp = 3 - Mathf.Abs(f);
        }
        /// <summary>
        /// starts gripping by locating the left hand grip around the grip area
        /// </summary>
        /// <param name="area"></param>
        /// <param name="cam"></param>
        public void Grip(Tames.TameArea area, Transform cam)
        {
            hand[0].wrist.SetActive(true);
            hand[0].wrist.transform.parent = null;
            FU(area, cam);
            hand[0].wrist.transform.LookAt(hand[0].wrist.transform.position - gripVector[gripForward] * gripSign, gripVector[gripUp]);
            hand[0].data.grip.Update(1);
            hand[0].Grip(15, 1);
            hand[0].AfterGrip(true);
            //  grip[0] = 1;
            Vector3 v = hand[0].wrist.transform.position - hand[0].gripCenter;
            hand[0].wrist.transform.position = area.relative.transform.position + v;
            hand[0].AfterGrip(true);
            hand[0].lastGripCenter = hand[0].gripCenter;
        }
        /// <summary>
        ///  updates the hand position and rotation based on the changed transform of the grip area
        /// </summary>
        /// <param name="area"></param>
        public void UpdateGrip(Tames.TameArea area)
        {
            GripVectors(area.relative.transform);
            hand[0].wrist.transform.LookAt(hand[0].wrist.transform.position - gripVector[gripForward] * gripSign, gripVector[gripUp]);

            Vector3 v = hand[0].wrist.transform.forward * 0.07f + hand[0].wrist.transform.up * 0.02f;
            hand[0].wrist.transform.position = area.relative.transform.position + v;
            hand[0].AfterGrip(true);
        }   /// <summary>
            ///  updates the hand position and rotation based on the changed transform of the grip area
            /// </summary>
            /// <param name="area"></param>
        public void UpdateGrip(Tames.TameObject to, float delta)
        {
            to.handle.SimulateGrip(to.progress.progress, delta, hand[0].wrist.transform);
            hand[0].AfterGrip(true);
            //    Debug.Log("being simulated " + hand[0].lastGripCenter.ToString("0.000") + hand[0].gripCenter.ToString("0.000"));
        }
        /// <summary>
        /// detaches the hand from the grip area
        /// </summary>
        public void Ungrip()
        {
            hand[0].wrist.SetActive(false);
            hand[0].wrist.transform.parent = null;
            hand[0].data.grip.Update(0);
            hand[0].Grip(15, 0);
            //      grip[0] = 0;
            hand[0].AfterGrip(true);
            hand[0].wrist.transform.position = head.transform.position - head.transform.up * 0.7f - head.transform.right * 0.3f;
        }
        public void Switch(Tames.TameArea area, Transform cam)
        {
            hand[0].wrist.SetActive(true);
            Vector3 sh = cam.position - 0.3f * Vector3.up - 0.2f * cam.right;
            Vector3 fn = area.relative.transform.position - sh;
            Vector3 f = fn.normalized;
            Vector3 r = Vector3.Cross(Vector3.up, f);
            Vector3 u = Vector3.Cross(f, r);
            hand[0].wrist.transform.position = area.relative.transform.position;
            hand[0].wrist.transform.LookAt(sh, u);
            hand[0].wrist.transform.position = area.relative.transform.position - hand[0].tipMax * f;
            hand[0].Grip(15, 0);
            hand[0].AfterGrip(true);
            switchCount = 1;
            action = ActionUpdateSwitch;
        }
        public bool UpdateSwitch()
        {
            if (switchCount == 0)
                return false;
            else
            {
                switchCount++;
                hand[0].AfterGrip(true);
            }
            if (switchCount >= 60)
            {
                hand[0].wrist.transform.position = head.transform.position;
                hand[0].wrist.SetActive(false);
                switchCount = 0;
                action = 0;
                return false;
            }
            return true;
        }

    }
}