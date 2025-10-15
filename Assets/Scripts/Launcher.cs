using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android; // pour AndroidApplication.currentActivity

public class Launcher : MonoBehaviour
{
    [SerializeField] private string packageName = "be.leclick.mylauncher"; // à adapter
    [SerializeField] private Button launcherButton;

    private void Awake()
    {
        Debug.Log("Launcher.Awake : script chargé");
        if (launcherButton != null)
            launcherButton.onClick.AddListener(LaunchApp);
    }

    public void LaunchApp()
    {
        Debug.Log("Launcher.LaunchApp : bouton appuyé, début du lancement");

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // Récupère l’activité courante via l’API moderne Unity 6
            var currentActivity = AndroidApplication.currentActivity;
            if (currentActivity == null)
            {
                Debug.LogError("Launcher.LaunchApp : currentActivity est null !");
                return;
            }

            Debug.Log("Launcher.LaunchApp : création de l’intent");
            AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent");
            intent.Call<AndroidJavaObject>(
                "setClassName",
                packageName, // nom du package cible
                "com.unity3d.player.UnityPlayerGameActivity" // activité cible (adapter selon l’app)
            );

            Debug.Log("Launcher.LaunchApp : startActivity appelé");
            currentActivity.Call("startActivity", intent);
            Debug.Log("Launcher.LaunchApp : application lancée avec succès !");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Launcher.LaunchApp : exception -> " + e.Message);
        }
#else
        Debug.LogWarning("Launcher.LaunchApp : ce code ne tourne que sur Android !");
#endif
    }
}