using Android.App;
using Android.Content.PM;
using Android.OS;

namespace MusicScoreManager;

[Activity(Label = "MusicScoreManager", Icon = "@mipmap/appicon", Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        CheckStoragePermissions();
    }

    private void CheckStoragePermissions()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(30))
        {
            if (!Android.OS.Environment.IsExternalStorageManager)
            {
                try
                {
                    Android.Content.Intent intent = new Android.Content.Intent(Android.Provider.Settings.ActionManageAppAllFilesAccessPermission);
                    string pkgName = PackageName ?? string.Empty;
                    Android.Net.Uri? uri = Android.Net.Uri.FromParts("package", pkgName, null);
                    if (uri != null)
                    {
                        intent.SetData(uri);
                    }
                    StartActivity(intent);
                }
                catch
                {
                    Android.Content.Intent intent = new Android.Content.Intent();
                    intent.SetAction(Android.Provider.Settings.ActionManageAllFilesAccessPermission);
                    StartActivity(intent);
                }
            }
        }
    }
}
