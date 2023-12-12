using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using Tames;

namespace Records
{

    public class VRMap
    {
        //     public float[] trigger;
        //    public float[] grip;
        //     public Vector2[] stick;
        //     public float[] thumb;
        //     public bool[] A;
        //     public bool[] B;
        //     private const uint u0 = 1, u1 = 2, u2 = 4, u3 = 8, u4 = 16, u5 = 32, u6 = 64, u7 = 128, u8 = 256, u9 = 512, u10 = 1024, u11 = 2048, u12 = 4096, u13 = 8192, u14 = 16384, u15 = 32768, u16 = 65536, u17 = 131072, u18 = 262144, u19 = 524288, u20 = 1048576, u21 = 2097152, u22 = 4194304, u23 = 8388608, u24 = 16777216, u25 = 33554432, u26 = 67108864, u27 = 134217728, u28 = 268435456, u29 = 536870912, u30 = 1073741824, u31 = 2147483648;

        public const uint
            TL = 1,
            TR = 2,
            GL = 4,
            GR = 8,
            SLLEFT = 16,
            SLRIGHT = 32,
            SLDOWN = 64,
            SLUP = 128,
            SRLEFT = 256,
            SRRIGHT = 512,
            SRDOWN = 1024,
            SRUP = 2048,
            THLDOWN = 4096,
            THLUP = 8192,
            THRDOWN = 16384,
            THRUP = 32768,
            AL = 65536,
            AR = 131072,
            BL = 262144,
            BR = 524288;

        public VRMap()
        {
            //      trigger = new float[] { 0, 0 };
            //      grip = new float[] { 0, 0 };
            //       stick = new Vector2[] { Vector2.zero, Vector2.zero };
            //        thumb = new float[] { 0, 0 };
            //         A = new bool[] { false, false };
            //         B = new bool[] { false, false };
        }
        public static bool Gripped(uint hold, int index)
        {
            if (index == 0)
                return (hold & GL) > 0;
            else
                return (hold & GR) > 0;
        }

