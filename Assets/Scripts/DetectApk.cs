using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Android;

public class DetectApkLauncher : MonoBehaviour
{
    [System.Serializable]
    public class AppInfo
    {
        public string packageName;
        public string appName;
        [System.NonSerialized] public Sprite appIcon;
        public string apkFilePath;
        public string description;
        public string previewImage;
    }

    [System.Serializable]
    public class AppDataEntry
    {
        public string packageName;
        public string description;
        public string previewImage;
    }

    [System.Serializable]
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
    public Image previewImageUI; // image de preview du JSON


    [Header("Settings")]
    public bool skipSystemApps = true;
    
    
   
    private List<AppInfo> apps = new List<AppInfo>();
    private List<AppDataEntry> appDataEntries = new List<AppDataEntry>();
    private AppInfo selectedApp = null;
    private bool appJustLaunched = false;

    void Start()
    {
        Debug.Log("[Launcher] Start appelée");

        // Copier JSON + images depuis StreamingAssets
        CopyStreamingAssetsToPersistent();

        // Charger JSON
        LoadAppDataJson();

        // Vérifier permission stockage et détecter APK
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
        Debug.Log("[Launcher] StreamingAssets copiés dans persistentDataPath");
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    IEnumerator CopyFolderAndroid(string sourceFolder, string targetFolder)
    {
        if (!Directory.Exists(targetFolder))
            Directory.CreateDirectory(targetFolder);

        string[] files = { "appdata.json", "demoa_preview.png", "demob_preview.png","mylauncher_preview.png"}; // liste des fichiers à copier
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
                    {
                        File.WriteAllBytes(dstPath, www.downloadHandler.data);
                        Debug.Log($"[Launcher] Copié {fileName} vers persistentDataPath");
                    }
                    else
                        Debug.LogWarning($"[Launcher] Impossible de copier {fileName}: {www.error}");
                }
            }
        }
    }
