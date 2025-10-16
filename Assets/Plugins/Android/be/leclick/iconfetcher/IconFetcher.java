package be.leclick.iconfetcher;

import android.content.Context;
import android.content.pm.PackageManager;
import android.graphics.Bitmap;
import android.graphics.drawable.BitmapDrawable;
import android.graphics.drawable.Drawable;
import java.io.ByteArrayOutputStream;
import java.io.IOException;

/**
 * Plugin Unity permettant de récupérer l'icône d'une app installée en PNG (byte[]).
 * Appelé depuis C# via AndroidJavaClass("com.yourcompany.iconfetcher.IconFetcher")
 */
public class IconFetcher {

    public static byte[] getAppIcon(String packageName, Context context) {
        try {
            if (context == null || packageName == null || packageName.isEmpty()) {
                android.util.Log.w("IconFetcher", "Contexte ou packageName invalide.");
                return null;
            }

            PackageManager pm = context.getPackageManager();
            Drawable drawable = pm.getApplicationIcon(packageName);
            if (drawable == null) {
                android.util.Log.w("IconFetcher", "Icône non trouvée pour " + packageName);
                return null;
            }

            Bitmap bitmap;

            // Conversion en Bitmap
            if (drawable instanceof BitmapDrawable) {
                bitmap = ((BitmapDrawable) drawable).getBitmap();
            } else {
                int width = drawable.getIntrinsicWidth() > 0 ? drawable.getIntrinsicWidth() : 96;
                int height = drawable.getIntrinsicHeight() > 0 ? drawable.getIntrinsicHeight() : 96;
                bitmap = Bitmap.createBitmap(width, height, Bitmap.Config.ARGB_8888);
                android.graphics.Canvas canvas = new android.graphics.Canvas(bitmap);
                drawable.setBounds(0, 0, canvas.getWidth(), canvas.getHeight());
                drawable.draw(canvas);
            }

            // Compression PNG
            ByteArrayOutputStream stream = new ByteArrayOutputStream();
            bitmap.compress(Bitmap.CompressFormat.PNG, 100, stream);
            byte[] bytes = stream.toByteArray();
            stream.close();

            android.util.Log.i("IconFetcher", "Icône récupérée pour " + packageName + " (" + bytes.length + " octets)");
            return bytes;

        } catch (PackageManager.NameNotFoundException e) {
            android.util.Log.e("IconFetcher", "Package non trouvé: " + packageName);
        } catch (IOException e) {
            android.util.Log.e("IconFetcher", "Erreur IO: " + e.getMessage());
        } catch (Exception e) {
            android.util.Log.e("IconFetcher", "Erreur inattendue: " + e.getMessage());
        }
        return null;
    }
}
