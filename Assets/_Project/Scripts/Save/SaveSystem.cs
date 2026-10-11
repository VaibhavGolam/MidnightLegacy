using System;
using System.IO;
using UnityEngine;

namespace MidnightLegacy
{
    /// <summary>
    /// JSON save in Application.persistentDataPath. Writes to a temp file first and then swaps it in, and keeps
    /// the previous save as a backup, so a crash or a full disk in the middle of a write cannot lose progress.
    /// </summary>
    public static class SaveSystem
    {
        const string FileName = "midnight_legacy_save.json";
        const string BackupName = "midnight_legacy_save.bak";
        const string TempName = "midnight_legacy_save.tmp";

        static SaveData data;

        public static string Dir { get { return Application.persistentDataPath; } }
        static string Path(string name) { return System.IO.Path.Combine(Dir, name); }

        public static SaveData Data
        {
            get
            {
                if (data == null) Load();
                return data;
            }
        }

        public static bool HasSave()
        {
            return File.Exists(Path(FileName)) || File.Exists(Path(BackupName));
        }

        public static void Load()
        {
            data = TryRead(Path(FileName));
            if (data == null) data = TryRead(Path(BackupName));
            if (data == null) data = new SaveData();
            data.Normalize();
        }

        static SaveData TryRead(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string json = File.ReadAllText(path);
                if (string.IsNullOrEmpty(json)) return null;
                SaveData d = JsonUtility.FromJson<SaveData>(json);
                if (d != null) d.Normalize();
                return d;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] could not read " + path + ": " + e.Message);
                return null;
            }
        }

        public static void Save()
        {
            if (data == null) return;
            try
            {
                data.lastSaved = DateTime.UtcNow.Ticks;
                string json = JsonUtility.ToJson(data, true);
                string main = Path(FileName), tmp = Path(TempName), bak = Path(BackupName);
                File.WriteAllText(tmp, json);
                if (File.Exists(main))
                {
                    if (File.Exists(bak)) File.Delete(bak);
                    File.Move(main, bak);
                }
                File.Move(tmp, main);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] write failed: " + e.Message);
            }
        }

        public static void Delete()
        {
            try
            {
                string[] names = { FileName, BackupName, TempName };
                for (int i = 0; i < names.Length; i++) if (File.Exists(Path(names[i]))) File.Delete(Path(names[i]));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Save] delete failed: " + e.Message);
            }
            data = new SaveData();
            data.Normalize();
        }
    }
}
