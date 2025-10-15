package be.leclick.mylauncher;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.util.Log;
import com.unity3d.player.UnityPlayer;

public class LauncherActivity extends Activity {
    private static final int REQUEST_LAUNCH_APP = 1;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);

        if (requestCode == REQUEST_LAUNCH_APP) {
            Log.d("LauncherActivity", "Retour d'une app externe");
            UnityPlayer.UnitySendMessage("AppLauncher", "OnExternalAppClosed", "");
        }
    }

    public void launchExternalApp(String packageName) {
        try {
            Intent intent = getPackageManager().getLaunchIntentForPackage(packageName);
            if (intent != null) {
                startActivityForResult(intent, REQUEST_LAUNCH_APP);
                Log.d("LauncherActivity", "Application lancée : " + packageName);
            } else {
                Log.e("LauncherActivity", "Package introuvable : " + packageName);
            }
        } catch (Exception e) {
            Log.e("LauncherActivity", "Erreur lancement app : " + e.getMessage());
        }
    }
}
