using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;

public class DetectApk : MonoBehaviour
{
    [System.Serializable]
    public class AppInfo
    {
        public string packageName;
        public string appName;
        public Sprite appIcon;
        public string apkFilePath;
    }

    [Header("UI References")]
    public Sprite defaultSprite;  // à assigner depuis l'inspecteur Unity

    public GameObject buttonPrefab;
    public Transform buttonsParent;
    public GameObject infoPanel;
    public GameObject panelApk;
    public TextMeshProUGUI infoText;
    public Button playButton;      // Bouton "Play" dans le panel info
    public Image infoIcon;         // Icône affichée dans le panel info

    [Header("Settings")]
    public bool skipSystemApps = true;

    private List<AppInfo> apps = new List<AppInfo>();
    private AppInfo selectedApp = null;

    void Start()
    {
        Debug.Log("[AppLauncher] Start called");

        // Panel info actif dès le début
        if (infoPanel != null)
        {
            infoPanel.SetActive(true);
            infoText.text = "Sélectionnez une app pour voir les détails.";
            if (infoIcon != null) 
            {
                infoIcon.sprite = defaultSprite;
                Debug.Log("[AppLauncher] Info icon set to default sprite");
            }
        }
        else
        {
            Debug.LogWarning("[AppLauncher] infoPanel n'est pas assigné !");
        }

        // Panel APK actif et parent des boutons
        if (panelApk != null)
        {
            panelApk.SetActive(true);
            buttonsParent = panelApk.transform;
            Debug.Log("[AppLauncher] panelApk is active and buttonsParent assigned");
        }
        else
        {
            Debug.LogWarning("[AppLauncher] panelApk n'est pas assigné !");
        }

        CheckStoragePermission();
    }

