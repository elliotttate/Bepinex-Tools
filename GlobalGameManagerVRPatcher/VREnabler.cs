namespace VRPatcher
{
    using AssetsTools.NET;
    using AssetsTools.NET.Extra;
    using BepInEx.Logging;
    using Mono.Cecil;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    /// <summary>
    /// A patcher which runs ahead of UnityPlayer to enable VR in the Global Game Manager and copy the required plugins.
    /// </summary>
    public static class VREnabler
    {
        internal static string VRPatcherPath => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        internal static string ManagedPath => BepInEx.Paths.ManagedPath;
        internal static string PluginsPath => Path.Combine(ManagedPath, "../Plugins");

        private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("VREnabler");
        private static string VRMode = "none";

        /// <summary>
        /// Called from BepInEx while patching, our entry point for patching.
        /// Do not change the method name as it is identified by BepInEx. Method must remain public.
        /// </summary>
        [Obsolete("Should not be used!", true)]
        public static void Initialize()
        {
            string configPath = Path.Combine(Environment.CurrentDirectory, "VRMODE.txt");
            if(!File.Exists(configPath))
            {
                using(StreamWriter writer = new StreamWriter(configPath))
                {
                    try
                    {
                        writer.WriteLine(VRMode);
                        writer.Close();
                    }
                    catch { writer.Close(); }
                }
            }
            else
            {
                using(StreamReader reader = new StreamReader(configPath))
                {
                    try
                    {
                        VRMode = reader.ReadToEnd().Trim().ToLower();
                        reader.Close();
                    }
                    catch(Exception e)
                    {
                        reader.Close();
                        Logger.LogError($"Failed loading config. Unpatching GGM");
                        Logger.LogError($"{e.Message}");
                        PatchVROptions(true);
                        return;
                    }
                }
            }

            if(PatchVROptions() && VRMode != "none")
            {
                Logger.LogInfo("Checking for VR plugins...");

                string path2 = Path.Combine(PluginsPath, "x86_64");

                DirectoryInfo gamePluginsDirectory;

                if (!Directory.Exists(path2))
                {
                    gamePluginsDirectory = new DirectoryInfo(PluginsPath);
                }
                else
                {
                    gamePluginsDirectory = new DirectoryInfo(path2);
                }

                string[] pluginNames = new string[]
                {
                        "AudioPluginOculusSpatializer.dll",
                        "openvr_api.dll",
                        "OVRGamepad.dll",
                        "OVRPlugin.dll"
                };

                FileInfo[] gamePluginFiles = gamePluginsDirectory.GetFiles();

                bool hasCopied = false;

                Assembly assembly = Assembly.GetExecutingAssembly();
                string assemblyName = assembly.GetName().Name;


                foreach (string pluginName in pluginNames)
                {
                    if (!Array.Exists<FileInfo>(gamePluginFiles, (file) => pluginName == file.Name))
                    {
                        hasCopied = true;
                        using (var resource = assembly.GetManifestResourceStream($"{assemblyName}.Plugins." + pluginName))
                        {
                            using (var file = new FileStream(Path.Combine(gamePluginsDirectory.FullName, pluginName), FileMode.Create, FileAccess.Write, FileShare.Delete))
                            {
                                Logger.LogInfo("Copying " + pluginName);
                                resource.CopyTo(file);
                            }
                        }
                    }
                }

                if (hasCopied)
                    Logger.LogInfo("Successfully copied VR plugins!");
                else
                    Logger.LogInfo("VR plugins already present");
                return;
            }
        }

        private static void VREnabler_Exited(object sender, EventArgs e)
        {
            PatchVROptions(true);
        }

        private static bool PatchVROptions(bool UnPatch = false)
        {
            string path = Path.Combine(ManagedPath, "../globalgamemanagers");
            AssetsManager am = new AssetsManager();
            AssetsFileInstance afi = am.LoadAssetsFile(path, false);
            am.LoadClassDatabase(Path.Combine(VRPatcherPath, "cldb.dat"));


            for(int i = 0; i < afi.table.assetFileInfoCount; i++)
            {
                try
                {
                    AssetFileInfoEx info = afi.table.GetAssetInfo(i);
                    AssetTypeInstance ati = am.GetTypeInstance(afi.file, info);
                    AssetTypeValueField baseField = ati?.GetBaseField();

                    AssetTypeValueField enabledVRDevicesField = baseField?.Get("enabledVRDevices");

                    if (enabledVRDevicesField is null)
                        continue;

                    AssetTypeValueField vrArrayField = enabledVRDevicesField.Get("Array");

                    if (vrArrayField is null)
                        continue;

                    AssetTypeValueField field = ValueBuilder.DefaultValueFieldFromArrayTemplate(vrArrayField);
                    field.GetValue().Set("None");

                    switch(VRMode)
                    {
                        case "oculus":
                            field.GetValue().Set("Oculus");
                            break;
                        case "steamvr":
                        case "openvr":
                            field.GetValue().Set("OpenVR");
                            break;
                        case "none":
                            UnPatch = true;
                            break;
                        default:
                            UnPatch = true;
                            Logger.LogMessage($"Unknown VR config string: {VRMode} Unpatching VR");
                            VRMode = "none";
                            break;
                    }


                    if(!UnPatch)
                    {
                        vrArrayField.SetChildrenList(new AssetTypeValueField[] { field });
                        Logger.LogMessage("Patching GGM");
                    }
                    else
                    {
                        vrArrayField.SetChildrenList(new AssetTypeValueField[] { field });
                        Logger.LogMessage("UnPatching GGM");
                    }

                    byte[] vrAsset;
                    using (MemoryStream memStream = new MemoryStream())
                    using (AssetsFileWriter writer = new AssetsFileWriter(memStream))
                    {
                        writer.bigEndian = false;
                        baseField.Write(writer);
                        vrAsset = memStream.ToArray();
                    }

                    List<AssetsReplacer> rep = new List<AssetsReplacer>() { new AssetsReplacerFromMemory(0, i, (int)info.curFileType, 0xFFFF, vrAsset) };

                    using (MemoryStream memStream = new MemoryStream())
                    using (AssetsFileWriter writer = new AssetsFileWriter(memStream))
                    {
                        afi.file.Write(writer, 0, rep, 0);
                        afi.stream.Close();
                        File.WriteAllBytes(path, memStream.ToArray());
                    }
                    return true;
                }
                catch
                {
                }
            }

            Logger.LogError("VR enable location not found!");
            return false;

        }

        /// <summary>
        /// For BepInEx to identify your patcher as a patcher, it must match the patcher contract as outlined in the BepInEx docs:
        /// https://bepinex.github.io/bepinex_docs/v5.0/articles/dev_guide/preloader_patchers.html#patcher-contract
        /// It must contain a list of managed assemblies to patch as a public static <see cref="IEnumerable{T}"/> property named TargetDLLs
        /// </summary>
        [Obsolete("Should not be used!", true)]
        public static IEnumerable<string> TargetDLLs { get; } = new string[0];

        /// <summary>
        /// For BepInEx to identify your patcher as a patcher, it must match the patcher contract as outlined in the BepInEx docs:
        /// https://bepinex.github.io/bepinex_docs/v5.0/articles/dev_guide/preloader_patchers.html#patcher-contract
        /// It must contain a public static void method named Patch which receives an <see cref="AssemblyDefinition"/> argument,
        /// which patches each of the target assemblies in the TargetDLLs list.
        /// 
        /// We don't actually need to patch any of the managed assemblies, so we are providing an empty method here.
        /// </summary>
        /// <param name="ad"></param>
        [Obsolete("Should not be used!", true)]
        public static void Patch(AssemblyDefinition ad) { }
    }
}
