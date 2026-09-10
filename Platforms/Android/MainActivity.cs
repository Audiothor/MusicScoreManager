using Android.App;
using Android.Content.PM;
using Android.Media.Midi;
using Android.OS;
using Android.Views;
using MusicScoreManager.Models;
using MusicScoreManager.Services;
using System;

namespace MusicScoreManager;

[Activity(Label = "MusicScoreManager", Icon = "@mipmap/appicon", Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    private MidiManager? _midiManager;
    private MidiDeviceCallbackListener? _midiCallback;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        CheckStoragePermissions();
        InitMidiManager();
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

    #region Interception des Touches Clavier & Pédaliers Bluetooth HID

    public override bool DispatchKeyEvent(KeyEvent? e)
    {
        if (e != null && PedalMidiService.Instance.IsEnabled)
        {
            var currentFocus = CurrentFocus;
            bool isTextInput = currentFocus is Android.Widget.EditText;

            // Touches clés de pédale prioritaires
            bool isPedalKey = e.KeyCode == Keycode.PageDown ||
                              e.KeyCode == Keycode.PageUp ||
                              e.KeyCode == Keycode.DpadRight ||
                              e.KeyCode == Keycode.DpadLeft ||
                              e.KeyCode == Keycode.DpadDown ||
                              e.KeyCode == Keycode.DpadUp ||
                              e.KeyCode == Keycode.F3 ||
                              e.KeyCode == Keycode.F4 ||
                              e.KeyCode == Keycode.F5 ||
                              e.KeyCode == Keycode.F6 ||
                              e.KeyCode == Keycode.F7 ||
                              e.KeyCode == Keycode.F8;

            if (!isTextInput || isPedalKey)
            {
                if (e.Action == KeyEventActions.Down)
                {
                    if (e.RepeatCount == 0)
                    {
                        PedalMidiService.Instance.ProcessKeyDown((int)e.KeyCode, e.KeyCode.ToString(), "Bluetooth HID / Clavier");
                    }
                    return true;
                }
                else if (e.Action == KeyEventActions.Up)
                {
                    PedalMidiService.Instance.ProcessKeyUp((int)e.KeyCode, e.KeyCode.ToString(), "Bluetooth HID / Clavier");
                    return true;
                }
            }
        }

        return base.DispatchKeyEvent(e);
    }

    #endregion

    #region Gestion des Contrôleurs & Pédales MIDI Android

    private void InitMidiManager()
    {
        try
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(23))
            {
                _midiManager = (MidiManager?)GetSystemService(MidiService);
                if (_midiManager != null && Looper.MainLooper != null)
                {
#pragma warning disable CA1422
                    var devices = _midiManager.GetDevices();
                    if (devices != null)
                    {
                        foreach (var dev in devices)
                        {
                            OpenMidiDevice(dev);
                        }
                    }

                    _midiCallback = new MidiDeviceCallbackListener(this);
                    _midiManager.RegisterDeviceCallback(_midiCallback, new Handler(Looper.MainLooper));
#pragma warning restore CA1422
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainActivity] Erreur initialisation MIDI: {ex.Message}");
        }
    }

    public void OpenMidiDevice(MidiDeviceInfo? devInfo)
    {
        if (devInfo == null || _midiManager == null) return;

        try
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(23) && Looper.MainLooper != null)
            {
                _midiManager.OpenDevice(devInfo, new MidiOpenListener(), new Handler(Looper.MainLooper));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainActivity] Erreur ouverture MIDI device: {ex.Message}");
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("android23.0")]
    private class MidiDeviceCallbackListener : MidiManager.DeviceCallback
    {
        private readonly MainActivity _activity;
        public MidiDeviceCallbackListener(MainActivity activity) => _activity = activity;

        public override void OnDeviceAdded(MidiDeviceInfo? device)
        {
            base.OnDeviceAdded(device);
            _activity.OpenMidiDevice(device);
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("android23.0")]
    private class MidiOpenListener : Java.Lang.Object, MidiManager.IOnDeviceOpenedListener
    {
        public void OnDeviceOpened(MidiDevice? device)
        {
            if (device == null) return;
            try
            {
                var info = device.Info;
                int portCount = info?.OutputPortCount ?? 0;
                for (int i = 0; i < portCount; i++)
                {
                    var outputPort = device.OpenOutputPort(i);
                    if (outputPort != null)
                    {
                        outputPort.Connect(new AndroidNativeMidiReceiver());
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainActivity] Erreur connexion port MIDI: {ex.Message}");
            }
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("android23.0")]
    private class AndroidNativeMidiReceiver : MidiReceiver
    {
        public override void OnSend(byte[]? msg, int offset, int count, long timestamp)
        {
            if (msg == null || count <= 0) return;

            try
            {
                int pos = offset;
                while (pos < offset + count)
                {
                    byte status = msg[pos];
                    if ((status & 0x80) != 0) // Status byte
                    {
                        int cmd = status & 0xF0;
                        if (cmd == 0x90 && pos + 2 < offset + count) // Note On
                        {
                            int note = msg[pos + 1];
                            int vel = msg[pos + 2];
                            PedalMidiService.Instance.ProcessMidiMessage(PedalInputType.MidiNote, note, vel, "MIDI USB/BT");
                            pos += 3;
                        }
                        else if (cmd == 0x80 && pos + 2 < offset + count) // Note Off
                        {
                            pos += 3;
                        }
                        else if (cmd == 0xB0 && pos + 2 < offset + count) // Control Change
                        {
                            int ccNum = msg[pos + 1];
                            int ccVal = msg[pos + 2];
                            PedalMidiService.Instance.ProcessMidiMessage(PedalInputType.MidiCC, ccNum, ccVal, "MIDI USB/BT");
                            pos += 3;
                        }
                        else if (cmd == 0xC0 && pos + 1 < offset + count) // Program Change
                        {
                            int prog = msg[pos + 1];
                            PedalMidiService.Instance.ProcessMidiMessage(PedalInputType.MidiProgramChange, prog, 127, "MIDI USB/BT");
                            pos += 2;
                        }
                        else
                        {
                            pos++;
                        }
                    }
                    else
                    {
                        pos++;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AndroidNativeMidiReceiver] Erreur parsing MIDI: {ex.Message}");
            }
        }
    }

    #endregion
}