        public uint UPressed = 0, UHold = 0;
        private void Act(uint index, bool[] value)
        {
            if ((UHold & index) > 0) UHold -= index;
            if (value[1]) UHold |= index;
            if ((UPressed & index) > 0) UPressed -= index;
            if (value[0]) UPressed |= index;

        }
        private void Act(uint index, bool value)
        {
            if ((UHold & index) > 0) UHold -= index;
            if (value) UHold |= index;
            bool notPressed = false;
            if ((UPressed & index) == 0) notPressed = true;
            if (notPressed && value)
                UPressed |= index;
            else if (!notPressed)
                UPressed -= index;
        }
        public bool Pressed(uint index)
        {
            return (UPressed & index) > 0;
        }
        public bool Held(uint index)
        {
            return (UHold & index) > 0;
        }
        public const uint Mask = uint.MaxValue - TL - TR - SLDOWN - SLRIGHT - SLUP - SLLEFT;
        public uint UPressedMasked
        {
            set
            {
                UPressed = Mask & value;
            }
        }
        public uint UHoldMasked
        {
            set
            {
                UHold = Mask & value;
            }
        }
        public void Capture()
        {
            Act(TL, CoreTame.localPerson.hand[0].data.trigger.Both);
            Act(TR, CoreTame.localPerson.hand[1].data.trigger.Both);
            Act(GL, CoreTame.localPerson.hand[0].data.grip.Both);
            Act(GR, CoreTame.localPerson.hand[1].data.grip.Both);
            Vector2 v = CoreTame.localPerson.hand[0].data.stick.Vector;
            Act(SLLEFT, v.x < -0.5f);
            Act(SLRIGHT, v.x > 0.5f);
            Act(SLDOWN, v.y < -0.5f);
            Act(SLUP, v.y > 0.5f);
            v = CoreTame.localPerson.hand[1].data.stick.Vector;
            Act(SRLEFT, v.x < -0.5f);
            Act(SRRIGHT, v.x > 0.5f);
            Act(SRDOWN, v.y < -0.5f);
            Act(SRUP, v.y > 0.5f);
            float f = CoreTame.localPerson.hand[0].data.thumb.Value;
            Act(THLDOWN, f < 0);
            Act(THLUP, f > 0);
            f = CoreTame.localPerson.hand[1].data.thumb.Value;
            Act(THRDOWN, f < 0);
            Act(THRUP, f > 0);
            bool h = CoreTame.localPerson.hand[0].data.A.Pressed;
            Act(AL, h);
            h = CoreTame.localPerson.hand[1].data.A.Pressed;
            Act(AR, h);
            h = CoreTame.localPerson.hand[0].data.B.Pressed;
            Act(BL, h);
            h = CoreTame.localPerson.hand[1].data.B.Pressed;
            Act(BR, h);
        }
        public string Export()
        {
            return "";
        }
        public bool ChangedFrom(VRMap vm)
        {
            return false;
        }
        public byte Aux
        {
            get { return (byte)(((UHold & TL) > 0 ? 16 : 0) + ((UHold & TR) > 0 ? 32 : 0)); }
            set
            {
                if ((Aux & 16) > 0) UHold |= TL;
                else UHold &= uint.MaxValue - TL;
                if ((Aux & 32) > 0) UHold |= TR;
                else UHold &= uint.MaxValue - TR;
            }
        }
    }
    public class GPMap
    {
        //  public bool[] pressed = new bool[9];
        //  public bool[] hold = new bool[9];
        //  public Vector2[] stick = new Vector2[] { Vector2.zero, Vector2.zero };
        //   public Vector2 dpad = Vector2.zero;
        //   public float[] trigger = new float[] { 0, 0 };
        //   public float[] shoulder = new float[] { 0, 0 };
        //    private const uint u0 = 1, u1 = 2, u2 = 4, u3 = 8, u4 = 16, u5 = 32, u6 = 64, u7 = 128, u8 = 256, u9 = 512, u10 = 1024, u11 = 2048, u12 = 4096, u13 = 8192, u14 = 16384, u15 = 32768, u16 = 65536, u17 = 131072, u18 = 262144, u19 = 524288, u20 = 1048576, u21 = 2097152, u22 = 4194304, u23 = 8388608, u24 = 16777216, u25 = 33554432, u26 = 67108864, u27 = 134217728, u28 = 268435456, u29 = 536870912, u30 = 1073741824, u31 = 2147483648;
        public const uint
             TL = 1,
             TR = 2,
             SHL = 4,
             SHR = 8,
             SLLEFT = 16,
             SLRIGHT = 32,
             SLDOWN = 64,
             SLUP = 128,
             SRLEFT = 256,
             SRRIGHT = 512,
             SRDOWN = 1024,
             SRUP = 2048,
             DLEFT = 4096,
             DRIGHT = 8192,
             DUP = 16384,
             DDOWN = 32768,
             A = 65536,
             B = 131072,
             X = 262144,
             Y = 524288;
        public GPMap()
        {
            UPressed = 0;
            UHold = 0;

        }
        public uint UPressed, UHold;
        public const uint Mask = uint.MaxValue - TL - TR - SLDOWN - SLRIGHT - SLUP - SLLEFT - SRDOWN - SRUP;
        public uint UPressedMasked
        {
            set
            {
                UPressed = Mask & value;
            }
        }
        public uint UHoldMasked
        {
            set
            {
                UHold = Mask & value;
            }
        }
        private void Act(uint index, bool pressed, bool hold)
        {
            if ((UPressed & index) > 0) UPressed -= index;
            if (pressed) UPressed |= index;
            if ((UHold & index) > 0) UHold -= index;
            if (hold) UHold |= index;
        }
        private void ActRetro(uint index, bool pressed)
        {
            bool p = (UPressed & index) > 0 || (UHold & index) > 0;
            if ((UPressed & index) > 0) UPressed -= index;
            if (pressed) UPressed |= index;
            if ((UHold & index) > 0) UHold -= index;
            if (p) if (pressed) UHold |= index;
        }
        public bool Held(uint index)
        {
            return (UHold & index) > 0;
        }
        public bool Pressed(uint index)
        {
            return (UPressed & index) > 0;
        }

