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
    private bool appJustLaunched = false; // <--- Pour savoir qu’on vient d’en lancer une

    void Start()
    {
        CheckStoragePermission();

        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButtonClicked);
    }

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

    public void RefreshApps()
    {
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

                for (int i = 0; i < size; i++)
                {
                    AndroidJavaObject appInfoObj = appsList.Call<AndroidJavaObject>("get", i);
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
            Debug.LogError("[AppLauncher] Erreur GetInstalledApps : " + e.Message);
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

            var iconTransform = btnObj.transform.Find("Icon");
            if (iconTransform != null)
            {
                var img = iconTransform.GetComponent<Image>();
                if (img != null)
                    img.sprite = app.appIcon ?? defaultSprite;
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
        catch { }
#endif
        return null;
    }

    void OnAppSelected(AppInfo app)
    {
        selectedApp = app;
        infoText.text = $"Nom: {app.appName}\nPackage: {app.packageName ?? "N/A"}\nAPK Path: {app.apkFilePath ?? "N/A"}";
        infoIcon.sprite = app.appIcon ?? defaultSprite;
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

    
  
    // Appelé quand on revient sur Unity après avoir lancé une autre app
    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && appJustLaunched)
        {
            Debug.Log("[AppLauncher] Retour depuis l’app lancée → relance du launcher");
            appJustLaunched = false;
            RefreshApps(); // ou recharger la scène pour repartir du début
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Debug.Log("[AppLauncher] Application mise en pause (autre app ouverte)");
        }
    }
    // Méthode appelée par le plugin Android au retour
    public void OnExternalAppClosed(string msg)
    {
        Debug.Log("[AppLauncher] L'app externe a été fermée — retour automatique au launcher !");
        appJustLaunched = false;
        RefreshApps(); // ou SceneManager.LoadScene(0) si tu veux un vrai redémarrage complet
    }

}
