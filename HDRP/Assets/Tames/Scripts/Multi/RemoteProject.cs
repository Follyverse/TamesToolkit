using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using UnityEngine;
namespace Multi
{
    public enum RemoteType
    {
        Student, Research, Construction
    }
   
    public class RemoteProject
    {
        public string name;
        public string id;
        public int index;
        public long created;
        public long lastModified;
        public string description;
        public string recipient;
        public List<PersonClient> users = new List<PersonClient>();
        public int maxUsers = 8;
        public Records.FrameShot[] frames;
        public DateTime lastChecked;
        public RemoteProject()
        {
            lastChecked = DateTime.Now;
        }
        public PersonClient FindByID(ushort id, out int index)
        {
            index = -1;
            for (int i = 0; i < users.Count; i++)
                if (id == users[i].id)
                {
                    index = i;
                    return users[i];
                }
            return null;
        }
        public void Disconnect(PersonClient pc)
        {
            users.Remove(pc);
        }
        public void PrepareFrame()
        {

        }
     
        public void CheckConnection()
        {
            if (DateTime.Now - lastChecked > TimeSpan.FromMinutes(1))
            {
                lastChecked = DateTime.Now;
                for (int i = users.Count - 1; i >= 1; i--)
                    if (DateTime.Now - users[i].lastSignal > TimeSpan.FromSeconds(300))
                        Disconnect(users[i]);
            }        
        }
    }
}
