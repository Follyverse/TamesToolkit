using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Markers
{
    public class ParentElement
    {
        public GameObject element;
        public string trigger;
    }
 
    public class MarkerParent : MonoBehaviour
    {
        public const int TypeID = 13;
        public bool interactive = true;
        public bool trackables = true;
        public bool elements = true;
        public bool input = true;
        public InputSetting swap;
    }
}
