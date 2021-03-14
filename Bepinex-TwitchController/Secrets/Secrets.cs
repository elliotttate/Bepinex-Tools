using BepInEx;
using LitJson;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace TwitchController
{
    public class Secrets
    {

        private static string _configFilePath = null;
        private static string ConfigFilePath => _configFilePath ?? (_configFilePath = Path.Combine(Paths.PluginPath, "HTH/TwitchConfig.json"));


        protected internal string client_id;
        protected internal string api_token;
        protected internal string refresh_token;
        protected internal string nick_id;
        protected internal string username;
        protected internal string botname;
        protected internal string regex;

        public Secrets(string customConfigPath = null)
        {
            _configFilePath = customConfigPath;

            if (!File.Exists(ConfigFilePath))
            {
                StringBuilder stringBuilder = new StringBuilder();
                JsonMapper.ToJson(new Config(), new JsonWriter(stringBuilder) { PrettyPrint = true });

                File.WriteAllText(ConfigFilePath, stringBuilder.ToString());
                Application.Quit();
            }

            Config config = JsonMapper.ToObject<Config>(File.ReadAllText(ConfigFilePath));
            client_id = config.ClientId;
            api_token = config.UsernameToken;
            refresh_token = config.UsernameRefreshToken;
            nick_id = config.UsernameId;
            username = config.Username;
            botname = config.BotName;
            regex = config.TipsRegEx;


            try
            {

            }
            catch { }
            StringBuilder stringBuilder2 = new StringBuilder();
            JsonMapper.ToJson(config, new JsonWriter(stringBuilder2) { PrettyPrint = true });
            File.WriteAllText(ConfigFilePath, stringBuilder2.ToString());

        }
    }
}