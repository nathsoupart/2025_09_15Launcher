using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Android;
using System.IO;

public class DetectApk : MonoBehaviour
{
    [System.Serializable]
    public class AppInfo
    {
        public string packageName;
        public string appName;
        [System.NonSerialized]
        public Sprite appIcon;
        public string apkFilePath;
    }

    [Header("UI References")]
    public Sprite defaultSprite;  
    public GameObject buttonPrefab;
    public Transform buttonsParent; 
    public TextMeshProUGUI infoText;
    public Button playButton;
    public Image infoIcon;

    [Header("Settings")]
    public bool skipSystemApps = true;

    private List<AppInfo> apps = new List<AppInfo>();
    private AppInfo selectedApp = null;

    void Start()
    {
        CheckStoragePermission();
    }

    void CheckStoragePermission()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
        {
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += (perm) =>
            {
                Debug.Log("[AppLauncher] Storage permission granted: " + perm);
                RefreshApps();
            };
            callbacks.PermissionDenied += (perm) =>
            {
                Debug.LogWarning("[AppLauncher] Storage permission denied: " + perm);
                RefreshApps();
            };
            Permission.RequestUserPermission(Permission.ExternalStorageRead, callbacks);
        }
        else
        {
            Debug.Log("[AppLauncher] Storage permission already granted");
            RefreshApps();
        }
    }

    public void RefreshApps()
    {
        apps.Clear();
        ClearButtons();

        List<AppInfo> installedApps = GetInstalledApps();
        List<AppInfo> apkFiles = GetApkFilesFromPrivateFolder();

        apps.AddRange(installedApps);
        apps.AddRange(apkFiles);

        Debug.Log("[AppLauncher] Total apps found: " + apps.Count);
        foreach (var app in apps)
            Debug.Log("[AppLauncher] App: " + app.appName + " (" + app.packageName + ")");

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
                AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject pm = currentActivity.Call<AndroidJavaObject>("getPackageManager");
                AndroidJavaObject apps = pm.Call<AndroidJavaObject>("getInstalledApplications", 0);
                int size = apps.Call<int>("size");
                for (int i = 0; i < size; i++)
                {
                    AndroidJavaObject appInfoObj = apps.Call<AndroidJavaObject>("get", i);
                    string packageName = appInfoObj.Get<string>("packageName");
                    int flags = appInfoObj.Get<int>("flags");
                    bool isSystem = (flags & 1) != 0;
                    if (skipSystemApps && isSystem)
                    {
                        Debug.Log("[AppLauncher] Ignoring system app: " + packageName);
                        continue;
                    }

                    string appName = pm.Call<string>("getApplicationLabel", appInfoObj);
                    Sprite iconSprite = GetAppIconSprite(packageName);

                    list.Add(new AppInfo { packageName = packageName, appName = appName, appIcon = iconSprite });
                    Debug.Log("[AppLauncher] Found app: " + appName + " (" + packageName + ") System: " + isSystem);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[AppLauncher] Exception in GetInstalledApps: " + e.Message);
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
                string name = Path.GetFileName(f);
                list.Add(new AppInfo { packageName = null, appName = name, appIcon = null, apkFilePath = f });
            }
        }
        else
        {
            Debug.LogWarning("[AppLauncher] Folder does not exist: " + folder);
        }
        return list;
    }

    void CreateButtons()
    {
        foreach (var app in apps)
        {
            GameObject btnObj = Instantiate(buttonPrefab, buttonsParent);
            var textMesh = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (textMesh != null)
                textMesh.text = app.appName;
            else
                Debug.LogWarning("[AppLauncher] TMP Text component not found on button prefab");

            var iconTransform = btnObj.transform.Find("Icon");
            if (iconTransform != null)
            {
                var img = iconTransform.GetComponent<Image>();
                if (img != null)
                    img.sprite = app.appIcon != null ? app.appIcon : defaultSprite;
                else
                    Debug.LogWarning("[AppLauncher] 'Icon' GameObject found but no Image component attached");
            }
            else
            {
                Debug.LogWarning("[AppLauncher] 'Icon' GameObject not found under button");
            }

            Button btnComp = btnObj.GetComponent<Button>();
            if (btnComp != null)
            {
                AppInfo appRef = app;
                btnComp.onClick.AddListener(() => OnAppButtonClicked(appRef));
            }
            else
                Debug.LogWarning("[AppLauncher] Button component not found on button prefab");
        }
    }

    Sprite GetAppIconSprite(string packageName)
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var jc = new AndroidJavaClass("com.yourcompany.iconfetcher.IconFetcher"))
            {
                using (var context = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    byte[] iconBytes = jc.CallStatic<byte[]>("getAppIcon", packageName, context);
                    if (iconBytes != null && iconBytes.Length > 0)
                    {
                        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        tex.LoadImage(iconBytes);
                        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[AppLauncher] Failed to get icon for " + packageName + " : " + e.Message);
        }
        #endif
        return null;
    }

    void OnAppButtonClicked(AppInfo app)
    {
        selectedApp = app;
        if (infoText != null)
            infoText.text = $"Nom: {app.appName}\nPackage: {app.packageName ?? "N/A"}\nAPK Path: {app.apkFilePath ?? "N/A"}";

        if (infoIcon != null)
            infoIcon.sprite = app.appIcon != null ? app.appIcon : defaultSprite;
    }
}
