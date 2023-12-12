using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Tames;

namespace Markers
{
    public class MarkerTeleport : MonoBehaviour
    {
        public const int TypeID = 20;
        public bool randomInitial = false;
        public bool asCarrier = false;
        public bool toChildren = false;
        public GameObject[] portals;
        public InputSetting control;
        private int current = 0;
        private List<Transform> points = new List<Transform>();
        public TameTeleport tame;
        public Transform Point(int index)
        {
            return points[index];
        }
        
        //  private GameObject normal
        public void StartIt()
        {
            tame = new TameTeleport() { marker=this};
       //     Debug.Log("teleport " + name);
            if (toChildren)
                for (int i = 0; i < gameObject.transform.childCount; i++)
                    points.Add(gameObject.transform.GetChild(i));
            for (int i = 0; i < portals.Length; i++)
                if (portals[i].transform.parent != gameObject.transform)
                    points.Add(portals[i].transform);
        }
        public void Check()
        {
            if (points.Count == 0) return;
            int dir = control.CheckDualPressed(null);
            if (dir != 0)
            {
                Debug.Log("tele " + name + " " + dir);
                current = (current + dir + points.Count) % points.Count;
                TameCamera.TeleportTo(this, current);
            }
        }
        public void Next()
        {
            current = (current + 1) % points.Count;
            TameCamera.TeleportTo(this, current);
        }
        public void Random()
        {
            DateTime dt = DateTime.Now;
            Debug.Log("telerrp: " + points.Count);
            if (points.Count > 0)
            {
                current = dt.Second % points.Count;
                TameCamera.TeleportTo(this,current);
                Debug.Log("telep: "+current);
            }
        }
    }
}