using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Android;

public class DetectApkLauncher : MonoBehaviour
{
    [Serializable]
    public class Partner
    {
        public string name;
       
    }

    
   

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
    public class AppDataEntry
    {
        public string packageName;
        public string appName;
        public string description;
        public string previewImage;
        public string organization;
        public List<Partner> partners;
        public string financialLogo;
    }

    [Serializable]
    private class AppDataWrapper
    {
        public AppDataEntry[] entries;
    }

    [Header("UI References")]
    public Sprite defaultIcon;
    public GameObject buttonPrefab;
    public Transform buttonsParent;
    public TextMeshProUGUI infoText;
    public Button playButton;
    public Image infoIcon;       // icône de l’appli
    public Image previewImageUI; // image de preview
    public Image infoOrglogo;    // logo de l’organisation
    public Image infoFinancialLogo; // Image UI pour le logo financier

    [Header("Settings")]
    public bool skipSystemApps = true;

    private List<AppInfo> apps = new List<AppInfo>();
    private List<AppDataEntry> appDataEntries = new List<AppDataEntry>();
    private AppInfo selectedApp = null;

    void Start()
    {
       
        CopyStreamingAssetsToPersistent();
        LoadAppDataJson();
        CheckStoragePermission();

        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButtonClicked);
    }

    #region StreamingAssets Copy
    void CopyStreamingAssetsToPersistent()
    {
        string sourceFolder = Application.streamingAssetsPath;
        string targetFolder = Application.persistentDataPath;

#if UNITY_ANDROID && !UNITY_EDITOR
        StartCoroutine(CopyFolderAndroid(sourceFolder, targetFolder));
#else
        CopyFolderStandalone(sourceFolder, targetFolder);
#endif
    }

    void CopyFolderStandalone(string sourceFolder, string targetFolder)
    {
        if (!Directory.Exists(sourceFolder)) return;

        foreach (string filePath in Directory.GetFiles(sourceFolder))
        {
            string fileName = Path.GetFileName(filePath);
            string destPath = Path.Combine(targetFolder, fileName);
            if (!File.Exists(destPath))
                File.Copy(filePath, destPath, true);
        }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    IEnumerator CopyFolderAndroid(string sourceFolder, string targetFolder)
    {
        if (!Directory.Exists(targetFolder))
            Directory.CreateDirectory(targetFolder);

        string[] files = { "appdata.json", "demoa_preview.png", "demob_preview.png", "mylauncher_preview.png", "leclick_logo.png",  "idea_logo.png", "umons_logo.png" ,"logofinancial_demoa.png", "interreg_logo.png"};
        foreach (string fileName in files)
        {
            string srcPath = Path.Combine(sourceFolder, fileName);
            string dstPath = Path.Combine(targetFolder, fileName);

            if (!File.Exists(dstPath))
            {
                using (var www = new UnityEngine.Networking.UnityWebRequest(srcPath))
                {
                    www.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
                    yield return www.SendWebRequest();

                    if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                        File.WriteAllBytes(dstPath, www.downloadHandler.data);
                }
            }
        }
    }
#endif
    #endregion

    #region JSON
    void LoadAppDataJson()
    {
        string jsonPath = Path.Combine(Application.persistentDataPath, "appdata.json");
        if (File.Exists(jsonPath))
        {
            string jsonText = File.ReadAllText(jsonPath);
            appDataEntries = new List<AppDataEntry>(
                JsonUtility.FromJson<AppDataWrapper>("{\"entries\":" + jsonText + "}").entries
            );
        }
        else
        {
            Debug.LogWarning("JSON appdata.json introuvable !");
        }
    }
    #endregion

    #region Permissions
    void CheckStoragePermission()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
        {
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += (perm) => RefreshApps();
            callbacks.PermissionDenied += (perm) => RefreshApps();
            Permission.RequestUserPermission(Permission.ExternalStorageRead, callbacks);
        }
        else
        {
            RefreshApps();
        }
    }
    #endregion

    #region APK Detection
    public void RefreshApps()
    {
        apps.Clear();
        ClearButtons();

        List<AppInfo> installedApps = GetInstalledApps();
        List<AppInfo> apkFiles = GetApkFilesFromPrivateFolder();
        apps.AddRange(installedApps);
        apps.AddRange(apkFiles);

        foreach (var app in apps)
        {
            var data = appDataEntries.Find(e => e.packageName == app.packageName || e.appName == app.appName);
            if (data != null)
            {
                app.description = data.description;
                app.previewImage = data.previewImage;
                app.organization = data.organization;
                app.partners = data.partners;
                app.financialLogo = data.financialLogo;
            }
        }

        CreateButtons();
    }

    void ClearButtons()
    {
        foreach (Transform child in buttonsParent)
            Destroy(child.gameObject);
    }

    List<AppInfo> GetInstalledApps()
    {
        List<AppInfo> list = new List<AppInfo>();
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                var pm = currentActivity.Call<AndroidJavaObject>("getPackageManager");
                var appsList = pm.Call<AndroidJavaObject>("getInstalledApplications", 0);
                int size = appsList.Call<int>("size");

                for (int i = 0; i < size; i++)
                {
                    var appInfoObj = appsList.Call<AndroidJavaObject>("get", i);
                    string packageName = appInfoObj.Get<string>("packageName");

                    if (string.IsNullOrEmpty(packageName)) continue;
                    if (!packageName.ToLower().Contains("leclick") &&
                        !packageName.ToLower().Contains("unitytechnologies"))
                        continue;

                    string appName = pm.Call<string>("getApplicationLabel", appInfoObj);
                    Sprite iconSprite = GetAppIconSprite(packageName);

                    list.Add(new AppInfo
                    {
                        packageName = packageName,
                        appName = appName,
                        appIcon = iconSprite ?? defaultIcon,
                        apkFilePath = null

                    });
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Erreur GetInstalledApps : " + e.Message);
        }
#endif
        return list;
    }

    List<AppInfo> GetApkFilesFromPrivateFolder()
    {
        List<AppInfo> list = new List<AppInfo>();
        string folder = Application.persistentDataPath;

        if (Directory.Exists(folder))
        {
            string[] files = Directory.GetFiles(folder, "*.apk");
            foreach (var f in files)
            {
                string name = Path.GetFileNameWithoutExtension(f);
                if (!name.ToLower().Contains("leclick") &&
                    !name.ToLower().Contains("unitytechnologies"))
                    continue;

                list.Add(new AppInfo
                {
                    packageName = null,
                    appName = name,
                    apkFilePath = f,
                    appIcon = null
                });
            }
        }
        return list;
    }
    #endregion

    #region UI
    void CreateButtons()
    {
        foreach (var app in apps)
        {
            GameObject btnObj = Instantiate(buttonPrefab, buttonsParent);
            var textMesh = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (textMesh != null) textMesh.text = app.appName;

            var iconTransform = btnObj.transform.Find("Icon");
            if (iconTransform != null)
            {
                var img = iconTransform.GetComponent<Image>();
                if (img != null) img.sprite = app.appIcon ?? defaultIcon;
            }

            Button btnComp = btnObj.GetComponent<Button>();
            if (btnComp != null)
            {
                AppInfo appRef = app;
                btnComp.onClick.AddListener(() => OnAppSelected(appRef));
            }
        }
    }

    void OnAppSelected(AppInfo app)
    {
        selectedApp = app;

        string partnersList = "Aucun";
        if (app.partners != null && app.partners.Count > 0)
            partnersList = string.Join("\n• ", app.partners.ConvertAll(p => p.name));

        infoText.text =
            $"<b>Nom:</b> {app.appName}\n" +
            $"<b>Package:</b> {app.packageName ?? "N/A"}\n\n" +
            $"<b>Description:</b>\n{app.description ?? "Aucune"}\n\n" +
            $"<b>Partenaires:</b>\n• {partnersList}";
           
        infoIcon.sprite = app.appIcon ?? defaultIcon;
        LoadSpriteFromPersistent(app.organization, infoOrglogo);
        LoadSpriteFromPersistent(app.previewImage, previewImageUI);
        LoadSpriteFromPersistent(app.financialLogo, infoFinancialLogo);
    }

    private void LoadSpriteFromPersistent(string fileName, Image targetImage)
    {
        if (string.IsNullOrEmpty(fileName) || targetImage == null)
        {
            if (targetImage != null) targetImage.sprite = defaultIcon;
            return;
        }

        string path = Path.Combine(Application.persistentDataPath, fileName);
        if (File.Exists(path))
        {
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
            targetImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
        else
        {
            targetImage.sprite = defaultIcon;
        }
    }
    #endregion

    #region PlayButton
    public void OnPlayButtonClicked()
    {
        if (selectedApp == null)
        {
            Debug.LogWarning("Aucune app sélectionnée !");
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                if (!string.IsNullOrEmpty(selectedApp.packageName))
                {
                    AndroidJavaObject pm = currentActivity.Call<AndroidJavaObject>("getPackageManager");
                    AndroidJavaObject launchIntent = pm.Call<AndroidJavaObject>("getLaunchIntentForPackage", selectedApp.packageName);

                    if (launchIntent != null)
                        currentActivity.Call("startActivity", launchIntent);
                }
                else if (!string.IsNullOrEmpty(selectedApp.apkFilePath))
                {
                    AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.VIEW");
                    AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri");
                    AndroidJavaObject uri = uriClass.CallStatic<AndroidJavaObject>("parse", "file://" + selectedApp.apkFilePath);

                    intent.Call<AndroidJavaObject>("setDataAndType", uri, "application/vnd.android.package-archive");
                    intent.Call<AndroidJavaObject>("addFlags", 0x10000000);
                    currentActivity.Call("startActivity", intent);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Erreur lancement : " + e.Message);
        }
#else
        Debug.Log("Simulation lancement : " + selectedApp.appName);
#endif
    }
    #endregion

    Sprite GetAppIconSprite(string packageName)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var jc = new AndroidJavaClass("be.leclick.iconfetcher.IconFetcher"))
            {
                var context = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
                byte[] iconBytes = jc.CallStatic<byte[]>("getAppIcon", packageName, context);
                if (iconBytes != null && iconBytes.Length > 0)
                {
                    Texture2D tex = new Texture2D(2, 2);
                    tex.LoadImage(iconBytes);
                    return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }
        }
        catch { }
#endif
        return null;
    }

    public void ClearPersistentFiles()
    {
        string path = Application.persistentDataPath;
        if (!Directory.Exists(path)) return;

        foreach (var file in Directory.GetFiles(path))
        {
            try { File.Delete(file); } catch { }
        }
    }
}