        public void Capture()
        {
            Gamepad gp = Gamepad.current;
            if (gp != null)
            {
                Act(A, gp.aButton.wasPressedThisFrame, gp.aButton.isPressed);
                Act(B, gp.bButton.wasPressedThisFrame, gp.bButton.isPressed);
                Act(X, gp.xButton.wasPressedThisFrame, gp.xButton.isPressed);
                Act(Y, gp.yButton.wasPressedThisFrame, gp.yButton.isPressed);
                Act(TL, gp.leftTrigger.wasPressedThisFrame, gp.leftTrigger.isPressed);
                Act(TR, gp.rightTrigger.wasPressedThisFrame, gp.rightTrigger.isPressed);
                Act(SHL, gp.leftShoulder.wasPressedThisFrame, gp.leftShoulder.isPressed);
                Act(SHR, gp.rightShoulder.wasPressedThisFrame, gp.rightShoulder.isPressed);
                Vector2 stick = gp.leftStick.ReadValue();
                if (stick.x < -0.5) ActRetro(SLLEFT, true);
                if (stick.x > 0.5) ActRetro(SLRIGHT, true);
                if (stick.y < -0.5) ActRetro(SLDOWN, true);
                if (stick.y > 0.5) ActRetro(SLUP, true);
                stick = gp.rightStick.ReadValue();
                if (stick.x < -0.5) ActRetro(SRLEFT, true);
                if (stick.x > 0.5) ActRetro(SRRIGHT, true);
                if (stick.y < -0.5) ActRetro(SRDOWN, true);
                if (stick.y > 0.5) ActRetro(SRUP, true);
                stick = gp.dpad.ReadValue();
                if (stick.x < -0.5) ActRetro(DLEFT, true);
                if (stick.x > 0.5) ActRetro(DRIGHT, true);
                if (stick.y < -0.5) ActRetro(DDOWN, true);
                if (stick.y > 0.5) ActRetro(DUP, true);
            }
        }
        public int ButtonHold(InputControlHold aux, InputHoldType h, float threshold)
        {
            if (Gamepad.current != null)
            {
                //     Debug.Log(Gamepad.current.rightTrigger.isPressed ? 1 : (Gamepad.current.leftTrigger.isPressed ? -1 : 0));
                bool comb = true;
                if (aux != InputControlHold.None)
                    comb = aux switch
                    {
                        InputControlHold.GTL => Held(TL) && !Held(TR),
                        InputControlHold.GTR => Held(TR) && !Held(TL),
                        _ => Held(TL) && Held(TR),
                    };
                else
                    comb = !(Held(TL) || Held(TR));
                if (!comb) return 0;
                switch (h)
                {
                    case InputHoldType.GDY: return Held(DDOWN) ? -1 : (Held(DUP) ? 1 : 0);
                    case InputHoldType.GDX: return Held(DLEFT) ? -1 : (Held(DRIGHT) ? 1 : 0);
                    case InputHoldType.GS: return Held(SHL) ? -1 : (Held(SHR) ? 1 : 0);
                    case InputHoldType.GXB: return Held(X) ? -1 : (Held(B) ? 1 : 0);
                    case InputHoldType.GYA: return Held(A) ? -1 : (Held(Y) ? 1 : 0);
                }
            }
            return 0;
        }
        public bool ButtonPressed(InputControlHold aux, InputHoldType h, float threshold)
        {
            if (Gamepad.current != null)
            {
                bool comb = true;
                if (aux != InputControlHold.None)
                    comb = aux switch
                    {
                        InputControlHold.GTL => Held(TL) && !Held(TR),
                        InputControlHold.GTR => Held(TR) && !Held(TL),
                        _ => Held(TL) && Held(TR),
                    };
                else
                    comb = !(Held(TL) || Held(TR));
                if (!comb) return false;
                switch (h)
                {
                    case InputHoldType.GDYD: return Pressed(DDOWN);
                    case InputHoldType.GDYU: return Pressed(DUP);
                    case InputHoldType.GDXL: return Pressed(DLEFT);
                    case InputHoldType.GDXR: return Pressed(DRIGHT);
                    case InputHoldType.GSL: return Pressed(SHL);
                    case InputHoldType.GSR: return Pressed(SHR);
                    case InputHoldType.GA: return Pressed(A);
                    case InputHoldType.GB: return Pressed(B);
                    case InputHoldType.GX: return Pressed(X);
                    case InputHoldType.GY: return Pressed(Y);
                }
            }
            return false;
        }
        public void Write(BinaryWriter bin)
        {
            bin.Write(UPressed);
            bin.Write(UHold);
        }
        public void Read(BinaryReader bin)
        {
            UPressed = bin.ReadUInt32();
            UHold = bin.ReadUInt32();
        }
        public string ExportStatus(bool[] status)
        {

            return "";
        }
        public string ExportValue()
        {

            return "";
        }
        public byte Aux
        {
            get
            {
                return (byte)((Held(TL) ? 64 : 0) + (Held(TR) ? 128 : 0));
            }
            set
            {
                ActRetro(TL, (value & 64) > 0);
                ActRetro(TR, (value & 128) > 0);
            }
        }
        public string ExportAux()
        {
            return "";
        }
        public bool ChangedFrom(GPMap vm)
        {
            return false;
        }
    }


