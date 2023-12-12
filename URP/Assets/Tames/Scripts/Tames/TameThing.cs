using Markers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace Tames
{
 
    public enum ThingType
    {
        Element, Basket, Alter, Teleport, Interaction, Info
    }
    public enum ThingSubtype
    {
        Mechanical, Material, Light, Custom, Basket, Score
    }
   
    public class TameThing
    {

        /// <summary>
        /// name of the element, usually the same as the name of the associated game object or material
        /// </summary>
        public string name;
        public ThingType thingType;
        public ThingSubtype thingSubtype;
        public const int Mechanical = 1;
        public const int Material = 2;
        public const int Light = 3;
        public const int Custom = 4;

        public GameObject owner;
        public bool baseVisibility = true;
        //    public List<Fulfillment> fulfillment = new List<Fulfillment>();

        public int updaterIndex = 0;
      //  public List<Updater> currentEffects = new List<Updater>();
        public List<Updater> updaters = new List<Updater>();
        public bool Manual { get { return CurrentUpdater.sourceType == TrackBasis.Manual ; } }
        public bool HasParent { get { return updaters.Count > 0; } }
        public Updater CurrentUpdater { get { return updaters[updaterIndex]; } }
 
        public List<MarkerControl> updateMarkers = new List<MarkerControl>();
        public MarkerControl visMarker, actMarker, altMarker;
       // public ControlParent control = null;
        //     public GameObject gameObject;
     
        public virtual float TotalProgress()
        {
            return 0;
        }
        public virtual float LastProgress()
        {
            return 0;
        }
        public virtual float Progress()
        {
            return 0;
        }
        public virtual float LastTotal()
        {
            return 0;
        }
        public virtual int CurrentIndex()
        {
            return 0;
        }
        public virtual int LastIndex()
        {
            return 0;
        }
        public virtual int LastTotalIndex()
        {
            return 0;
        }
        public virtual int TotalIndex()
        {
            return 0;
        }
      public  void AddTime()
        {
            // parents.Clear();
            // basis = TrackBasis.Time;
            updaters.Add(new Updater(this, TrackBasis.Time));
            //basis[1] = basis[2] = TrackBasis.Error;

        }
        public void InputUpdate(InputSetting input)
        {
            updaters.Add(new UpdaterInput(this, input, true));
        }
        public void InputUpdate(InputSetting input, InputSetting.ControlType ct)
        {
            updaters.Add(new UpdaterInput(this, input, ct == InputSetting.ControlType.DualHold?true:false));
        }
        public void MonoUpdate()
        {
            updaters.Add(new Updater(this, TrackBasis.Head));

        }
        public void MonoUpdate(TameGameObject tgo)
        {
            //    TameEffect tp;
            //   parents.Clear();
            //  basis = TrackBasis.Object;
            //  parents.Add(new TameTrackEffect(tgo));
            updaters.Add(new UpdaterTrack(this, tgo));
        }
        public void MonoUpdate(GameObject tgo)
        {
            //    TameEffect tp;
            //   parents.Clear();
            //  basis = TrackBasis.Object;
            //  parents.Add(new TameTrackEffect(tgo));
            updaters.Add(new UpdaterTrack(this, tgo));
        }
        public void MonoUpdate(TameElement te)
        {
            //    TameEffect tp;
            //     parents.Clear();
            //     basis = TrackBasis.Tame;
            //      parents.Add(new TameElementEffect(te));
            updaters.Add(new UpdaterElement(this, te));
        }
        public void MonoUpdate(TameElement te, string trig, float interval)
        {
            //    TameEffect tp;
            //     parents.Clear();
            //     basis = TrackBasis.Tame;
            //      parents.Add(new TameElementEffect(te));
            Updater p = new UpdaterElement(this, te);
            UpdaterElement tee = (UpdaterElement)p;
            tee.trigger = TameTrigger.TriggerFromText(trig);
            tee.interval = interval;
            updaters.Add(p);
        }
    }
    /*
     *  
     */
}