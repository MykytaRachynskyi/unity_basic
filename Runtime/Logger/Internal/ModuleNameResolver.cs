using System.Collections.Generic;
using System.IO;

namespace Basic.Logger
{
    internal static class ModuleNameResolver
    {
        private static readonly Dictionary<string, string> Cache = new();

        private const string ModulesFolder = "Modules";
        private const char ForwardSlash = '/';
        private const char BackSlash = '\\';

        public static string Resolve(string callerFilePath)
        {
            if (string.IsNullOrEmpty(callerFilePath))
                return "Unknown";

            if (Cache.TryGetValue(callerFilePath, out var cached))
                return cached;

            var resolved = Extract(callerFilePath);
            Cache[callerFilePath] = resolved;
            return resolved;
        }

        private static string Extract(string path)
        {
            // Normalize to forward slashes for consistent parsing
            var normalized = path.Replace(BackSlash, ForwardSlash);

            // Look for "Modules/{Name}/" pattern
            var modulesIndex = normalized.IndexOf(ModulesFolder + ForwardSlash);
            if (modulesIndex >= 0)
            {
                var afterModules = modulesIndex + ModulesFolder.Length + 1;
                var nextSlash = normalized.IndexOf(ForwardSlash, afterModules);
                if (nextSlash > afterModules)
                    return normalized.Substring(afterModules, nextSlash - afterModules);
            }

            // Fallback: return filename without extension
            return Path.GetFileNameWithoutExtension(path);
        }
    }
}