    void CheckStoragePermission()
    {
        Debug.Log("[AppLauncher] Checking storage permission");
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
        {
            Debug.Log("[AppLauncher] Storage permission not granted. Requesting...");
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

    void OnApplicationFocus(bool hasFocus)
    {
        Debug.Log("[AppLauncher] OnApplicationFocus: " + hasFocus);
        if (hasFocus)
        {
            RefreshApps();
        }
    }

    public void RefreshApps()
    {
        Debug.Log("[AppLauncher] RefreshApps called");
        apps.Clear();
        ClearButtons();

        List<AppInfo> installedApps = GetInstalledApps();
        List<AppInfo> apkFiles = GetApkFilesFromPrivateFolder();

        Debug.Log("[AppLauncher] Installed apps found: " + installedApps.Count);
        Debug.Log("[AppLauncher] APK files found in private folder: " + apkFiles.Count);

        apps.AddRange(installedApps);
        apps.AddRange(apkFiles);

        Debug.Log("[AppLauncher] Total apps to show: " + apps.Count);

        CreateButtons();

        Debug.Log("[AppLauncher] RefreshApps complete. Total buttons created: " + (buttonsParent != null ? buttonsParent.childCount : 0));
    }

    void ClearButtons()
    {
        if (buttonsParent == null)
        {
            Debug.LogWarning("[AppLauncher] ClearButtons: buttonsParent is null");
            return;
        }
        Debug.Log("[AppLauncher] Clearing buttons");
        for (int i = buttonsParent.childCount - 1; i >= 0; i--)
        {
            Destroy(buttonsParent.GetChild(i).gameObject);
        }
    }

    List<AppInfo> GetInstalledApps()
    {
        Debug.Log("[AppLauncher] GetInstalledApps called");
        List<AppInfo> list = new List<AppInfo>();

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                var pm = currentActivity.Call<AndroidJavaObject>("getPackageManager");
                var appsObj = pm.Call<AndroidJavaObject>("getInstalledApplications", 0);
                int size = appsObj.Call<int>("size");
                Debug.Log("[AppLauncher] Number of installed apps = " + size);

                for (int i = 0; i < size; i++)
                {
                    var appInfoObj = appsObj.Call<AndroidJavaObject>("get", i);
                    string packageName = appInfoObj.Get<string>("packageName");
                    int flags = appInfoObj.Get<int>("flags");
                    bool isSystem = (flags & 1) != 0;
                    if (skipSystemApps && isSystem) continue;

                    string appName = pm.Call<string>("getApplicationLabel", appInfoObj);

                    Debug.Log("[AppLauncher] Found app: " + appName + " (" + packageName + ") System: " + isSystem);

                    Sprite icon = null; // Optionnel via plugin natif
                    list.Add(new AppInfo { packageName = packageName, appName = appName, appIcon = icon });
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[AppLauncher] Exception in GetInstalledApps: " + e.Message);
        }
#else
        Debug.Log("[AppLauncher] GetInstalledApps skipped - not running on Android device");
#endif
        return list;
    }

    List<AppInfo> GetApkFilesFromPrivateFolder()
    {
        Debug.Log("[AppLauncher] GetApkFilesFromPrivateFolder called");
        List<AppInfo> list = new List<AppInfo>();
        string folder = Application.persistentDataPath;
        Debug.Log("[AppLauncher] Checking folder: " + folder);

        if (Directory.Exists(folder))
        {
            string[] files = Directory.GetFiles(folder, "*.apk");
            Debug.Log("[AppLauncher] APK files count in folder: " + files.Length);
            foreach (var f in files)
            {
                string name = Path.GetFileName(f);
                Debug.Log("[AppLauncher] Found APK file: " + name);
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
        if (buttonsParent == null)
        {
            Debug.LogWarning("[AppLauncher] CreateButtons: buttonsParent is null");
            return;
        }
        if (buttonPrefab == null)
        {
            Debug.LogError("[AppLauncher] CreateButtons: buttonPrefab is not assigned!");
            return;
        }

        Debug.Log("[AppLauncher] Creating buttons for apps count: " + apps.Count);
        foreach (var app in apps)
        {
            Debug.Log("[AppLauncher] Creating button for app: " + app.appName);

            // Instanciation du bouton dans panelApk
            GameObject btnObj = Instantiate(buttonPrefab, buttonsParent, false);

            // Nom sur le bouton
            Text t = btnObj.GetComponentInChildren<Text>();
            if (t != null)
            {
                t.text = app.appName;
                Debug.Log("[AppLauncher] Button text set to: " + app.appName);
            }
            else
            {
                Debug.LogWarning("[AppLauncher] Button child Text component not found");
            }

            // Icône sur le bouton (si existante)
            Image img = btnObj.GetComponentInChildren<Image>();
            if (img != null)
            {
                if (app.appIcon != null)
                {
                    img.sprite = app.appIcon;
                    Debug.Log("[AppLauncher] Button icon set");
                }
                else
                {
                    Debug.Log("[AppLauncher] No icon available for app: " + app.appName);
                }
            }
            else
            {
                Debug.LogWarning("[AppLauncher] Button child Image component not found");
            }

            // Clic pour afficher infos dans le panel info
            Button b = btnObj.GetComponent<Button>();
            if (b != null)
            {
                AppInfo copy = app;
                b.onClick.AddListener(() => OnAppButtonClicked(copy));
                Debug.Log("[AppLauncher] Listener added to button for app: " + app.appName);
            }
            else
            {
                Debug.LogWarning("[AppLauncher] Button component not found on prefab");
            }
        }
    }

    void OnAppButtonClicked(AppInfo app)
    {
        selectedApp = app;
        Debug.Log("[AppLauncher] OnAppButtonClicked: " + app.appName);

        string info = $"Name: {app.appName}\nPackage: {app.packageName ?? "N/A"}\nAPK Path: {app.apkFilePath ?? "N/A"}";
        infoText.text = info;

        if (infoIcon != null)
        {
            if (app.appIcon != null)
            {
                infoIcon.sprite = app.appIcon;
                Debug.Log("[AppLauncher] infoIcon set to app icon");
            }
            else
            {
                infoIcon.sprite = defaultSprite;
                Debug.Log("[AppLauncher] infoIcon set to default sprite");
            }
        }
        else
        {
            Debug.LogWarning("[AppLauncher] infoIcon is not assigned");
        }
    }
}
