using BepInEx;
using BepInEx.Logging;
using dnlib.DotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using FieldAttributes = dnlib.DotNet.FieldAttributes;
using MethodAttributes = dnlib.DotNet.MethodAttributes;
using TypeAttributes = dnlib.DotNet.TypeAttributes;

namespace Bepinex_Publicizer
{
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

        #endregion

        public Main()
        {
            log = Logger;
        }

        public void Awake()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach(Assembly assembly in assemblies)
				if (Path.GetDirectoryName(assembly.Location).Contains("Managed") && assembly.GetName().Name.ToLowerInvariant().StartsWith("assembly"))
					ProcessAssembly(assembly);
		}

        void ProcessAssembly(Assembly assembly)
        {
			string assemblyPath = assembly.Location;
            string filename = assembly.GetName().Name;
			string outputPath = Path.Combine(Path.GetDirectoryName(assemblyPath), "publicized_assemblies");
			string outputSuffix = "_publicized";

			Directory.CreateDirectory(outputPath);

			string lastHash = null;
            string curHash = ComputeHash(assemblyPath);

            string hashPath = Path.Combine(outputPath, $"{filename}{outputSuffix}.hash");

            if (File.Exists(hashPath))
                lastHash = File.ReadAllText(hashPath);

            if (curHash == lastHash)
				return;

			log.LogMessage($"Making a public assembly from {filename}");
            RewriteAssembly(assemblyPath).Write($"{Path.Combine(outputPath, filename)}{outputSuffix}.dll");
            File.WriteAllText(hashPath, curHash);
		}

		static string ComputeHash(string assemblyPath)
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
					res.Append(b.ToString("X2"));
			}

			return res.ToString();
		}

		static ModuleDef RewriteAssembly(string assemblyPath)
		{
			ModuleDef assembly = ModuleDefMD.Load(assemblyPath);
			foreach (var type in assembly.GetTypes())
			{
				type.Attributes &= ~TypeAttributes.VisibilityMask;

				if (type.IsNested)
					type.Attributes |= TypeAttributes.NestedPublic;
				else
					type.Attributes |= TypeAttributes.Public;

				foreach (MethodDef method in type.Methods)
				{
					method.Attributes &= ~MethodAttributes.MemberAccessMask;
					method.Attributes |= MethodAttributes.Public;
				}

				List<string> eventNames = new List<string>();
				foreach (EventDef ev in type.Events)
					eventNames.Add(ev.Name);

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
