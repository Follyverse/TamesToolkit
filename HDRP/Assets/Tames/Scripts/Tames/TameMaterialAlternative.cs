using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Markers;

namespace Tames
{
    public class TameMaterialAlternative : TameAlternative
    {
        public new MarkerAlterMaterial marker;
        /// <summary>
        /// The alternative inventory
        /// </summary>
        public new Material[] alternatives;
        /// <summary>
        /// The changed material. The properties of this material are copied from the current alternative.  
        /// </summary>
        public Material target;
        /// <summary>
        /// The index of the current alternative.
        /// </summary>
        public TameMaterialAlternative()
        {
            thingType = ThingType.Alter;
            thingSubtype = ThingSubtype.Material;
        }
        /// <summary>
        /// Sets the <see cref="initial"/> alternative.
        /// </summary>
        /// <param name="i"></param>
        /// <summary>
        /// Updates the material (by copying the current alternative's property)
        /// </summary>
        override public void Apply()
        {
            if (current >= 0)
                target.CopyPropertiesFromMaterial(alternatives[current]);
        }

        override public void Update()
        {
            if ((count <= 0) || (current < 0))
                return;
            int d = CurrentUpdater.Directable();
            // if (d != 0) Debug.Log("alter " + d);
            if (d < 0) GoPrevious();
            else if (d > 0) GoNext();
        }
        /// <summary>
        /// Finds and creates all alternatives in the scene.
        /// </summary>
        /// <param name="tgos">The list of <see cref="TameGameObject"/>s extracted in <see cref="TameManager"/></param>
        /// <returns></returns>
        public new static List<TameMaterialAlternative> GetAlternatives(List<TameGameObject> tgos)
        {
            List<TameMaterialAlternative> tmas = new List<TameMaterialAlternative>();
            TameMaterialAlternative tma;
            MarkerAlterMaterial mam;
            for (int i = 0; i < tgos.Count; i++)
                if ((mam = tgos[i].gameObject.GetComponent<MarkerAlterMaterial>()) != null)
                {
                    MarkerControl mc = mam.gameObject.GetComponent<MarkerControl>();
                    tma = new() { marker = mam, multiControl = mam.multiControl, cycle = mam.cycle };
                    tma.markerControl = mc;
                    if (mc != null) tma.SetKeys();
                    tma.alternatives = mam.alternatives;
                    tma.target = mam.applyTo;
                    if (mam.initial == null)
                        tma.initial = tma.alternatives.Length > 0 ? 0 : -1;
                    else
                        for (int j = 0; j < tma.alternatives.Length; j++)
                            if (tma.alternatives[j] == mam.initial)
                                tma.initial = j;

                    for (int j = 0; j < tma.alternatives.Length; j++)
                        if (tma.alternatives[j] == mam.applyTo)
                        {
                            MaterialReference mr = MaterialReference.AddToReference(tma.alternatives[j]);
                            tma.target = mr.clone;
                        }
                    tma.count = tma.alternatives.Length;
                    tma.name = mam.name;
                    tma.SetInitial(tma.initial);
                    tmas.Add(tma);
                }
            return tmas;
        }

    }
}
