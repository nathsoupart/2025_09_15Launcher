using UnityEngine;
using UnityEngine.UI;

public class Launcher : MonoBehaviour
{
    [SerializeField] private string packageName = "be.leclick.mylauncher"; // change selon l'app à lancer
    [SerializeField] public Button launcherbutton;

    public void Awake()
    {
        Debug.Log("Launcher.Awake : script chargé");
    }

   

    public void LaunchApp()
    {
        Debug.Log("Launcher.LaunchApp : bouton appuyé, début du lancement");
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            Debug.Log("Launcher.LaunchApp : currentActivity récupéré");

            try
            {
                Debug.Log("Launcher.LaunchApp : création de l'intent");
                var intent = new AndroidJavaObject("android.content.Intent");
                intent.Call<AndroidJavaObject>("setClassName",
                    "be.leclick.mylauncher",   // <-- package cible
                    "com.unity3d.player.UnityPlayerGameActivity"); // <-- activité exacte

                Debug.Log("Launcher.LaunchApp : startActivity appelé");
                currentActivity.Call("startActivity", intent);
                Debug.Log("Launcher.LaunchApp :  App lancée !");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Launcher.LaunchApp :  Impossible de lancer : " + e.Message);
            }
        }
    }
}