using AssetsTools.NET;
using AssetsTools.NET.Extra;
using BepInEx.Logging;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace QModManager
{
    /// <summary>
    /// A patcher which runs ahead of UnityPlayer to enable VR in the Global Game Manager.
    /// </summary>
    public static class VREnabler
    {
        internal static string UnityAudioFixerPath => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        internal static string GameRootPath => BepInEx.Paths.GameRootPath;
        internal static string ManagedPath => BepInEx.Paths.ManagedPath;

        private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource("UnityAudioFixer");

        /// <summary>
        /// Called from BepInEx while patching, our entry point for patching.
        /// Do not change the method name as it is identified by BepInEx. Method must remain public.
        /// </summary>
        [Obsolete("Should not be used!", true)]
        public static void Initialize()
        {
            EnableVROptions(Path.Combine(ManagedPath, "../globalgamemanagers"));
        }

        private static void EnableVROptions(string path)
        {
            AssetsManager am = new AssetsManager();
            AssetsFileInstance afi = am.LoadAssetsFile(path, false);
            am.LoadClassDatabase(Path.Combine(UnityAudioFixerPath, "cldb.dat"));


            for(int i = 0; i < afi.table.assetFileInfoCount; i++)
            {
                try
                {
                    AssetFileInfoEx info = afi.table.GetAssetInfo(i);
                    AssetTypeInstance ati = am.GetATI(afi.file, info);
                    AssetTypeValueField baseField = ati?.GetBaseField();

                    AssetTypeValueField enabledVRDevicesField = baseField?.Get("enabledVRDevices");

                    if (enabledVRDevicesField is null)
                        continue;

                    AssetTypeValueField vrArrayField = enabledVRDevicesField.Get("Array");

                    if (vrArrayField is null)
                        continue;


                    AssetTypeValueField Oculus = ValueBuilder.DefaultValueFieldFromArrayTemplate(vrArrayField);
                    Oculus.GetValue().Set("Oculus");
                    AssetTypeValueField OpenVR = ValueBuilder.DefaultValueFieldFromArrayTemplate(vrArrayField);
                    OpenVR.GetValue().Set("OpenVR");
                    AssetTypeValueField None = ValueBuilder.DefaultValueFieldFromArrayTemplate(vrArrayField);
                    None.GetValue().Set("None");

                    vrArrayField.SetChildrenList(new AssetTypeValueField[] { Oculus, OpenVR, None });

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
                    return;
                }
                catch { }
            }

            Logger.LogError("VR enable location not found!");

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
