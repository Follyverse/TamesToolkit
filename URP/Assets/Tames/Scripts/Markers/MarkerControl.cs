using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace Markers
{
    public enum ControlTarget
    {
        Progress, Activation, Visibility, Alter
    }
    public enum ControlType
    {
        Manual, Element, Object, Time
    }
    public class MarkerControl : MonoBehaviour
    {
        public const int TypeID = 5;
        public ControlType feature;
        public ControlTarget type;
        // activation/visibility
        public bool initial = true;
        // alter
        public float interval = 1;
        // manual
        public InputSetting control;
        // element
        public GameObject parent;
        public string trigger;
        // object
        public bool withPeople = false;
        public bool withPeoploids = false;
        public GameObject[] trackables;
    }

}
