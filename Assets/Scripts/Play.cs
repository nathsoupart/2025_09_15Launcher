using UnityEngine;
using UnityEngine.UI;

public class Play : MonoBehaviour
{
    [SerializeField] private string packageName = "com.whatsapp"; // change selon l'app à lancer
    [SerializeField] private Button launchButton;

    private void Awake()
    {
        Debug.Log("Play.Awake : script chargé");
    }

    private void Start()
    {
        Debug.Log("Play.Start : script démarré");
        if (launchButton != null)
        {
            launchButton.onClick.AddListener(LaunchApp);
            Debug.Log("Play.Start : Listener bouton ajouté");
        }
        else
        {
            Debug.LogError("Play.Start : launchButton non assigné !");
        }
    }

    public void LaunchApp()
    {
        Debug.Log("Play.LaunchApp : bouton appuyé, début du lancement");
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            Debug.Log("Play.LaunchApp : currentActivity récupéré");

            try
            {
                Debug.Log("Play.LaunchApp : création de l'intent");
                var intent = new AndroidJavaObject("android.content.Intent");
                intent.Call<AndroidJavaObject>("setClassName",
                    "com.leclick.demoa",   // <-- package cible
                    "com.unity3d.player.UnityPlayerGameActivity"); // <-- activité exacte

                Debug.Log("Play.LaunchApp : startActivity appelé");
                currentActivity.Call("startActivity", intent);
                Debug.Log("Play.LaunchApp :  App lancée !");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Play.LaunchApp :  Impossible de lancer : " + e.Message);
            }
        }
    }
}