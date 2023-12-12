using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Markers;
namespace Tames
{
    public class Updater
    {
        public static Updater[] AllUpdaters;
        //  public InputSetting.ControlType effectType;
        public ushort sourceType;
        public TameThing source;
        public TameThing target;

        //
        public bool isGrippable = false;
        public bool isSwitch = false;
        public bool isDistance = false;
         public bool IsInteractive { get { return isDistance || isGrippable || isSwitch || sourceType == TrackBasis.Head; } }
        //

        public int direction = 1;
        public float interval = 1;
        public Vector3 position;

        public Interaction interaction;

        public GameObject Body { get { return ((UpdaterTrack)this).bodies[((UpdaterTrack)this).index]; } }
        //  public TameGameObject Body { get { return ((UpdaterTrack)this).body; } }
        public class Interaction
        {
            public bool real;
            public int index;
            public int hand;
            public int area;
            public Interaction(int i, int h, int a, bool r)
            {
                index = i;
                hand = h;
                area = a;
                real = r;

            }
        }

        public Updater(TameThing owner, ushort b)
        {
            sourceType = b;
            target = owner;
            switch (b)
            {
                case TrackBasis.Grip:
                    isGrippable = true;
                    break;
                case TrackBasis.Distance:
                    isDistance = true;
                    sourceType = TrackBasis.Time;
                    break;
                case TrackBasis.Switch:
                    isSwitch = true;
                    sourceType = TrackBasis.Time;
                    break;
                case TrackBasis.Time:
                    sourceType = TrackBasis.Time;
                    break;
             }
        }
        public static Updater Time(TameThing target)
        {
            return new Updater(target, TrackBasis.Time);
        }
        public virtual int Direction() { return direction; }
        public virtual int DirectionChaage() { return direction; }
        public virtual float Delta() { return 0; }
        public virtual float Progress() { return 0; }
        public virtual int Index() { return 0; }

        public void Apply()
        {
            TameElement.Apply(this);
        }
        public int Active()
        {
            if (sourceType == TrackBasis.Tame)
            {
                UpdaterElement tee = (UpdaterElement)this;
                TameElement te = (TameElement)source;
                if (tee.trigger != null)
                    return tee.trigger.Direction(te.progress.subProgress);
                else
                    if (te.progress.active) return 1; else return -1;
            }
            return 0;
        }
        public bool Changed()
        {
            switch (sourceType)
            {
                case TrackBasis.Alter:
                    TameAlternative ta = (TameAlternative)source;
                    return (ta.current == ta.count - 1 && ta.LastIndex() != ta.current);
                case TrackBasis.Tame:
                    TameElement te = (TameElement)source;
                    return (te.progress.progress == 1 && te.progress.lastProgress != 1);
                case TrackBasis.Manual:
                    UpdaterInput tie = (UpdaterInput)this;
                    bool multi = target.thingType != ThingType.Info;
                    return tie.control.CheckMono(target.owner, multi);
                default: return false;
            }
        }
        public int Directable()
        {
            switch (sourceType)
            {
                case TrackBasis.Alter:
                    TameAlternative ta = (TameAlternative)source;
                    if (ta.TotalIndex() > ta.LastTotalIndex()) return 1;
                    if (ta.TotalIndex() < ta.LastTotalIndex()) return -1;
                    return 0;
                case TrackBasis.Tame:
                    TameElement te = (TameElement)source;
                    if ((int)(te.progress.totalProgress / interval) > (int)(te.progress.lastTotal / interval)) return 1;
                    else if ((int)(te.progress.totalProgress / interval) < (int)(te.progress.lastTotal / interval)) return -1;
                    else return 0;
                case TrackBasis.Manual:
                    UpdaterInput tie = (UpdaterInput)this;
                    bool multi = target.thingType != ThingType.Info;
                //   if(target.name=="theme alter")     Debug.Log(target.name + " " + sourceType +" "+ tie.control.back.Count);
                    if (tie.holdable)
                        return tie.control.CheckDualHeld(target.owner, multi);
                    else
                        return tie.control.CheckDualPressed(target.owner, multi);

                default: return 0;
            }
        }
        public static Updater FromControlMarker(TameThing owner, MarkerControl mc, InputSetting.ControlType ct)
        {
            switch (mc.feature)
            {
                case ControlType.Object:
                    return null;
                case ControlType.Element:
                    if (mc.parent == null) return null;
                    TameThing t = TameManager.FindThing(mc.parent);
                    if (t != null)
                    {
                        UpdaterElement tee = new UpdaterElement(owner, t);
                        if (tee.sourceType != TrackBasis.Error) return tee;
                    }
                    return null;
                case ControlType.Manual:
                    UpdaterInput ui = new UpdaterInput(owner, mc.control, false);
                    return ui;
            }
            return null;
        }
    }
   
    public class UpdaterTrack : Updater
    {
        public GameObject[] bodies;
        public int index;
        public UpdaterTrack(TameThing owner) : base(owner, TrackBasis.Object)
        {

        }
        public UpdaterTrack(TameThing owner, GameObject[] g) : base(owner, TrackBasis.Object)
        {
            bodies = g;
            index = 0;
        }
        public UpdaterTrack(TameThing owner, GameObject go) : base(owner, TrackBasis.Object)
        {
            bodies = new GameObject[] { go };
            index = 0;
        }
        public UpdaterTrack(TameThing owner, TameGameObject tgo) : base(owner, TrackBasis.Object)
        {
            bodies = new GameObject[] { tgo.gameObject };
            index = 0;
        }
        public UpdaterTrack(TameThing owner, List<TameGameObject> tgos) : base(owner, TrackBasis.Object)
        {
            bodies = new GameObject[tgos.Count];
            index = 0;
            for (int i = 0; i < tgos.Count; i++)
                bodies[i] = tgos[i].gameObject;
        }
    }
    public class UpdaterElement : Updater
    {
        public TameTrigger trigger;
        public UpdaterElement(TameThing owner, TameElement te) : base(owner, TrackBasis.Tame)
        {
            source = te;
        }
        public UpdaterElement(TameThing owner, TameThing te) : base(owner, TrackBasis.Tame)
        {
            source = te;
            switch (te.thingType)
            {
                case ThingType.Element: sourceType = TrackBasis.Tame; break;
                case ThingType.Alter: sourceType = TrackBasis.Alter; break;
                //             case ThingType.Teleport: sourceType = TrackBasis.Teleport;break;
                //         case ThingType.Basket: sourceType = TrackBasis.
                default: sourceType = TrackBasis.Error; break;
            }
            //     effect = e == ManifestKeys.Update ? 0 : (e == ManifestKeys.Slide ? 1 : 2);
        }

    }

    public class UpdaterInput : Updater
    {
        public InputSetting control;
        public bool holdable;
        public UpdaterInput(TameThing owner, InputSetting input, bool hold) : base(owner, TrackBasis.Manual)
        {
            control = input;
            holdable = hold;
        }
    }
   
}
