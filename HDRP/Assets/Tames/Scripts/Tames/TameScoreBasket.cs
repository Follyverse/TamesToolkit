using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Markers;
using InfoUI;

namespace Tames
{
    public class ScoreBase : TameThing
    {
        public MarkerScore marker;
        public TameElement element = null;
        public TameElement activateAfter = null, showAfter = null;
        public GameObject show = null;
        private InfoUI.InfoControl showInfo = null;
        public bool fulfilled = false;
        public bool active = false;
        internal InfoFrame frame = null;
        public void CheckShow()
        {
            if (show != null)
                foreach (InfoUI.InfoControl info in TameManager.info)
                    if (show == info.marker.gameObject)
                    {
                        showInfo = info;
                        break;
                    }
        }
        public void Fulfill(bool f)
        {
            if (element != null) element.progress.active = f;
            if (showInfo != null)
                showInfo.Visible = f;
            if (show != null) show.SetActive(f);
            //     if(show!=null) Debug.Log(show.activeSelf);
        }
        public void FindElements(List<TameGameObject> tgos)
        {
            TameGameObject tg = TameGameObject.Find(marker.activate, tgos);
            if (tg != null)
                if (tg.isElement)
                    element = tg.tameParent;
            tg = TameGameObject.Find(marker.showAfter, tgos);
            if (tg != null)
            {
                if (tg.isElement)
                    showAfter = tg.tameParent;

            }
            tg = TameGameObject.Find(marker.activateAfter, tgos);
            if (tg != null)
                if (tg.isElement)
                    activateAfter = tg.tameParent;
            active = activateAfter == null;
            marker.gameObject.SetActive(showAfter == null);
            //   Debug.Log(marker.gameObject.name + " > " + (showAfter == null));
        }

    }
    public class TameScore : ScoreBase
    {
        public TameScoreBasket parent;
        public InputSetting control;
        public TameScore after;
        public float lastPassed = -1;
        public int count = 0;
        public float interval;
        public int lastAfterCount = 0;
        public float score = 0;

        public TameScore(MarkerScore ms)
        {
            marker = ms;
            marker.control.AssignControl(InputSetting.ControlType.Mono);
            show = ms.show;
            interval = ms.interval > 0 ? ms.interval : 10;
            control = marker.control;
            //    Debug.Log(marker.name + " " + control.mono.Count);
            TameGameObject tg = TameGameObject.Find(marker.showAfter, TameManager.tgos);
            InfoControl ic;
            if (tg != null)
            {
                if (tg.isElement)
                    showAfter = tg.tameParent;
                if ((ic = InfoControl.Find(tg.gameObject)) != null)
                    for (int i = 0; i < ic.frames.Length; i++)
                        if (ic.frames[i].choice.Count > 0)
                        { frame = ic.frames[i]; break; }
            }
        }

        public override int CurrentIndex()
        {
            return count;
        }

        public bool Update()
        {
            bool check, visible = true, passed = false;
            //      if(fulfilled) Debug.Log("fulfilled " + marker.name + " " + fulfilled);
            if (frame != null)
            {
                score = 0;
                for (int i = 0; i < frame.choice.Count; i++)
                    if (frame.choice[i].selected)
                        score += marker.choiceScore.Length > i ? marker.choiceScore[i] : 0;
            }
            else
            {
                if (fulfilled) return false;
                if (activateAfter != null)
                    active = activateAfter.progress.progress > 0.99f;
                if (showAfter != null)
                    marker.gameObject.SetActive(visible = showAfter.progress.progress > 0.99f);
                if (active)
                {
                    if ((lastPassed < 0) || (TameElement.ActiveTime - lastPassed >= interval))
                    {
                        check = after == null;
                        if (!check)
                            if (after.count > lastAfterCount) check = true;
                        if (check)
                            if (control.CheckMono(marker.gameObject))
                            {
                                lastPassed = TameElement.ActiveTime;
                                count++;
                                score = count * marker.score;
                                fulfilled = count == marker.count;
                                lastAfterCount = after != null ? after.count : 0;
                                //        Debug.Log("updating score " + marker.name + " " + fulfilled);
                                passed = true;
                            }
                    }
                    Fulfill(passed);
                }
            }
            return passed;
        }
    }

    public class TameScoreBasket : ScoreBase
    {

        public List<TameScore> scores = new();
        public float totalScore = 0;
        public TameScoreBasket(MarkerScore ms)
        {
            marker = ms;
            show = ms.show;
            name = ms.name;
        }
        public void Update()
        {
            if (fulfilled) return;
            if (activateAfter != null)
                active = activateAfter.progress.progress > 0.99f;
            foreach (TameScore ts in scores)
            {
                if (ts.Update())
                {
                    totalScore += ts.marker.score;
                    //   Debug.Log("updating from " + totalScore);
                }
            }
            if (totalScore >= marker.passScore)
            {
                fulfilled = true;
                Fulfill(true);
                Debug.Log("fulfilled");
            }
        }
    }

}