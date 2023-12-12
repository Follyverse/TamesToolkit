using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.XR.Management;
using UnityEngine;
using System.Collections;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEditor.AddressableAssets.Build;
#endif
public class DLCManager : MonoBehaviour
{
#if UNITY_EDITOR
    private static AddressableAssetSettings settings;
    static Markers.PublishProject PublishSettings;
    public static void DLCCreate(Markers.PublishProject pp)
    {
        string projectPath = Application.dataPath;
        if (pp != null) PublishSettings = pp;
        else
        {
            GameObject[] roots = EditorSceneManager.GetActiveScene().GetRootGameObjects();
            PublishSettings = null;
            foreach (GameObject root in roots)
                if ((PublishSettings = root.GetComponent<Markers.PublishProject>()) != null)
                    break;
        }
        if (PublishSettings == null) return;
        AddressableAssetGroup group = null;
        settings = AddressableAssetSettingsDefaultObject.Settings;
        string profileID = "";
        foreach (var g in settings.groups)
        {
            Debug.Log("group " + g.name);
            if (g.name.StartsWith("Default"))
            {
                group = g;
                break;
            }
        }
        if (group != null)
        {
            List<string> pns = settings.profileSettings.GetAllProfileNames();
            foreach (string p in pns)
            {
                Debug.Log("profile: " + p);
                if (p.StartsWith("Default"))
                {
                    profileID = settings.profileSettings.GetProfileId(p);
                    break;
                }
            }
        }
        bool success = false;
        if (profileID != "")
        {

            for (int i = 0; i < settings.DataBuilders.Count; i++)
                if (PublishSettings.PublishType)
                {
                    if (settings.DataBuilders[i].name == "BuildScriptPackedMode")
                    {
                        settings.ActivePlayerDataBuilderIndex = i;
                        success = true;
                        break; //   Debug.Log("bulder: "+db.name);
                    }
                }
                else if (settings.DataBuilders[i].name == "BuildScriptPackedMode")
                {
                    success = true;
                    settings.ActivePlayerDataBuilderIndex = i;
                    break;
                }
        }
        if (success)
            BuildAddressableContent(projectPath);

    }
    //  string assetPath = AssetDatabase.GetAssetPath(asset);
    //  string assetGUID = AssetDatabase.AssetPathToGUID(EditorSceneManager.GetActiveScene().path);
    //   AssetReference ar = settings.CreateAssetReference(assetGUID);
    public static string build_script
            = "Assets/AddressableAssetsData/DataBuilders/BuildScriptPackedMode.asset";
    public static string settings_asset
        = "Assets/AddressableAssetsData/AddressableAssetSettings.asset";
    public static string profile_name = "Default";


    static bool BuildAddressableContent(string projectPath)
    {

        AddressableAssetSettings
            .BuildPlayerContent(out AddressablesPlayerBuildResult result);

        bool success = string.IsNullOrEmpty(result.Error);
        if (success)
        {
            string outPath = Directory.GetParent(result.OutputPath).ToString();
            if (outPath[^1] != '\\' && outPath[^1] != '/') outPath += '/';
            PublishSettings.CreateDescription(outPath);
            string destPath = EditorUtility.OpenFolderPanel("Choose destination ...", projectPath, "");
            if (destPath != "")
            {
                if (destPath[^1] != '\\' && destPath[^1] != '/') destPath += '/';
                // string pout = Directory.GetParent(result.OutputPath);
                ZipFile.CreateFromDirectory(outPath, destPath + PublishSettings.id + ".zip");

                //      CopyEverything( Directory.GetParent(result.OutputPath).ToString(), destPath);
            }
            Debug.Log("output: " + result.OutputPath + " :: " + projectPath + "::" + Directory.GetParent(result.OutputPath));
        }

        else
        {
            Debug.LogError("Addressables build error encountered: " + result.Error);
        }
        return success;
    }
   
