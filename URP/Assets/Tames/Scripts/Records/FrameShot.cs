using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace Records
{
    public class FrameShot
    {
        public float time;
        public Vector3 cpos;
        public Quaternion crot;
        public Vector3[] hpos;
        public Quaternion[] hrot;
        //  public float[] grip;
        public uint GPHold, KBHold, GPPressed, KBPressed, VRPressed, VRHold;
        public byte aux, grip;
        //  public uint mouse;
        public FrameShot()
        {
            hpos = new Vector3[2];
            hrot = new Quaternion[2];
        }
        public bool Grip(int index)
        {
            return (VRMap.Gripped(VRHold, index));
        }
        public void GetSpatial(Multi.Person person)
        {
            cpos = person.headPosition;
            crot = person.headRotation;
            hpos = new Vector3[] { person.position[0], person.position[1] };
            hrot = new Quaternion[] { person.rotation[0], person.rotation[1] };
        }
        public void Write(BinaryWriter bin)
        {

        }
        public bool Changed(FrameShot prev)
        {
            
            return false;
        }
        public bool Changed(FrameShot prev, Markers.ExportOption eo)
        {
            if (eo.actionKeys)
            {
                
            }
            return false;
        }
    }
}
