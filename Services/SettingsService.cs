using Microsoft.Maui.Storage;

namespace MusicScoreManager.Services
{
    public class SettingsService
    {
        private const string ScoresRootKey = "ScoresRootDirectory";
        private const string AudioRootKey = "AudioRootDirectory";

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
            
            var root = isAudio ? AudioRootDirectory : ScoresRootDirectory;
            if (fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetRelativePath(root, fullPath);
            }
            
            return fullPath; 
        }
    }
}
