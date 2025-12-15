using UnityEngine;
using UnityEditor;

public class FindMissingScripts : EditorWindow
{
    [MenuItem("Tools/Find Missing Scripts")]
    public static void FindMissing()
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        int count = 0;

        foreach (GameObject go in allObjects)
        {
            Component[] components = go.GetComponents<Component>();

            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    count++;
                    Debug.LogWarning($"Missing Script: {GetFullPath(go)}", go);
                }
            }
        }

        if (count == 0)
            Debug.Log("No missing scripts found!");
        else
            Debug.LogWarning($"Found {count} missing script(s)!");
    }

    [MenuItem("Tools/Remove All Missing Scripts")]
    public static void RemoveAllMissing()
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        int count = 0;

        foreach (GameObject go in allObjects)
        {
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (removed > 0)
            {
                count += removed;
                EditorUtility.SetDirty(go);
                Debug.Log($"Removed {removed} missing script(s) from: {GetFullPath(go)}", go);
            }
        }

        if (count == 0)
            Debug.Log("No missing scripts to remove!");
        else
        {
            Debug.LogWarning($"Removed {count} missing script(s) total!");
            AssetDatabase.SaveAssets();
        }
    }

    private static string GetFullPath(GameObject go)
    {
        string path = go.name;
        Transform parent = go.transform.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }
}