#endif
    #endregion

    #region JSON Loading
    void LoadAppDataJson()
    {
        string jsonPath = Path.Combine(Application.persistentDataPath, "appdata.json");
        if (File.Exists(jsonPath))
        {
            string jsonText = File.ReadAllText(jsonPath);
            appDataEntries = new List<AppDataEntry>(JsonUtility.FromJson<AppDataWrapper>("{\"entries\":" + jsonText + "}").entries);
            Debug.Log($"[Launcher] JSON chargé : {appDataEntries.Count} entrées");
        }
        else
        {
            Debug.LogWarning("[Launcher] JSON appdata.json introuvable !");
        }
    }
    #endregion

    #region Permissions
    void CheckStoragePermission()
    {
        Debug.Log("[Launcher] Vérification permission stockage");
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
        {
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += (perm) =>
            {
                Debug.Log("[Launcher] Permission stockage accordée");
                RefreshApps();
            };
            callbacks.PermissionDenied += (perm) =>
            {
                Debug.LogWarning("[Launcher] Permission stockage refusée");
                RefreshApps();
            };
            Permission.RequestUserPermission(Permission.ExternalStorageRead, callbacks);
        }
        else
        {
            Debug.Log("[Launcher] Permission stockage déjà accordée");
            RefreshApps();
        }
    }
    #endregion

    #region APK Detection
    public void RefreshApps()
    {
        Debug.Log("[Launcher] RefreshApps appelée");
        apps.Clear();
        ClearButtons();

        apps.AddRange(GetInstalledApps());
        apps.AddRange(GetApkFilesFromPrivateFolder());

        // Associer JSON + image preview (sans écraser l'icône réelle)
        foreach (var app in apps)
        {
            var data = appDataEntries.Find(e => e.packageName == app.packageName || e.packageName == app.appName);
            if (data != null)
            {
                app.description = data.description;
                app.previewImage = data.previewImage;

                if (!string.IsNullOrEmpty(data.previewImage))
                {
                    string imgPath = Path.Combine(Application.persistentDataPath, data.previewImage);
                    if (File.Exists(imgPath))
                    {
                        Debug.Log($"[Launcher] Preview trouvée pour {app.appName}: {imgPath}");
                    }
                    else
                    {
                        Debug.LogWarning($"[Launcher] Preview introuvable pour {app.appName}: {imgPath}");
                    }
                }
            }
        }

        Debug.Log($"[Launcher] {apps.Count} apps détectées au total.");
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
                    !packageName.ToLower().Contains("unitytechnologies"))    // filtre sur packageName à changer ici
                         continue;                                         


                    string appName = pm.Call<string>("getApplicationLabel", appInfoObj);
                    Sprite iconSprite = GetAppIconSprite(packageName);

                    list.Add(new AppInfo
                    {
                        packageName = packageName,
                        appName = appName,
                        appIcon = iconSprite  ? iconSprite : defaultIcon,
                        apkFilePath = null
                    });

                    Debug.Log($"[GetInstalledApps] App ajoutée: {appName} ({packageName})");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Launcher] Erreur GetInstalledApps : " + e);
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
                if (!name.ToLower().Contains("leclick") && !name.ToLower().Contains("unitytechnologies")) continue;  // deuxieme filtre à modifier ici

                list.Add(new AppInfo
                {
                    packageName = null,
                    appName = name,
                    apkFilePath = f,
                    appIcon = null
                });

                Debug.Log($"[GetApkFiles] APK trouvée: {name} ({f})");
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
        infoText.text = $"Nom: {app.appName}\nPackage: {app.packageName ?? "N/A"}\n\nDescription:\n{app.description ?? "Aucune"}";

        infoIcon.sprite = app.appIcon ?? defaultIcon;
       
            Debug.Log($"[OnAppSelected] Sélection : {app.appName} | Package: {app.packageName}");
            Debug.Log($"[OnAppSelected] Description = {(app.description ?? "null")}, Preview = {(app.previewImage ?? "null")}");
            Debug.Log($"[OnAppSelected] infoText={(infoText != null)}, infoIcon={(infoIcon != null)}, previewImageUI={(previewImageUI != null)}");


        Debug.Log($"[OnAppSelected] {app.appName} sélectionnée | description={app.description ?? "null"} | image={app.previewImage ?? "null"}");
        if (!string.IsNullOrEmpty(app.previewImage))
        {
            string imgPath = Path.Combine(Application.persistentDataPath, app.previewImage);
            if (File.Exists(imgPath))
            {
                byte[] bytes = File.ReadAllBytes(imgPath);
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(bytes))
                    previewImageUI.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
            else
            {
                Debug.LogWarning($"[OnAppSelected] Preview non trouvée : {imgPath}");
                previewImageUI.sprite = defaultIcon;
            }
        }
        else
        {
            previewImageUI.sprite = defaultIcon; // placeholder si pas d'image
        }
    }

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
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(iconBytes))
                        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[GetAppIconSprite] Exception: " + e);
        }
#endif
        return null;
    }

    void OnPlayButtonClicked()
    {
        if (selectedApp == null)
        {
            Debug.LogWarning("[LauncherPlay] Aucune app sélectionnée !");
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            try
            {
                if (!string.IsNullOrEmpty(selectedApp.packageName))
                {
                    var pm = currentActivity.Call<AndroidJavaObject>("getPackageManager");
                    var intent = pm.Call<AndroidJavaObject>("getLaunchIntentForPackage", selectedApp.packageName);
                    if (intent != null)
                        currentActivity.Call("startActivity", intent);
                }
                else if (!string.IsNullOrEmpty(selectedApp.apkFilePath))
                {
                    var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.VIEW");
                    var uriClass = new AndroidJavaClass("android.net.Uri");
                    var fileObj = new AndroidJavaObject("java.io.File", selectedApp.apkFilePath);
                    var uri = uriClass.CallStatic<AndroidJavaObject>("fromFile", fileObj);
                    intent.Call<AndroidJavaObject>("setDataAndType", uri, "application/vnd.android.package-archive");
                    intent.Call<AndroidJavaObject>("addFlags", 0x10000000);
                    currentActivity.Call("startActivity", intent);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[LauncherPlay] Erreur lancement : " + e);
            }
        }
#else
        Debug.Log($"[LauncherPlay] Simulation lancement : {selectedApp.appName}");
#endif
    }
    #endregion
}
