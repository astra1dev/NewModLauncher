using System;

namespace NewModLauncher
{
    public static class VersionUtils
    {
        public static string CleanVersion(string v)
        {
            if (string.IsNullOrWhiteSpace(v)) return "0.0.0";
            return v.Trim().TrimStart('v', 'V').TrimEnd('s');
        }

        public static Version Parse(string v)
        {
            v = CleanVersion(v);
            var parts = v.Split('.');
            int major = parts.Length > 0 ? int.Parse(parts[0]) : 0;
            int minor = parts.Length > 1 ? int.Parse(parts[1]) : 0;
            int patch = parts.Length > 2 ? int.Parse(parts[2]) : 0;
            return new Version(major, minor, patch);
        }

        public static int Compare(string a, string b)
        {
            var va = Parse(a);
            var vb = Parse(b);
            return va.CompareTo(vb);
        }
    }
}
