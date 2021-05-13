namespace Bepinex_Publicizer
{
    using System;
    using System.Collections.Generic;

    [Serializable]
    public class Config
    {
        public bool UseWhiteList = true;
        public List<string> OutputFolders = new List<string>();
        public List<string> NameContainsWhiteList = new List<string>() { "Assembly" };
        public List<string> NameContainsBlacklist = new List<string>() { "Unity", "BepInEx", "System", "Mono", "Harmony", "Newtonsoft", "LitJSON", "dnlib" };
    }
}
