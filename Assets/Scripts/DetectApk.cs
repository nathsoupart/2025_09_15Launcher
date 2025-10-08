using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Android;

public class DetectApk : MonoBehaviour
{
    [System.Serializable]
    public class AppInfo
    {
        public string packageName;
        public string appName;
        [System.NonSerialized] public Sprite appIcon;
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

    private void Start()
    {
        CheckStoragePermission();

        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButtonClicked);
    }

    private void CheckStoragePermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
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
#else
        RefreshApps();
#endif
    }

    public void RefreshApps()
    {
        apps.Clear();
        ClearButtons();

        apps.AddRange(GetInstalledApps());
        apps.AddRange(GetApkFilesFromPrivateFolder());

        Debug.Log($"[AppLauncherPlay] {apps.Count} apps trouvées.");
        CreateButtons();
    }

    private void ClearButtons()
    {
        foreach (Transform child in buttonsParent)
            Destroy(child.gameObject);
    }

    private List<AppInfo> GetInstalledApps()
    {
        List<AppInfo> list = new List<AppInfo>();

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                var pm = currentActivity.Call<AndroidJavaObject>("getPackageManager");
                var appsList = pm.Call<AndroidJavaObject>("getInstalledApplications", 0);
                int size = appsList.Call<int>("size");

                for (int i = 0; i < size; i++)
                {
                    var appInfoObj = appsList.Call<AndroidJavaObject>("get", i);
                    string packageName = appInfoObj.Get<string>("packageName");
                    int flags = appInfoObj.Get<int>("flags");
                    bool isSystem = (flags & 1) != 0;
                    if (skipSystemApps && isSystem) continue;

                    string appName = pm.Call<string>("getApplicationLabel", appInfoObj);
                    Sprite iconSprite = GetAppIconSprite(packageName);

                    list.Add(new AppInfo { packageName = packageName, appName = appName, appIcon = iconSprite });
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[AppLauncherPlay] Erreur GetInstalledApps : " + e.Message);
        }
#endif
        return list;
    }

    private List<AppInfo> GetApkFilesFromPrivateFolder()
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
        return list;
    }

    private void CreateButtons()
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
                btnComp.onClick.AddListener(() => OnAppSelected(appRef));
            }
        }
    }

    private Sprite GetAppIconSprite(string packageName)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var jc = new AndroidJavaClass("com.yourcompany.iconfetcher.IconFetcher"))
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
        catch { }
#endif
        return null;
    }

    private void OnAppSelected(AppInfo app)
    {
        selectedApp = app;
        infoText.text = $"Nom: {app.appName}\nPackage: {app.packageName ?? "N/A"}\nAPK Path: {app.apkFilePath ?? "N/A"}";
        infoIcon.sprite = app.appIcon ?? defaultSprite;
    }

    private void OnPlayButtonClicked()
    {
        if (selectedApp == null)
        {
            Debug.LogWarning("[AppLauncherPlay] Aucun app sélectionnée !");
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
                    else
                        Debug.LogWarning("[AppLauncherPlay] Impossible de trouver l’intent pour " + selectedApp.packageName);
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
                Debug.LogError("[AppLauncherPlay] Erreur lancement : " + e.Message);
            }
        }
#else
        Debug.Log($"[AppLauncherPlay] Simulation lancement : {selectedApp.appName}");
#endif
    }
}
