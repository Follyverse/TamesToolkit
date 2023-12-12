using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Tames;
namespace Markers
{
    public class MarkerMedia : MonoBehaviour
    {
        public const int TypeID = 12;
        public enum Activation { ParentActive, ParentStart, ParentEnd, Manual, Interaction }
        public enum Repetition { Once, Consecutive, Loop, Last, Interval }
        public Activation activation;
        public Repetition repetition;
        public AudioClip[] sounds;
        public float interval;
        public InputSetting control;
        public GameObject Area;
        private TameArea area;
        public TameThing parent;
        private AudioSource source;
        private int count = 0;
        private int current = 0;
        private float lastPlayed = 0;
        private bool firstStart = true;
        public void StartIt()
        {
            control.AssignControl(InputSetting.ControlType.Mono);
        }
        private void FixedUpdate()
        {
            if (CoreTame.loadStatus!= CoreTame.LoadStatus.Ready) return;
            if (firstStart)
            {
                StartIt();
                firstStart = false;
                if (Area != null)
                {
                    area = TameManager.area.Find(x => x.gameObject == Area);
                    if (area.mode != InteractionMode.Inside)
                        area = null;
                }
                source = TameManager.audioSource;
                if (source != null)
                {
                    source.loop = false;
                    source.playOnAwake = false;
                }
            }
            else
                CheckActivation();
        }
        public void CheckActivation()
        {
            if (sounds.Length == 0 || source == null) return;
            if (area != null)
                if (!area.Inside(TameCamera.cameraTransform.position))
                    source.Pause();
            switch (activation)
            {
                case Activation.ParentActive:
                    switch (parent.thingType)
                    {
                        case ThingType.Element:
                            TameElement te = (TameElement)parent;
                            if (te.progress.active)
                            {
                                if (TameManager.clipSource != this)
                                {
                                    TameManager.clipSource = this;
                                    source.clip = sounds[0];
                                }
                                if (!source.isPlaying) source.Play();
                            }
                            else if (source.isPlaying) source.Pause();
                            break;
                    }
                    break;
                case Activation.ParentStart:
                    switch (parent.thingType)
                    {
                        case ThingType.Element:
                            TameElement te = (TameElement)parent;
                            if (te.progress.justCycled == 1)
                            {
                                if (RepetitionAllowed())
                                    if (!source.isPlaying) source.Play();
                            }
                            break;
                    }
                    break;
                case Activation.ParentEnd:
                    switch (parent.thingType)
                    {
                        case ThingType.Element:
                            TameElement te = (TameElement)parent;
                            if (te.progress.justCycled == -1)
                                if (RepetitionAllowed())
                                    if (!source.isPlaying) source.Play();
                            break;
                    }
                    break;
                case Activation.Manual:
                    //      Debug.Log("checking activation");
                    if (control.CheckMono(gameObject, false))
                    {
                        Debug.Log("checking activation");
                        if (source.isPlaying) source.Pause();
                        else if (RepetitionAllowed())
                            source.Play();
                    }
                    break;
                case Activation.Interaction:
                    if (area != null)
                        if (area.Entered(CoreTame.localPerson.lastHead, CoreTame.localPerson.headPosition))
                            if (!source.isPlaying) if (RepetitionAllowed())
                                    source.Play();
                    break;
            }
        }
        private bool RepetitionAllowed()
        {
            bool r = false;
            switch (repetition)
            {
                case Repetition.Once:
                    if (count == 0) { count = 1; current = 0; source.clip = sounds[current]; TameManager.clipSource = this; r = true; }
                    else
                        r = false;
                    break;
                case Repetition.Consecutive:
                    if (count < sounds.Length) { source.clip = sounds[count]; count++; current++; TameManager.clipSource = this; r = true; }
                    else
                        r = false;
                    break;
                case Repetition.Last:
                    if (count < sounds.Length - 1) { source.clip = sounds[current]; count++; current++; r = true; }
                    else { current = sounds.Length - 1; source.clip = sounds[current]; count = sounds.Length; r = true; }
                    TameManager.clipSource = this;
                    break;
                case Repetition.Loop:
                    current = count % sounds.Length;
                    source.clip = sounds[current];
                    count++;
                    TameManager.clipSource = this;
                    r = true;
                    break;
                case Repetition.Interval:
                    if (TameElement.ActiveTime > lastPlayed + interval)
                    {
                        source.clip = sounds[0];
                        TameManager.clipSource = this;
                        r = true;
                    }
                    else
                        r = false;
                    break;
            }
            if (!r && activation != Activation.Manual)
                if (control.mono.Count > 0)
                    if (control.CheckMono(gameObject, false))
                    {
                        TameManager.clipSource = this;
                        source.clip = sounds[current];
                        r = true;
                    }
            return r;
        }
    }
}
