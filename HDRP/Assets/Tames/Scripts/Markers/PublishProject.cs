using UnityEngine;
using UnityEditor;
using System;
using System.ComponentModel;
using System.IO;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
#endif
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.Rendering;
using System.Collections;
using UnityEngine.Networking;
using Multi;
using System.Net;

namespace Markers
{

    public class PublishProject : MonoBehaviour
    {
        public string title = "";
        public string author = "";
        public string email = "";
        public string serverIP = "";
        public string serverPort = "";
        public string altPort = "";
        public string password = "";
        public string id;
        public string token;
        [SerializeField]
        [TextAreaAttribute(5, 10)]
        private string description = "";
        private bool publishType = false;
        public bool PublishType { get { return publishType; } set { publishType = value; } }

#if UNITY_EDITOR

        public void NewID()
        {
            DateTime now = DateTime.Now;
            id = now.ToString("yyyy.MM.dd.HH.mm.ss.") + now.Millisecond;
            Debug.Log(id);
        }


        public void Register()
        {
            string body = email + " " + id;
            //     WebMessage wm = WebMessage.Create(RiptideNetworking.MessageSendMode.reliable, Player.C_Register);
            //    wm.AddString(body);
            //  byte[] buffer = wm.GetByteData();
            //StartCoroutine(Send(buffer));
            NetworkManager.RegisterID = id;
            NetworkManager.RegisterEmail = email;
            NetworkManager.Token = token;
            string s = email + " " + token + " " + id;
            TCPClient.InitiateRegister(s, serverIP, altPort);
            token = "";
        }
   

        public void Render(string path)
        {
            int cc = gameObject.transform.childCount;
            Camera cam = null;
            for (int i = 0; i < cc; i++)
            {
                Transform t = gameObject.transform.GetChild(i);
                cam = t.GetComponent<Camera>();
                if (cam != null) break;
            }
            RenderTexture tex = new RenderTexture(200, 200, 24);
            if (cam != null)
            {
                cam.targetTexture = tex;
                cam.Render();
                RenderTexture.active = tex;
                Texture2D t = new Texture2D(tex.width, tex.height, TextureFormat.RGB24, false);
                t.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
                t.Apply();
                //    Destroy(t);
                byte[] b = t.EncodeToJPG();

                File.WriteAllBytes(path + "thumbnail.jpg", b);
                cam.targetTexture = null;
            }
        }
        public void CreateDescription(string path)
        {
            string assetTypeHDRP = "HDRenderPipelineAsset";
            bool hdrp = false;
            try
            {
                hdrp = GraphicsSettings.renderPipelineAsset.GetType().Name.Contains(assetTypeHDRP);
            }
            catch { hdrp = false; }
            Debug.Log("HDRP:" + hdrp);
            List<string> lines = new List<string>();
            lines.Add(CoreTame.TamesVersion + "");
            lines.Add(hdrp ? "Unity_HDRP" : "Unity_URP");
            lines.Add(title);
            lines.Add(author);
            lines.Add(id);
            //  Debug.Log("ToID:");
            lines.Add(serverIP);
            lines.Add(serverPort + "," + altPort);
            lines.AddRange(new string[] { "-", "-", "-", "-" });
            lines.Add(description);
            File.WriteAllLines(path + "description.ini", lines.ToArray());
            Render(path);
        }
#endif
    }

}
