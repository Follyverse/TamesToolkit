using UnityEngine;
using RiptideNetworking;
using System.Collections.Generic;

namespace Multi
{
    /// <summary>
    /// this class contains all the activities of a person, including their id, and transforms and status of head and hands
    /// </summary>
    public class RemoteInput
    {
        public uint[] pressed = new uint[] { 0, 0, 0 };
        public uint[] held = new uint[] { 0, 0, 0 };
        public byte aux = 0;
        public byte grip = 0;
    }
    public class PersonClient
    {
        public bool initiated = false;
        public bool isLocal = false;
        public string nickname = "";
        //    public List<InputRecord> record = new List<InputRecord>();
        public ushort id;
        public bool awaitingRequest = false;
        // public InputRecord[] commonRecords;
        public System.DateTime lastSignal, connection, requestTime;
     //   public ResultsInMail toEmail = null;
        public RemoteInput input, receivedInput;
        public Vector3[] position = new Vector3[3];
        public Quaternion[] rotation = new Quaternion[3];
        public bool[] commonChanged = new bool[6];
        public bool[] inputChanged = new bool[8];
        public Person person;
        public string ConnectionTime
        {
            get
            {
                return connection.ToString("yyyy.MM.dd.HH.mm.ss.") + connection.Millisecond;
            }
        }
        public PersonClient(ushort id)
        {
            input = new RemoteInput();
            receivedInput = new RemoteInput();
            this.id = id;
            commonChanged = new bool[] { false, false, false, false, false, false };
            inputChanged = new bool[] { false, false, false, false, false, false, false, false };
        }


        public const byte AUX = 7;
        public const byte POS = 8;
        public const byte ROT = 9;
        public const byte HOLD = 10;
        public const byte PRESS = 11;
        public const byte GRIP = 12;
        public const byte END = 255;
        public void AddCommon(Message m, int c)
        {
            switch (c)
            {
                case 0:
                    m.AddByte(POS); m.AddByte(0);// Debug.Log("send pos");
                    m.AddVector3(position[0]); break;
                case 1: m.AddByte(POS); m.AddByte(1); m.AddVector3(position[1]); break;
                case 2: m.AddByte(POS); m.AddByte(2); m.AddVector3(position[2]); break;
                case 3: m.AddByte(ROT); m.AddByte(0); m.AddQuaternion(rotation[0]); break;
                case 4: m.AddByte(ROT); m.AddByte(1); m.AddQuaternion(rotation[1]); break;
                case 5: m.AddByte(ROT); m.AddByte(2); m.AddQuaternion(rotation[2]); break;
            }
        }
        public void AddInput(Message m, int sub, bool hold)
        {
            m.AddByte((byte)(hold ? HOLD : PRESS));
            if (!hold)
                Debug.Log(id + " press ");
            m.AddByte((byte)sub);
            m.AddUInt(hold ? input.held[sub] : input.pressed[sub]);
        }
        public void AddAux(Message m)
        {
            m.AddByte(AUX);
            m.AddByte(input.aux);
        }
        public void AddGrip(Message m)
        {
            m.AddByte(GRIP);
            m.AddByte(input.grip);
        }
        public bool reliable;
        public byte sendableCount;
        public void SetChanged(Records.FrameShot prev, Records.FrameShot current)
        {
            for (int i = 0; i < sendableInput.Length; i++) sendableInput[i] = false;
            for (int i = 0; i < sendableCommon.Length; i++) sendableCommon[i] = false;
            sendableCount = 0;
            if (prev == null) return;
            if (prev.crot != current.crot)
            {
                sendableCount++;
                sendableCommon[3] = true;
                rotation[0] = current.crot;
            }
            if (prev.cpos != current.cpos)
            {
                //     Debug.Log("send pos");
                sendableCount++;
                sendableCommon[0] = true;
                position[0] = current.cpos;
            }
            for (int i = 0; i < 2; i++)
            {
                if (prev.hpos[i] != current.hpos[i])
                {
                    sendableCount++;
                    sendableCommon[(i + 1)] = true;
                    position[i + 1] = current.hpos[i];
                }
                if (prev.hrot[i] != current.hrot[i])
                {
                    sendableCount++;
                    sendableCommon[(i + 1) +3] = true;
                    rotation[i + 1] = current.hrot[i];
                }
            }
            reliable = false;

            input.pressed[0] = current.KBPressed;
            input.pressed[1] = current.GPPressed;
            input.pressed[2] = current.VRPressed;
            for (int i = 0; i < 3; i++) if (input.pressed[i] != 0) { sendableCount++; sendableInput[i] = true; } else sendableInput[i] = false;

            input.held[0] = input.held[1] = input.held[2] = 0;
            if ((current.KBHold != prev.KBHold) || (current.KBHold != 0))
            {
                input.held[0] = current.KBHold;
                sendableInput[3] = true;
                sendableCount++;
                if (current.KBHold == 0) reliable = true;
            }
            if ((current.GPHold != prev.GPHold) || (current.GPHold != 0))
            {
                input.held[1] = current.GPHold;
                sendableInput[4] = true;
                sendableCount++;
                if (current.GPHold == 0) reliable = true;
            }
            if ((current.VRHold != prev.VRHold) || (current.VRHold != 0))
            {
                input.held[2] = current.VRHold;
                sendableInput[5] = true;
                sendableCount++;
                if (current.VRHold == 0) reliable = true;
            }
            //    if (current.aux != 0)
            //     {
            input.aux = current.aux;
            sendableInput[6] = true;
            sendableCount++;
            if (current.aux != prev.aux) reliable = true;
            //     }
            if (current.grip != prev.grip || current.grip != 0)
            {
                input.grip = current.grip;
                sendableInput[7] = true;
                sendableCount++;
                if (current.grip != prev.grip) reliable = true;
            }
        }

