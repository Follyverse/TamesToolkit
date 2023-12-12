using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Markers;
using Tames;
using UnityEngine;
namespace InfoUI
{
    public class InfoReference
    {
        public GameObject gameObject;
        public RefType refType;
        public TameScoreBasket basket;
        public TameScore score;
        public TameElement element;
        public TameAlternative alternative;
        public List<InfoReference> references;
        public enum RefType
        {
            Basket, Score, Element, Alternative, Object
        }
        public enum RefProperty
        {
            Name, Value, Time, Total, Label, None, Image
        }
        public static string[] Labels = new string[] { "name", "max", "label" };
        public bool Identify()
        {
            List<TameGameObject> tgos = TameManager.tgos;
            TameGameObject tgo = null;
            for (int i = 0; i < tgos.Count; i++)
                if (tgos[i].gameObject == gameObject)
                {
                    tgo = tgos[i];
                    break;
                }
            //     Debug.Log("id?: " + gameObject.name + "" + tgos.Count);
            if (tgo == null) return false;
            //      Debug.Log("id: " + tgo.gameObject.name);
            if (tgo.isElement)
            {
                refType = RefType.Element;
                element = tgo.tameParent;
        //        Debug.Log("id: hold " + element.name);
                return true;
            }

            MarkerAlterObject mao = gameObject.GetComponent<MarkerAlterObject>();
            if (mao != null)
            {
                foreach (TameAlternative ta in TameManager.altering)
                    if (ta.marker.gameObject == gameObject)
                    {
                        refType = RefType.Alternative;
                        alternative = ta;
                        return true;
                    }
            }

            foreach (TameScoreBasket tsb in TameManager.basket)
                if (tsb.marker.gameObject == gameObject)
                {
                    refType = RefType.Basket;
                    basket = tsb;
                    return true;
                }
                else
                    foreach (TameScore ts in tsb.scores)
                        if (ts.marker.gameObject == gameObject)
                        {
                            refType = RefType.Score;
                            score = ts;
                            return true;
                        }

            refType = RefType.Object;
            return true;
        }
        public string Get(RefProperty rp)
        {
            switch (rp)
            {
                case RefProperty.Time: return TameElement.ActiveTime.ToString("0");
                case RefProperty.Name: return GetName();
                case RefProperty.Value: return GetValue();
                case RefProperty.Total: return GetTotal();
                default: return "";
            }
        }
        private string GetName()
        {
            switch (refType)
            {
                case RefType.Alternative: return alternative.alternatives[alternative.current].gameObject[0].name;
                case RefType.Basket: return basket.marker.gameObject.name;
                case RefType.Score: return score.marker.name;
                case RefType.Element: return element.name;
                default: return gameObject.name;
            }
        }
        private string GetValue()
        {
            switch (refType)
            {
                case RefType.Alternative: return "" + alternative.current;
                case RefType.Basket: return "" + basket.totalScore;
                case RefType.Score: return "" + score.marker.score * score.count;
                case RefType.Element: return "" + MathF.Round(100 * element.progress.subProgress);
                default: return "";
            }
        }