    public class TameKeyMap
    {
        public const int LeftMouse = 29, MiddleMouse = 30, RightMouse = 31;
        public VRMap vrMap;
        public GPMap gpMap;
        //    public MouseMap mouse;
        public int keyCount;
        public bool[] pressed;
        public bool[] hold;
        public float[] values;
        public bool forward = false;
        public bool back = false;
        public bool left = false;
        public bool right = false;
        public bool up = false;
        public bool down = false;
        public bool shift = false;
        public bool ctrl = false;
        public bool alt = false;
        public bool[] grip = new bool[] { false, false };
        //    public bool info = false;
        bool passed = false;
        public TameKeyMap(int keyCount)
        {
            this.keyCount = keyCount;
            pressed = new bool[64];
            hold = new bool[64];
            vrMap = new VRMap();
            gpMap = new GPMap();
            //        mouse = new MouseMap();
        }
        public void Grip(byte g)
        {
            grip[0] = (g & 1) > 0;
            grip[1] = (g & 2) > 0;
        }
        public void WriteDescription(BinaryWriter bin)
        {
            string s = "";
            for (int i = 0; i < keyCount; i++)
                s += (i == 0 ? "" : ",") + TameInputControl.checkedKeys[i].displayName;
            bin.Write(s);
        }
        //      static float lastMouseX = 0;
        int MouseShifted()
        {
            int r = 0;
            if (TameCamera.navMode == Markers.NavigationMode.WSMouse)
                if (Mouse.current != null)
                {
                    float x = Mouse.current.position.x.value;
                    if (x < Screen.width / 3) r = -1;
                    if (x > Screen.width * 0.66f) r = 1;
                    //      if (lastMouseX != x) Debug.Log(r + " " + lastMouseX + " " + x);
                    //               lastMouseX = x;
                }
            return r;
        }
        public FrameShot Capture()
        {
            if (Keyboard.current != null)
            {
                forward = Keyboard.current.wKey.isPressed;
                back = Keyboard.current.sKey.isPressed;
                int dir = MouseShifted();
                left = TameCamera.navMode == Markers.NavigationMode.WASD ? Keyboard.current.aKey.isPressed : dir < 0;
                right = TameCamera.navMode == Markers.NavigationMode.WASD ? Keyboard.current.dKey.isPressed : dir > 0;
                //     Debug.Log(left + " " + right);
                up = Keyboard.current.rKey.isPressed;
                down = Keyboard.current.fKey.isPressed;
                shift = Keyboard.current.shiftKey.isPressed;
                ctrl = Keyboard.current.ctrlKey.isPressed;
                alt = Keyboard.current.altKey.isPressed;
                //      info = Keyboard.current.enterKey.wasPressedThisFrame;
                for (int i = 0; i < keyCount; i++)
                {
                    if (pressed[i]) pressed[i] = false;
                    else
                        pressed[i] = TameInputControl.checkedKeys[i].wasPressedThisFrame;
                    if (pressed[i]) Debug.Log("key " + i + " pressed at tick " + TameElement.Tick);
                    hold[i] = TameInputControl.checkedKeys[i].isPressed;
                    if (!VoiceCommands.used)
                        if (VoiceCommands.key == TameInputControl.checkedKeys[i]) { pressed[i] = true; VoiceCommands.used = true; }
                }
            }
            if (Mouse.current != null)
            {
                hold[LeftMouse] = Mouse.current.leftButton.isPressed;
                hold[MiddleMouse] = Mouse.current.middleButton.isPressed;
                hold[RightMouse] = Mouse.current.rightButton.isPressed;
                pressed[LeftMouse] = pressed[LeftMouse] ? false : Mouse.current.leftButton.wasPressedThisFrame;
                pressed[MiddleMouse] = pressed[MiddleMouse] ? false : Mouse.current.middleButton.wasPressedThisFrame;
                pressed[RightMouse] = pressed[RightMouse] ? false : Mouse.current.rightButton.wasPressedThisFrame;
                //        Vector2 mousePosition = new Vector2(Mouse.current.position.x.ReadValue(), Mouse.current.position.y.ReadValue());
                //       mouse.y = (mousePosition.y - 0.5f * CoreTame.screenSize.y) / (0.5f * CoreTame.screenSize.y);
            }
            vrMap.Capture();
            grip[0] = vrMap.Held(VRMap.GL);
            grip[1] = vrMap.Held(VRMap.GR);

            gpMap.Capture();
            return ToFrameShot();
        }
        public uint UPressed
        {
            get
            {
                uint r = 0;
                for (int i = 0; i < keyCount; i++)
                    r += (pressed[i] ? 1u << i : 0);
                r += pressed[LeftMouse] ? 1u << LeftMouse : 0;
                r += pressed[MiddleMouse] ? 1u << MiddleMouse : 0;
                r += pressed[RightMouse] ? 1u << RightMouse : 0;
                return r;
            }
            set
            {
                for (int i = 0; i < keyCount; i++)
                    pressed[i] = (value & (1u << i)) != 0;
                pressed[LeftMouse] = (value & 1u << LeftMouse) > 0;
                pressed[MiddleMouse] = (value & 1u << MiddleMouse) > 0;
                pressed[RightMouse] = (value & 1u << RightMouse) > 0;
            }
        }
        public uint UHold
        {
            get
            {
                uint r = 0;
                for (int i = 0; i < keyCount; i++)
                    r += (hold[i] ? 1u << i : 0);
                r += hold[LeftMouse] ? 1u << LeftMouse : 0;
                r += hold[MiddleMouse] ? 1u << MiddleMouse : 0;
                r += hold[RightMouse] ? 1u << RightMouse : 0;
                return r;
            }
            set
            {
                for (int i = 0; i < keyCount; i++)
                    hold[i] = (value & (1u << i)) != 0;
                hold[LeftMouse] = (value & 1u << LeftMouse) > 0;
                hold[MiddleMouse] = (value & 1u << MiddleMouse) > 0;
                hold[RightMouse] = (value & 1u << RightMouse) > 0;
            }
        }
        public byte Aux
        {
            get
            {
                return (byte)((shift ? 1 : 0) + (ctrl ? 2 : 0) + (alt ? 4 : 0));
            }
            set
            {
                shift = (value & 1) > 0;
                ctrl = (value & 2) > 0;
                alt = (value & 4) > 0;
            }
        }
        public bool AuxHold(InputControlHold aux)
        {
            return aux switch
            {
                InputControlHold.Shift => shift,
                InputControlHold.Ctrl => ctrl,
                InputControlHold.Alt => alt,
                _ => !(alt || ctrl || shift),
            };
        }
        public FrameShot ToFrameShot()
        {
            FrameShot f = new FrameShot();
            f.KBPressed = UPressed;
            f.KBHold = UHold;
            f.GPPressed = gpMap.UPressed;
            f.GPHold = gpMap.UHold;
            f.VRPressed = vrMap.UPressed;
            f.VRHold = vrMap.UHold;
            f.aux = (byte)(Aux + gpMap.Aux);
            f.grip = (byte)((grip[0] ? 1 : 0) + (grip[1] ? 2 : 0));
            //  f.mouse = mouse.U;
            return f;
        }
        public void Write(BinaryWriter bin)
        {
            //   bin.Write(keyCount);
            bin.Write(forward);
            bin.Write(back);
            bin.Write(left);
            bin.Write(right);
            bin.Write(up);
            bin.Write(down);
            bin.Write(shift);
            bin.Write(ctrl);
            bin.Write(alt);
            //          bin.Write(info);
            //     bin.Write(mouse.y);
            bin.Write(UPressed);
            bin.Write(UHold);
            //   bin.Write(mouse.U);
            bin.Write(gpMap.UPressed);
            bin.Write(gpMap.UHold);
            bin.Write(vrMap.UPressed);
            bin.Write(vrMap.UHold);
        }
        public void Read(BinaryReader bin)
        {
            forward = bin.ReadBoolean();
            back = bin.ReadBoolean();
            left = bin.ReadBoolean();
            right = bin.ReadBoolean();
            up = bin.ReadBoolean();
            down = bin.ReadBoolean();
            shift = bin.ReadBoolean();
            ctrl = bin.ReadBoolean();
            alt = bin.ReadBoolean();
            //         info = bin.ReadBoolean();
            //       mouse.y = bin.ReadSingle();
            UPressed = bin.ReadUInt32();
            UHold = bin.ReadUInt32();
            //   mouse.U = bin.ReadUInt32();
            gpMap.UPressed = bin.ReadUInt32();
            gpMap.UHold = bin.ReadUInt32();
            vrMap.UPressed = bin.ReadUInt32();
            vrMap.UHold = bin.ReadUInt32();
        }
        public void FromFrame(FrameShot f)
        {
            if (CoreTame.multiPlayer)
            {
                UPressed = f.KBPressed;
                UHold = f.KBHold;
                gpMap.UPressed = f.GPPressed;
                gpMap.UHold = f.GPHold;
                vrMap.UPressed = f.VRPressed;
                vrMap.UHold = f.VRHold;
            }
        }
        public string Export(bool[] status, string[] names)
        {
            string r = "";
            int k = 0;
            for (int i = 0; i < status.Length; i++)
                if (status[i])
                { r += k == 0 ? names[i] : ";" + names[i]; k++; }
            return r;
        }
        public string ExportAux()
        {
            return (shift ? "s;" : "") + (ctrl ? "c;" : "") + (alt ? "a;" : "");
        }
    }
}
