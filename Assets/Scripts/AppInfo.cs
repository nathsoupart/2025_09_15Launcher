using System;
using UnityEngine;
using System.Collections.Generic;
[Serializable]
public class AppInfo
{
    public string packageName;
    public string appName;
    [NonSerialized] public Sprite appIcon;
    public string apkFilePath;
    public string description;
    public string previewImage;
    public string organization;
    public List<Partner> partners;
    public string financialLogo; // Image UI pour logofinancial
}
[Serializable]
public class Partner
{
    public string name;
}

[Serializable]
public class Organization
{
    public string name;
}