using System.Collections;
using System.IO;
using UnityEngine;

public class StreamingAssetsCopier : MonoBehaviour
{
    // Appeler cette fonction au Start() du launcher
    public void CopyImagesToPersistentData()
    {
        string sourceFolder = Application.streamingAssetsPath;
        string targetFolder = Application.persistentDataPath;

#if UNITY_ANDROID && !UNITY_EDITOR
        StartCoroutine(CopyFolderAndroid(sourceFolder, targetFolder));
#else
        CopyFolderStandalone(sourceFolder, targetFolder);
#endif
    }

    // Version pour PC / Editor / Standalone
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

        Debug.Log("[StreamingAssetsCopier] Images copiées dans persistentDataPath");
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    // Version Android, StreamingAssets compressé dans l’APK
    IEnumerator CopyFolderAndroid(string sourceFolder, string targetFolder)
    {
        if (!Directory.Exists(targetFolder))
            Directory.CreateDirectory(targetFolder);

        foreach (string fileName in new string[] { "demoa_preview.png", "test_preview.png" }) // liste des fichiers
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
                        Debug.Log($"[StreamingAssetsCopier] Copié {fileName} vers persistentDataPath");
                    }
                    else
                    {
                        Debug.LogWarning($"[StreamingAssetsCopier] Impossible de copier {fileName}: {www.error}");
                    }
                }
            }
        }
    }
#endif
}
