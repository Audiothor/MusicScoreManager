using Microsoft.Maui.Storage;

namespace MusicScoreManager.Services
{
    public class SettingsService
    {
        private const string ScoresRootKey = "ScoresRootDirectory";
        private const string AudioRootKey = "AudioRootDirectory";
        private const string ExportsRootKey = "ExportsRootDirectory";

        public string ScoresRootDirectory
        {
            get => Preferences.Get(ScoresRootKey, GetDefaultScoresRoot());
            set => Preferences.Set(ScoresRootKey, value);
        }

        public string AudioRootDirectory
        {
            get => Preferences.Get(AudioRootKey, GetDefaultAudioRoot());
            set => Preferences.Set(AudioRootKey, value);
        }

        public string ExportsRootDirectory
        {
            get => Preferences.Get(ExportsRootKey, GetDefaultExportsRoot());
            set => Preferences.Set(ExportsRootKey, value);
        }

        public string GetDefaultExportsRoot()
        {
            string path;
#if ANDROID
            try
            {
                var downloadDir = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads)?.AbsolutePath;
                if (!string.IsNullOrEmpty(downloadDir))
                {
                    path = downloadDir;
                }
                else
                {
                    path = "/storage/emulated/0/Download";
                }
            }
            catch
            {
                path = "/storage/emulated/0/Download";
            }
#else
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string downloads = Path.Combine(userProfile, "Downloads");
            path = Directory.Exists(downloads) ? downloads : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Downloads");
#endif
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch { }
            
            return path;
        }

        public string GetDefaultScoresRoot()
        {
            string baseDir;
#if ANDROID
            // Dossier externe visible sur Android (Android/data/com.companyname.musicscoremanager/files)
            var context = Android.App.Application.Context;
            baseDir = context.GetExternalFilesDir(null)?.AbsolutePath ?? FileSystem.AppDataDirectory;
#else
            // Dossier Documents sur Windows/iOS
            baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MusicScoreManager");
#endif
            var path = Path.Combine(baseDir, "Scores");
            
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            
            return path;
        }

        public string GetDefaultAudioRoot()
        {
            string baseDir;
#if ANDROID
            var context = Android.App.Application.Context;
            baseDir = context.GetExternalFilesDir(null)?.AbsolutePath ?? FileSystem.AppDataDirectory;
#else
            baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MusicScoreManager");
#endif
            var path = Path.Combine(baseDir, "Audio");
            
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            
            return path;
        }

        public string GetAbsolutePath(string storedPath, bool isAudio = false)
        {
            if (string.IsNullOrWhiteSpace(storedPath)) return string.Empty;
            
            if (Path.IsPathRooted(storedPath))
            {
                return storedPath;
            }
            
            var root = isAudio ? AudioRootDirectory : ScoresRootDirectory;
            return Path.Combine(root, storedPath);
        }

        public string GetRelativePath(string fullPath, bool isAudio = false)
        {
            if (string.IsNullOrWhiteSpace(fullPath)) return string.Empty;
            
            try
            {
                var root = isAudio ? AudioRootDirectory : ScoresRootDirectory;
                
                string normFullPath = Path.GetFullPath(fullPath);
                string normRoot = Path.GetFullPath(root);

                if (normFullPath.StartsWith(normRoot, StringComparison.OrdinalIgnoreCase))
                {
                    return Path.GetRelativePath(normRoot, normFullPath);
                }
            }
            catch { }
            
            return fullPath; 
        }
    }
}
