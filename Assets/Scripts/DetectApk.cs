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
    private bool appJustLaunched = false;

    void Start()
    {
        Debug.Log("[AppLauncher] Start appelée");
        CheckStoragePermission();

        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButtonClicked);
    }

    void CheckStoragePermission()
    {
        Debug.Log("[AppLauncher] Vérification permission stockage");
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
        {
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += (perm) => 
            {
                Debug.Log("[AppLauncher] Permission stockage accordée");
                RefreshApps();
            };
            callbacks.PermissionDenied += (perm) => 
            {
                Debug.LogWarning("[AppLauncher] Permission stockage refusée");
                RefreshApps();
            };
            Permission.RequestUserPermission(Permission.ExternalStorageRead, callbacks);
        }
        else
        {
            Debug.Log("[AppLauncher] Permission stockage déjà accordée");
            RefreshApps();
        }
    }

    public void RefreshApps()
    {
        Debug.Log("[AppLauncher] RefreshApps appelée");
        apps.Clear();
        ClearButtons();

        apps.AddRange(GetInstalledApps());
        apps.AddRange(GetApkFilesFromPrivateFolder());

        Debug.Log($"[AppLauncher] {apps.Count} apps trouvées.");
        CreateButtons();
    }

    void ClearButtons()
    {
        foreach (Transform child in buttonsParent)
            Destroy(child.gameObject);
    }

    List<AppInfo> GetInstalledApps()
    {
        Debug.Log("[AppLauncher] Récupération apps installées");
        List<AppInfo> list = new List<AppInfo>();
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject pm = currentActivity.Call<AndroidJavaObject>("getPackageManager");
                AndroidJavaObject appsList = pm.Call<AndroidJavaObject>("getInstalledApplications", 0);
                int size = appsList.Call<int>("size");
                Debug.Log($"[AppLauncher] {size} apps installées détectées");

                for (int i = 0; i < size; i++)
                {
                    AndroidJavaObject appInfoObj = appsList.Call<AndroidJavaObject>("get", i);
                    string packageName = appInfoObj.Get<string>("packageName");
                    int flags = appInfoObj.Get<int>("flags");
                    bool isSystem = (flags & 1) != 0;
                    if (skipSystemApps && isSystem) continue;

                    string appName = pm.Call<string>("getApplicationLabel", appInfoObj);
                    Sprite iconSprite = GetAppIconSprite(packageName);

                    Debug.Log($"[AppLauncher] App trouvée: {appName} [{packageName}], icône {(iconSprite != null ? "OK" : "NULL")}");
                    list.Add(new AppInfo { packageName = packageName, appName = appName, appIcon = iconSprite });
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[AppLauncher] Erreur GetInstalledApps : " + e.Message);
        }
#endif
        return list;
    }

    List<AppInfo> GetApkFilesFromPrivateFolder()
    {
        Debug.Log("[AppLauncher] Recherche APK dans dossier privé");
        List<AppInfo> list = new List<AppInfo>();
        string folder = Application.persistentDataPath;

        if (Directory.Exists(folder))
        {
            string[] files = Directory.GetFiles(folder, "*.apk");
            foreach (var f in files)
            {
                string name = Path.GetFileName(f);
                Debug.Log($"[AppLauncher] APK trouvé: {name}");
                list.Add(new AppInfo { packageName = null, appName = name, appIcon = null, apkFilePath = f });
            }
        }
        else
        {
            Debug.LogWarning("[AppLauncher] Dossier privé apk non trouvé");
        }
        return list;
    }

    void CreateButtons()
    {
        Debug.Log("[AppLauncher] Création boutons UI");
        foreach (var app in apps)
        {
            GameObject btnObj = Instantiate(buttonPrefab, buttonsParent);
            var textMesh = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (textMesh != null)
                textMesh.text = app.appName;

            var iconTransform = btnObj.transform.Find("Icon");
            if (iconTransform != null)
            {
                var img = iconTransform.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = app.appIcon ?? defaultSprite;
                    Debug.Log($"[Button] appIcon pour {app.appName}: {(app.appIcon != null ? "OK" : "NULL")}");
                }
            }

            Button btnComp = btnObj.GetComponent<Button>();
            if (btnComp != null)
            {
                AppInfo appRef = app;
                btnComp.onClick.AddListener(() => OnAppSelected(appRef));
            }
        }
    }

    Sprite GetAppIconSprite(string packageName)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Debug.Log($"[Icon] Start GetAppIconSprite pour {packageName}");
        try
        {
            using (var jc = new AndroidJavaClass("com.yourcompany.iconfetcher.IconFetcher"))
            {
                Debug.Log("[Icon] AndroidJavaClass loaded");
                using (var context = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    Debug.Log("[Icon] Context récupéré");
                    byte[] iconBytes = jc.CallStatic<byte[]>("getAppIcon", packageName, context);
                    if (iconBytes == null)
                    {
                        Debug.LogWarning($"[Icon] getAppIcon RX null pour {packageName}");
                    }
                    else if (iconBytes.Length == 0)
                    {
                        Debug.LogWarning($"[Icon] getAppIcon RX vide pour {packageName}");
                    }
                    else
                    {
                        Debug.Log($"[Icon] Bytes reçus ({iconBytes.Length}) pour {packageName}");
                        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (tex.LoadImage(iconBytes))
                        {
                            Debug.Log("[Icon] Texture chargée OK");
                            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                            Debug.Log("[Icon] Sprite créé OK");
                            return sprite;
                        }
                        else
                        {
                            Debug.LogWarning("[Icon] LoadImage échec");
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Icon] Exception: " + e);
        }
#endif
        Debug.Log("[Icon] Fin GetAppIconSprite → null");
        return null;
    }

    void OnAppSelected(AppInfo app)
    {
        selectedApp = app;
        infoText.text = $"Nom: {app.appName}\nPackage: {app.packageName ?? "N/A"}\nAPK Path: {app.apkFilePath ?? "N/A"}";
        infoIcon.sprite = app.appIcon ?? defaultSprite;
        Debug.Log($"[Select] infoIcon sprite pour {app.appName}: {(app.appIcon != null ? "ICON OK" : "DEF Sprite")}");
    }

    void OnPlayButtonClicked()
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

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && appJustLaunched)
        {
            Debug.Log("[AppLauncher] Retour depuis l’app lancée → relance du launcher");
            appJustLaunched = false;
            RefreshApps();
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Debug.Log("[AppLauncher] Application mise en pause (autre app ouverte)");
        }
    }

    public void OnExternalAppClosed(string msg)
    {
        Debug.Log("[AppLauncher] L'app externe a été fermée — retour automatique au launcher !");
        appJustLaunched = false;
        RefreshApps();
    }
}
