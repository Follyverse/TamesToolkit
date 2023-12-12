using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
#if !PLATFORM_WEBGL
using UnityEngine.Windows.Speech;
#endif
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Tames
{
    public class VoiceCommands
    {
#if !PLATFORM_WEBGL
     private  KeywordRecognizer rec;
#endif     
        private Dictionary<string, ButtonControl> pairs = new Dictionary<string, ButtonControl>();
        public static bool used = false;
        public static int frame = -1;
        public static ButtonControl key = null;
        private bool loadedOnce = false;
        public void Initialize()
        {
            if (loadedOnce)
            {
                Debug.Log("voice rerun");
                return;
            }
            else
            {
                loadedOnce = true;
                Debug.Log("initializing voice");
            }
            if (Keyboard.current != null)
            {
                Add("one,van,won", Keyboard.current.digit1Key);
                Add("to,too,two", Keyboard.current.digit2Key);
                Add("tree,free,three,sri", Keyboard.current.digit3Key);
                Add("four,for", Keyboard.current.digit4Key);
                Add("five", Keyboard.current.digit5Key);
                Add("six,sex", Keyboard.current.digit6Key);
                Add("seven", Keyboard.current.digit7Key);
                Add("eight,ate", Keyboard.current.digit8Key);
                Add("nine", Keyboard.current.digit9Key);
                Add("zero", Keyboard.current.digit0Key);
#if !PLATFORM_WEBGL
                rec = new KeywordRecognizer(pairs.Keys.ToArray());
                rec.OnPhraseRecognized += SpeechRecognized;
                rec.Start();
#endif 
            }
        }
        private void Add(string s, ButtonControl b)
        {
            string[] key = s.Split(",");
            foreach (string key2 in key)
            {
                pairs.Add(key2, b);
      //          Debug.Log("added: " + key2);
            }
        }
#if !PLATFORM_WEBGL
        private void SpeechRecognized(PhraseRecognizedEventArgs args)
            {
                Debug.Log(args.text);
                foreach (KeyValuePair<string, ButtonControl> pair in pairs)
                    if (pair.Key == args.text)
                    {
                        used = false;
                        key = pair.Value;
                    }
             }
#endif
    }
}