        private string GetTotal()
        {

            switch (refType)
            {
                case RefType.Alternative: return "" + alternative.alternatives.Count;
                case RefType.Basket: return "" + basket.marker.passScore;
                case RefType.Score: return "" + score.marker.score * score.marker.count;
                case RefType.Element: return "" + 100;
                default: return "";
            }
        }
        private string GetTotalName()
        {
            string r = "";
            if (refType == RefType.Alternative)
            {
                foreach (TameAlternative.Alternative al in alternative.alternatives)
                    if (r.Length < al.gameObject[0].name.Length) r = al.gameObject[0].name;
            }
            else r = GetName();
            return r;
        }
        public string MaxLength()
        {
            string s = GetTotal();
            string r = "";
            for (int i = 0; i < s.Length; i++)
                r += "8";
            return r;
        }
        public string MaxName()
        {
            string s = GetTotalName();
            string r = "";
            for (int i = 0; i < s.Length; i++)
                r += "8";
            return r;
        }

    }
    public class InfoControl
    {
        public bool multiControl = false;
        public bool detached = false;
        public enum FaceCamera { None, RestrictY, Free }
        public static bool InfoVisibility = true;
        public MarkerInfo marker;
        public GameObject parent;
        public InfoFrame[] frames;
        Material material;
        public InputSetting control;
        public List<Tames.TameArea> areas;
        public int current = 0;
        bool firstUpdate = true;
        public List<InfoReference> references = new List<InfoReference>();
        //     public FaceCamera faceCamera = FaceCamera.None;
        float lastUpdate = 0;
        public Material lineMaterial;
        public const float RefUpdateInterval = 0.2f;
        public int maxLines;
        int maxIndex;
        public InfoItem[] items;
        public InfoControl(MarkerInfo m)
        {
            // multiControl = m.multiControl;
            m.SetIC(this);
            marker = m;
            detached = m.detached;
            if (CoreTame.VRMode && m.position != InfoPosition.OnObject && m.position != InfoPosition.WithObject)
            {
                parent = new GameObject(m.name);
                parent.transform.parent = CoreTame.VRAnchor.transform;
                parent.SetActive(marker.gameObject.activeSelf);
            }
            else parent = marker.gameObject;
            visible = m.gameObject.activeSelf;
            if (marker.link != null)
            {
                lineMaterial = new Material(Shader.Find("Unlit/Color"));
                lineMaterial.SetColor("_Color", marker.textHighlight);
            }
            InfoReference ir;
            for (int i = 0; i < marker.references.Length; i++)
                if (marker.references[i] != null)
                {
                    //      Debug.Log("id + " + marker.references[i].name);
                    ir = new InfoReference() { gameObject = marker.references[i] };
                    if (ir.Identify()) references.Add(ir);
                    else references.Add(null);
                }
            control = m.control;
            control.AssignControl(InputSetting.ControlType.DualPress);
            areas = new();
            List<InfoFrame> infos = new List<InfoFrame>();
            maxLines = 0;
            maxIndex = -1;
            InfoFrame f, lastFrame = null;
            List<InfoItem> its = new List<InfoItem>();
            for (int i = 0; i < marker.items.Length; i++)
            {
                f = null;
                if (marker.items[i].replace == InfoOrder.ReplaceImage && lastFrame != null)
                {
                    if (lastFrame.item.image != null)
                    {
                        f = new InfoFrame() { marker = marker, index = infos.Count, parent = this, item = marker.items[i], justImage = true, parentFrame = lastFrame };
                    }
                }
                else if (marker.items[i].replace != InfoOrder.ReplaceImage)
                {
                    f = new InfoFrame() { marker = marker, material = material, index = infos.Count, parent = this, item = marker.items[i] };
                    lastFrame = f;
                }
                if (f != null)
                {
                    infos.Add(f);
                    if (!f.justImage)
                        if (marker.items[i].lineCount > maxLines) { maxLines = marker.items[i].lineCount; maxIndex = i; }
                }
            }
            items = its.ToArray();
            frames = infos.ToArray();
            SetControls();
            Calculate();
        }
        InputSetting visibility = null;
        public static InfoControl Find(GameObject g)
        {
            foreach (InfoControl ic in TameManager.info)
                if (ic.marker.gameObject == g)
                    return ic;
            return null;
        }
        public void SetControls()
        {
            MarkerControl[] mcs = marker.gameObject.GetComponents<MarkerControl>();
            foreach (MarkerControl control in mcs)
                if (control.type == ControlTarget.Visibility)
                {
                    visibility = control.control;
                    visibility.AssignControl(InputSetting.ControlType.Mono);
                    //         Debug.Log("visi assigned " + visibility.mono.Count);
                }
        }
        public void Calculate(bool first = true)
        {
            //   Debug.Log("infor recalc");
            for (int i = 0; i < frames.Length; i++) frames[i].Reset();
            frames[maxIndex].Initialize(-1);

            for (int i = 0; i < frames.Length; i++)
                if (i != maxIndex)
                    if (!frames[i].justImage)
                        frames[i].Initialize(frames[maxIndex].lineHeight);
            if (items.Length > 0) current = 0;
            if (first)
            {
                MarkerArea ma;
                Tames.TameArea ta;

                for (int i = 0; i < marker.areas.Length; i++)
                    if (marker.areas[i] != null)
                    {
                        ta = null;
                        if ((ma = marker.areas[i].GetComponent<MarkerArea>()) != null)
                        {
                            ma.update = EditorUpdate.Fixed;
                            ma.mode = InteractionMode.Inside;
                            ma.autoPosition = false;
                            switch (ma.geometry)
                            {
                                case InteractionGeometry.Box:
                                case InteractionGeometry.Cylinder:
                                case InteractionGeometry.Sphere:
                                    ta = Tames.TameArea.ImportArea(ma.gameObject, new Tames.TameElement() { owner = marker.gameObject, tameType = TameKeys.Custom });
                                    break;
                            }
                            if (ta != null)
                                areas.Add(ta);
                        }
                    }
            }
        }
        public bool Inside(Vector3 p)
        {
            if (areas.Count == 0) return true;
            Tames.TameAreaTrack tat = Tames.TameArea.TrackWithAreas(areas, p);
            return tat.direction == 1;
        }
        public bool InView()
        {
            if (!Inside(TameCamera.camera.transform.position)) return false;
            return TameCamera.CheckDistanceAndAngle(marker.gameObject, control.maxDistance, control.maxAngle, control.axis);
        }
        private bool visible = true;
        public bool Visible
        {
            get { return visible; }
            set { visible = value; }
        }
        public bool isInView = false;
        private bool indexChanged = false;

