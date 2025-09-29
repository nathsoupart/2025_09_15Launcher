using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;

public class DetectApk : MonoBehaviour
{
    [System.Serializable]
    public class AppInfo
    {
        public string packageName;
        public string appName;
        public Sprite appIcon;
    }

    public GameObject buttonPrefab;  // Votre prefab bouton avec Text + Image
    public Transform buttonsParent;  // Panel/parent UI pour contenir les boutons

    private bool permissionGranted = false;
    private List<AppInfo> installedApps = new List<AppInfo>();

    void Start()
    {
        CheckPermission();
    }

    void CheckPermission()
    {
        if (!Permission.HasUserAuthorizedPermission("android.permission.READ_EXTERNAL_STORAGE"))
        {
            // Demande la permission avec gestion des callbacks
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += PermissionGranted;
            callbacks.PermissionDenied += PermissionDenied;
            callbacks.PermissionDeniedAndDontAskAgain += PermissionDenied;

            Permission.RequestUserPermission("android.permission.READ_EXTERNAL_STORAGE", callbacks);
        }
        else
        {
            permissionGranted = true;
            BeginDetection();
        }
    }

    void PermissionGranted(string permission)
    {
        Debug.Log($"Permission accordée : {permission}");
        permissionGranted = true;
        BeginDetection();
    }

    void PermissionDenied(string permission)
    {
        Debug.LogWarning($"Permission refusée : {permission}");
        // Gérer le refus selon votre logique (afficher message, désactiver fonctions, ...)
    }

    void BeginDetection()
    {
        Debug.Log("Démarrage détection des apps");
        installedApps = GetInstalledApps();
        CreateButtons();
    }

    List<AppInfo> GetInstalledApps()
    {
        List<AppInfo> appsList = new List<AppInfo>();

#if UNITY_ANDROID && !UNITY_EDITOR
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        {
            var pm = currentActivity.Call<AndroidJavaObject>("getPackageManager");
            var apps = pm.Call<AndroidJavaObject>("getInstalledApplications", 0);
            int size = apps.Call<int>("size");

            for (int i = 0; i < size; i++)
            {
                var appInfo = apps.Call<AndroidJavaObject>("get", i);
                string packageName = appInfo.Get<string>("packageName");
                string appName = pm.Call<string>("getApplicationLabel", appInfo);

                Sprite appIcon = null; // À convertir via plugin natif si besoin

                appsList.Add(new AppInfo { packageName = packageName, appName = appName, appIcon = appIcon });
            }
        }
#endif
        return appsList;
    }

    void CreateButtons()
    {
        foreach (var app in installedApps)
        {
            GameObject btnObj = Instantiate(buttonPrefab, buttonsParent);

            Text btnText = btnObj.GetComponentInChildren<Text>();
            if (btnText != null)
                btnText.text = app.appName;

            Image btnImage = btnObj.GetComponentInChildren<Image>();
            if (btnImage != null && app.appIcon != null)
                btnImage.sprite = app.appIcon;

            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                string packageNameCopy = app.packageName;
                btn.onClick.AddListener(() => LaunchApp(packageNameCopy));
            }
        }
    }

    void LaunchApp(string packageName)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var pm = currentActivity.Call<AndroidJavaObject>("getPackageManager"))
        using (var launchIntent = pm.Call<AndroidJavaObject>("getLaunchIntentForPackage", packageName))
        {
            if (launchIntent != null)
            {
                currentActivity.Call("startActivity", launchIntent);
            }
        }
#endif
    }
}