        public static void FakeReceive(Message m)
        {
            byte changeCount = m.GetByte();
            for (int i = 0; i < changeCount; i++)
            {
                byte type = m.GetByte();
                byte sub = m.GetByte();
                switch (type)
                {
                    case Person.POS: m.GetVector3(); break;
                    case Person.ROT: m.GetQuaternion(); break;
                    case Person.PRESS:
                    case Person.HOLD: m.GetUInt(); break;
                }
            }
        }
        public void RecevieFrameAsServer(Message m)
        {
            byte changeCount = m.GetByte();
            for (int i = 0; i < changeCount; i++)
            {
                byte type = m.GetByte();
                byte sub = m.GetByte();
                switch (type)
                {
                    case Person.POS:
                        if (sub == 0)
                        {
                       //     Debug.Log("receive pos");
                            position[0] = m.GetVector3();
                            commonChanged[0] = true;
                        }
                        else
                        {
                            position[sub] = m.GetVector3();
                            commonChanged[sub] = true;
                        }
                        break;
                    case Person.ROT:
                        if (sub == 0)
                        {
                            rotation[0] = m.GetQuaternion();
                            commonChanged[3] = true;
                        }
                        else
                        {
                            rotation[sub] = m.GetQuaternion();
                            commonChanged[3 + sub] = true;
                        }
                        break;
                    case Person.PRESS:
                        receivedInput.pressed[sub] |= m.GetUInt();
                        inputChanged[sub] = true;
                        Debug.Log("press");
                        break;
                    case Person.HOLD:
                        inputChanged[3 + sub] = true;
                        receivedInput.held[sub] = m.GetUInt();
                        break;
                    case Person.AUX:
                        inputChanged[6] = true;
                        receivedInput.aux = sub;
                        Debug.Log("aux");
                        break;
                    case Person.GRIP:
                        inputChanged[7] = true;
                        receivedInput.grip = sub;
                        break;
                }
            }
        }
        private void Flush()
        {
            for (int j = 0; j < input.held.Length; j++)
            {
                input.held[j] = receivedInput.held[j];
                input.pressed[j] = receivedInput.pressed[j];
                receivedInput.pressed[j] = 0;
            }
            input.aux = receivedInput.aux;
            input.grip = receivedInput.grip;
        }
        private bool[] sendableCommon = new bool[6], sendableInput = new bool[8];
        //   private byte sendableCount;

        public byte Sendables()
        {
            byte r = 0;
            for (int i = 0; i < 6; i++)
            {
                if (sendableCommon[i] = commonChanged[i]) r++;
                commonChanged[i] = false;
            }
            for (int i = 0; i < 8; i++)
            {
                if (sendableInput[i] = inputChanged[i]) r++;
                inputChanged[i] = false;
            }
            return r;
        }
        public void ClearRecord()
        {
            sendableCount = Sendables();
            Flush();
        }
        public void SendFrameAsServer()
        {
            Player.project.CheckConnection();
            bool reliable = false;
            for (int i = 0; i < Player.project.users.Count; i++)
                if (Player.project.users[i].reliable) { reliable = true; break; }
            Message m = Message.Create(reliable ? MessageSendMode.reliable : MessageSendMode.unreliable, Player.S_FrameData);
            m.AddByte((byte)Player.project.users.Count);
            byte playerAdded = 0;
            //     bool[] sendableCommon, sendableInput;
            for (int i = 0; i < Player.project.users.Count; i++)
            {
                PersonClient pc = Player.project.users[i];
                //    sendableCount = pc.Sendables(out sendableCommon, out sendableInput);
                //    pc.ClearRecord();
                if (sendableCount == 0)
                    m.AddUShort(Player.NoID);
                else
                {
                    m.AddUShort(pc.id);
                    playerAdded++;
                    m.AddByte(sendableCount);
                    for (int j = 0; j < 6; j++) if (sendableCommon[j]) pc.AddCommon(m, j);
                    for (int j = 0; j < 3; j++) if (sendableInput[j]) pc.AddInput(m, j, false);
                    for (int j = 0; j < 3; j++) if (sendableInput[j + 3]) pc.AddInput(m, j, true);
                    if (sendableInput[6]) pc.AddAux(m);
                    if (sendableInput[7]) pc.AddGrip(m);
                }
                //         if (i != 0) pc.ClearRecord();
            }
            if (playerAdded != 0)
                NetworkManager.Singleton.Server.SendToAll(m);
        }


    }
}