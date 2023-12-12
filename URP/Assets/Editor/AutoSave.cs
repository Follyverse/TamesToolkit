using System;
using System.IO;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class AutoSaveScene
{
    private const string SAVE_FOLDER = "Editor/AutoSaves";

    private static System.DateTime lastSaveTime = System.DateTime.Now;
    private static System.TimeSpan updateInterval;
    private static int Turn = 0;
    static AutoSaveScene()
    {
        EnsureAutoSavePathExists();

        // Register for autosaves.
        // Change this number to modify the autosave interval.
        RegisterOnEditorUpdate(7);
    }

    public static void RegisterOnEditorUpdate(int interval)
    {
        Debug.Log("Enabling AutoSave");

        updateInterval = new TimeSpan(0, interval, 0);
        EditorApplication.update += OnUpdate;
    }

    /// 
    /// Makes sure the target save path exists.
    /// 
    private static void EnsureAutoSavePathExists()
    {
        var path = Path.Combine(Application.dataPath, SAVE_FOLDER);

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    /// 
    /// Saves a copy of the currently open scene.
    /// 
    private static void SaveScene()
    {
        Debug.Log("Auto saving scene: " + EditorSceneManager.GetActiveScene());

        EnsureAutoSavePathExists();

        // Get the new saved scene name.
        if (Markers.MarkerSettings.AutoSaveMode != Markers.AutoSaveType.None)
        {
            var newName = GetNewSceneName(EditorSceneManager.GetActiveScene().name);
            var folder = Path.Combine("Assets", SAVE_FOLDER);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), Path.Combine(folder, newName), true);
            AssetDatabase.SaveAssets();
            Debug.Log("saved " + newName + " " + folder);
        }
        else Debug.Log("save none");
    }

    /// 
    /// Helper method that creates a new scene name.
    /// 
    private static string GetNewSceneName(string originalSceneName)
    {
        var scene = Path.GetFileNameWithoutExtension(originalSceneName);
        if (Markers.MarkerSettings.AutoSaveMode == Markers.AutoSaveType.MultipleFiles)  // Turn = (Turn + 1) % 4 + 1;
            return string.Format(
               "{0}_{1}.unity",
               scene,
               System.DateTime.Now.ToString(
               "yyyy-MM-dd_HH-mm-ss",
               CultureInfo.InvariantCulture));
        else
            return scene + "-auto.unity";
    }

    private static void OnUpdate()
    {
        if ((System.DateTime.Now - lastSaveTime) >= updateInterval)
        {
            SaveScene();
            lastSaveTime = System.DateTime.Now;
        }
    }
}
