using System.IO;
using System.Reflection;

namespace NewModLauncher.Services
{
    public class VersionCheckService
    {
        public bool IsNewModPresent(string amongUsPath, out string newModVersion)
        {
            var NewModDll = Path.Combine(amongUsPath, "BepInEx", "plugins", "NewMod.dll");
            if (!File.Exists(NewModDll))
            {
                newModVersion = "";
                return false;
            }
           try 
           {
             var assembly = Assembly.LoadFile(NewModDll);
             var ver = assembly.GetName().Version;
             newModVersion = ver != null ? ver.ToString() : "";
             return true;
           }
           catch
           {
             newModVersion = "";
           }
           return false;
        }

        public bool IsBepInExPresent(string amongUsPath, out string bepinExVersion)
        {
            var BepInExDirectory = Path.Combine(amongUsPath, "BepInEx");
            if (!Directory.Exists(BepInExDirectory))
            {
                bepinExVersion = "";
                return false;
            }
            bepinExVersion = "BepInEx 6.0.0-be.738";
            return true;
        }
    }
}