    static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
    {
        foreach (DirectoryInfo dir in source.GetDirectories())
            CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
        foreach (FileInfo file in source.GetFiles())
            file.CopyTo(Path.Combine(target.FullName, file.Name));
    }




#endif
    public static GameObject Rig = null;
    public static GameObject EventSystem = null;
    public static void GetDefaultObjects()
    {
        OriginalScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        GameObject[] roots = OriginalScene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
            if (roots[i].name != "Rig" && roots[i].name != "Event System")
                continue;//      Destroy(roots[i]);
            else if (roots[i].name == "Rig") Rig = roots[i];
            else if (roots[i].name == "Event System") EventSystem = roots[i];

    }
    

    static string catalogFolder = "E:/Games/UBuilt//addrass/2023.11.13.11.08.20.169/";
    static string bundleFolder;
    static CoreTame core;
    public class DirectoryOrder
    {
        public string[] order;
        public DirectoryOrder(string s)
        {
            s = s.Replace("//", "/");
            s = s.Replace("\\\\", "\\");
            order = s.Split(new char[] { '/', '\\' });
        }
        public string Pull(int n)
        {
            string s = "";
            for (int i = 0; i < order.Length - n; i++)
                s += order[i] + '/';
            return s;
        }
    }
   static bool catalogNotFound = false;
    public static void GetBundleFolder()
    {
        DirectoryOrder dor;
        if (CoreTame.ProjectID != "")
        {
            string dataPath = Application.dataPath;
            dor = new DirectoryOrder(dataPath);
            catalogFolder = dor.Pull(2) + "Projects/" + CoreTame.ProjectID + "/";
        }
        else if (CoreTame.ProjectPath != "")
        {
            catalogFolder = CoreTame.ProjectPath;
        }
        Debug.Log("catalog folder "+catalogFolder);
        try
        {
            string[] lines = File.ReadAllLines(catalogFolder + "catalog.json");
            int index0 = lines[0].IndexOf("m_InternalIds\"");
            int index1 = lines[0].IndexOf("\"", index0 + 15);
            int index2 = lines[0].IndexOf(".bundle", index0);

            string s = lines[0].Substring(index1 + 1, index2 - index1);
            dor = new DirectoryOrder(s);
            s = dor.order[dor.order.Length - 2];
            bundleFolder = catalogFolder + s + '/';
            Debug.Log("bundle folder = " + bundleFolder);
        }
        catch (Exception e)
        {
            catalogNotFound = true;
        }
    }
    public static void LoadScene(CoreTame ct)
    {
        core = ct;
        if (catalogNotFound) { CoreTame.loadStatus = CoreTame.LoadStatus.AddressableLoaded; return; }
        core.StartCoroutine(LoadAddressable());
    }
    static IEnumerator LoadAddressable()
    {
        yield return null;
        try
        {
            var cat = Addressables.LoadContentCatalogAsync(catalogFolder + "catalog.json");
            cat.Completed += Cat_Completed;
            Debug.Log(cat.Status);
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.Message);
            CoreTame.loadStatus = CoreTame.LoadStatus.AddressableLoaded;
        }
        Debug.Log("Loading scene started");

    }
    static UnityEngine.SceneManagement.Scene OriginalScene;
    static object SceneKey = null;
    static int ResultIndex;
    static private void Cat_Completed1(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.AddressableAssets.ResourceLocators.IResourceLocator> obj)
    {
        Debug.Log("cat completed");
        foreach (object o in obj.Result.Keys)
        {
            Debug.Log("object " + o.ToString());
            if (o.ToString().EndsWith(".unity")) SceneKey = o;
        }
        if (SceneKey != null)
            //   LoadDependency();
            LoadBundle();
    }
    static List<string> DepBundle = new List<string>();
    static string SceneBundle = "";
    static private void Cat_Completed(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.AddressableAssets.ResourceLocators.IResourceLocator> obj)
    {
        Debug.Log("cat completed");
        foreach (object o in obj.Result.Keys)
        {
            //   Debug.Log("object " + o.ToString());
            if (o.ToString().EndsWith(".bundle") && o.ToString().IndexOf("scene") > 0) SceneBundle = o.ToString();
            else if (o.ToString().EndsWith(".bundle"))
                DepBundle.Add(o.ToString());
        }
        //   LoadDependency();
        LoadBundle();
    }
    static void LoadBundle()
    {
        try
        {
            Debug.Log("loading bundle...");
            foreach (string s in DepBundle)
            {
                Debug.Log("loading bundle " + s);
                AssetBundle db = AssetBundle.LoadFromFile(bundleFolder + s);
                if (db != null) db.LoadAllAssets();
            }
            AssetBundle ab = AssetBundle.LoadFromFile(bundleFolder + SceneBundle);

            if (ab != null)
            {
                string[] path = ab.GetAllScenePaths();
                if (path != null)
                    if (path.Length > 0)
                    {
                        Debug.Log("asset name: " + path[0]);
                        SceneManager.LoadScene(path[0], LoadSceneMode.Additive);
                        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
                    }  //     ab.LoadAllAssets();
            }
        }
        catch (Exception ex)
        {
            CoreTame.loadStatus = CoreTame.LoadStatus.AddressableLoaded;
            Debug.Log("error " + ex.Message);
        }

    }

    private static void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        CompleteLoad(arg0);
    }

    static void LoadDependency()
    {
        try
        {
            AsyncOperationHandle<long> getDownloadSize = Addressables.GetDownloadSizeAsync(SceneKey);
            getDownloadSize.Completed += GetDownloadSize_Completed;
        }
        catch
        {
            CoreTame.loadStatus = CoreTame.LoadStatus.AddressableLoaded;
        }
        //If the download size is greater than 0, download all the dependencies.

    }

    private static void GetDownloadSize_Completed(AsyncOperationHandle<long> obj)
    {
        Debug.Log(obj.Result);
        if (obj.Result > 0)
            try
            {
                AsyncOperationHandle downloadDependencies = Addressables.DownloadDependenciesAsync(SceneKey);
                downloadDependencies.Completed += DownloadDependencies_Completed; ;
            }
            catch
            {
                CoreTame.loadStatus = CoreTame.LoadStatus.AddressableLoaded;
            }
        else
            LoadResult();
    }

    private static void DownloadDependencies_Completed(AsyncOperationHandle obj)
    {
        if (obj.Status == AsyncOperationStatus.Succeeded)
            LoadResult();
    }

    static void LoadResult()
    {
        try
        {
            var o = Addressables.LoadSceneAsync(SceneKey, LoadSceneMode.Additive, false);
            o.Completed += Load_Completed;
            //    CoreTame.loadStatus = CoreTame.LoadStatus.AddressableLoaded;
        }
        catch
        {
            CoreTame.loadStatus = CoreTame.LoadStatus.AddressableLoaded;
        }
    }

    private static void Load_Completed(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance> obj)
    {
        Debug.Log("precentage: " + obj.PercentComplete);
        //  CompleteLoad();
    }
    static void CompleteLoad(Scene s)
    {
        //   var s = UnityEngine.SceneManagement.SceneManager.GetSceneAt(1);
        Debug.Log("scene name 1 : " + s.name);
        GameObject[] gos = s.GetRootGameObjects();
        foreach (GameObject g in gos)
            if (g.name == "Rig" || g.name == "Event System")
            {
                Destroy(g);
                Debug.Log("go name : " + g.name);
            }
        SceneManager.MergeScenes(OriginalScene, s);
        //  UnityEngine.SceneManagement.SceneManager.(s);
        //RemoveDuplicateDefaults();
        CoreTame.loadStatus = CoreTame.LoadStatus.AddressableLoaded;
    }

}


