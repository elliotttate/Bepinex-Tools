namespace Bepinex_Publicizer
{
    using BepInEx;
    using BepInEx.Logging;
    using dnlib.DotNet;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.Text;
    using UnityEngine;
    using FieldAttributes = dnlib.DotNet.FieldAttributes;
    using MethodAttributes = dnlib.DotNet.MethodAttributes;
    using TypeAttributes = dnlib.DotNet.TypeAttributes;

    [BepInPlugin(GUID, MODNAME, VERSION)]
    public class Main : BaseUnityPlugin
    {
        #region[Declarations]

        public const string
            MODNAME = "Bepinex_Publicizer",
            AUTHOR = "MrPurple6411",
            GUID = AUTHOR + "_" + MODNAME,
            VERSION = "1.0.0.0";

        internal readonly ManualLogSource log;
        public Config config { get; private set; }

        #endregion

        public Main()
        {
            log = Logger;
        }

        public void Awake()
        {

            string configPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Config.json");

            if(File.Exists(configPath))
            {
                using(StreamReader reader = new StreamReader(configPath))
                {
                    try
                    {
                        string configstring = reader.ReadToEnd().Replace("\\\\", "/").Replace("\\", "/");
                        config = JsonUtility.FromJson<Config>(configstring);
                        reader.Close();
                    }
                    catch(Exception e)
                    {
                        log.LogError($"Failed to read {configPath}");
                        log.LogError(e);
                        reader.Close();
                        return;
                    }
                }
            }
            else
            {
                config = new Config();
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            if(config.OutputFolders.Count == 0)
            {
                foreach(Assembly assembly in assemblies)
                {
                    try
                    {
                        string path = Path.GetDirectoryName(assembly.Location);
                        if(path?.EndsWith("Managed") ?? false)
                        {
                            config.OutputFolders.Add(Directory.CreateDirectory(Path.Combine(path, "publicized_assemblies")).FullName);
                            using(StreamWriter writer = new StreamWriter(configPath))
                            {
                                try
                                {
                                    writer.WriteLine(JsonUtility.ToJson(config, true));
                                    writer.Flush();
                                    writer.Close();
                                }
                                catch
                                {
                                    writer.Close();
                                    if(File.Exists(configPath))
                                        File.Delete(configPath);
                                    return;
                                }
                            }
                            break;
                        }
                    }
                    catch { }
                }
            }

            StringBuilder stringBuilder = new StringBuilder(Environment.NewLine);
            stringBuilder.AppendLine();
            int totalCount = 0;
            foreach(string outputPath in config.OutputFolders)
            {
                if(!Directory.Exists(outputPath))
                {
                    log.LogError($"Output folder not found at {outputPath}");
                    return;
                }
                stringBuilder.AppendLine($"Publicizing files into {outputPath}");
                int count = 0;

                foreach(Assembly assembly in assemblies)
                {
                    string assemblyName = assembly.GetName().Name;
                    if(config.UseWhiteList)
                    {
                        foreach(string name in config.NameContainsWhiteList)
                        {
                            if(assemblyName.ToLower().Contains(name.ToLower()))
                            {
                                try
                                {
                                    ProcessAssembly(assembly, outputPath, ref count, ref totalCount);
                                }
                                catch { }
                                break;
                            }
                        }
                    }
                    else
                    {
                        bool skip = false;
                        foreach(string name in config.NameContainsBlacklist)
                        {
                            if(assemblyName.ToLower().Contains(name.ToLower()))
                            {
                                skip = true;
                                break;
                            }
                        }
                        if(!skip)
                        {
                            try
                            {
                                ProcessAssembly(assembly, outputPath, ref count, ref totalCount);
                            }
                            catch { }
                        }
                    }

                }
                if(count > 0)
                {
                    stringBuilder.AppendLine($"    Files output: {count}");
                    stringBuilder.AppendLine();
                }
            }

            if(totalCount > 0)
            {
                stringBuilder.AppendLine($"Total files output: {totalCount}");
                log.LogMessage(stringBuilder.ToString());
            }
        }

        private void ProcessAssembly(Assembly assembly, string outputPath, ref int count, ref int totalCount)
        {
            string assemblyPath = assembly.Location;
            string filename = assembly.GetName().Name;


            string lastHash = null;
            string curHash = ComputeHash(assemblyPath);

            string hashPath = Path.Combine(outputPath, $"{filename}.hash");

            if (File.Exists(hashPath))
            {
                lastHash = File.ReadAllText(hashPath);
            }

            if (curHash == lastHash)
            {
                return;
            }

            RewriteAssembly(assemblyPath).Write($"{Path.Combine(outputPath, filename)}.dll");
            File.WriteAllText(hashPath, curHash);

            count++;
            totalCount++;
        }

        private static string ComputeHash(string assemblyPath)
        {
            StringBuilder res = new StringBuilder();
            using (SHA1 hash = SHA1.Create())
            {
                using (FileStream file = File.Open(assemblyPath, FileMode.Open, FileAccess.Read))
                {
                    hash.ComputeHash(file);
                    file.Close();
                }

                foreach (byte b in hash.Hash)
                {
                    res.Append(b.ToString("X2"));
                }
            }

            return res.ToString();
        }

        private static ModuleDef RewriteAssembly(string assemblyPath)
        {
            ModuleDef assembly = ModuleDefMD.Load(assemblyPath);
            foreach (TypeDef type in assembly.GetTypes())
            {
                type.Attributes &= ~TypeAttributes.VisibilityMask;

                if (type.IsNested)
                {
                    type.Attributes |= TypeAttributes.NestedPublic;
                }
                else
                {
                    type.Attributes |= TypeAttributes.Public;
                }

                foreach (MethodDef method in type.Methods)
                {
                    method.Attributes &= ~MethodAttributes.MemberAccessMask;
                    method.Attributes |= MethodAttributes.Public;
                    method.Body = new dnlib.DotNet.Emit.CilBody();
                }

                List<string> eventNames = new List<string>();
                foreach (EventDef ev in type.Events)
                {
                    eventNames.Add(ev.Name);
                }

                foreach (FieldDef field in type.Fields)
                {
                    if (!eventNames.Contains(field.Name))
                    {
                        field.Attributes &= ~FieldAttributes.FieldAccessMask;
                        field.Attributes |= FieldAttributes.Public;
                    }
                }
            }
            return assembly;
        }
    }
}
