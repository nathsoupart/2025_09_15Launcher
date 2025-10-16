using System;
using UnityEngine;

[Serializable]
public class AppInfo
{
    public string packageName;
    public string appName;
    [NonSerialized] public Sprite appIcon;
    public string apkFilePath;
    public string description;
    public string previewImage; // chemin relatif vers l’image dans persistentDataPath
}