        private void Change(int dir)
        {
            indexChanged = false;
            //     Debug.Log(marker.name + " " + dir);
            if (frames.Length > 0)
            {
                if (dir < 0)
                {
                    if (!frames[current].GoPrev())
                    {
                        current = current < 0 ? frames.Length - 1 : (current + frames.Length - 1) % frames.Length;
                        frames[current].Enter(dir);
                        indexChanged |= true;
                    }
                }
                else if (!frames[current].GoNext())
                {
                    current = current < 0 ? 0 : (current + 1) % frames.Length;
                    frames[current].Enter(dir);
                    indexChanged = true;
                }
            }
        }
        public void Update()
        {
            if (visibility != null)
            {
                if (visibility.CheckMono(marker.gameObject, multiControl))
                {
               //     Debug.Log("visible bef: " + visible + " " + visibility.mono.Count);
                    visible = !visible;
                //   Debug.Log("visible: " + visible);
                   parent.SetActive(visible);
                }
            }
            bool vis = detached ? visible : visible && isInView;
            if (vis)
            {
                int d = control.CheckDualPressed(marker.gameObject, multiControl);

                if (d != 0) Change(d);
                int replace = 0;
                if (frames[current].type == InfoFrame.ItemType.Object)
                    frames[current].SetInstancePosition();
                frames[current].UpdateChoice();
                if (marker.position == InfoPosition.OnObject)
                {
                    Vector3 p = Camera.main.WorldToScreenPoint(marker.transform.position);
                    for (int i = 0; i < frames.Length; i++)
                        frames[i].MoveTo(p.x, p.y, frames[0].outer.xMin, frames[0].outer.yMin);
                }
                if (TameElement.ActiveTime - lastUpdate > RefUpdateInterval)
                {
                    frames[current].UpdateReferences();
                    lastUpdate = TameElement.ActiveTime;
                }
                if (marker.link != null) frames[current].UpdateLine();
                if (current < frames.Length)
                    for (int i = frames.Length - 1; i >= 0; i--)
                    {
                        if (!frames[i].justImage)
                        {
                            if (i > current)
                                frames[i].owner.SetActive(false);
                            else switch (replace)
                                {
                                    case 0:
                                        frames[i].Show(true, i < current, frames[current].justImage);
                                        replace = frames[i].GetReplace();
                                        break;
                                    case 1:
                                        frames[i].Show(false);
                                        replace = frames[i].GetReplace();
                                        break;
                                    case 2:
                                        frames[i].Show(false);
                                        break;
                                }
                        }
                        //       if (1 == current && 1 == i) Debug.Log(marker.name + " current = " + i + " " + replace + " " + frames[i].owner.activeSelf);

                    }
            }
            else
                for (int i = 0; i < frames.Length; i++) if (!frames[i].justImage) frames[i].owner.SetActive(false);
        }
        public void UpdateMarker()
        {
            foreach (InfoFrame frame in frames)
            {
                frame.UpdateColors();
            }
        }
        public void AddStatus(RiptideNetworking.Message m)
        {
            int choice;
            for (int i = 0; i < frames.Length; i++)
                if (frames[i].choice.Count > 0)
                {
                    choice = 0;
                    for (int j = 0; j < frames[i].choice.Count; j++)
                        choice += frames[i].choice[j].selected ? 1 << j : 0;
                }
        }
    }
}
