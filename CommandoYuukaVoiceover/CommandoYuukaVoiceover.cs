using BaseVoiceoverLib;
using BepInEx;
using BepInEx.Configuration;
using CommandoYuukaVoiceover.Components;
using CommandoYuukaVoiceover.Modules;
using RoR2;
using RoR2.Audio;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace CommandoYuukaVoiceover
{
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Moffein.BaseVoiceoverLib", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.Alicket.HayaseYuukaCommando", BepInDependency.DependencyFlags.HardDependency)]
    [BepInPlugin("com.Schale.CommandoYuukaVoiceover", "CommandoYuukaVoiceover", "1.3.0")]
    public class CommandoYuukaVoiceover : BaseUnityPlugin
    {
        public static ConfigEntry<KeyboardShortcut> buttonTitle, buttonIntro, buttonHurt, buttonKanpeki, buttonSmart, buttonLogic, buttonFactor, buttonMuda, buttonThanks, buttonIku, buttonMathTruth, buttonShout;
        public static ConfigEntry<bool> enableVoicelines;

        public static bool playedSeasonalVoiceline = false;
        public static AssetBundle assetBundle;

        public void Awake()
        {
            Files.PluginInfo = this.Info;
            RoR2.RoR2Application.onLoad += OnLoad;
            new Content().Initialize();

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CommandoYuukaVoiceover.commandoyuukavoiceoverbundle"))
            {
                assetBundle = AssetBundle.LoadFromStream(stream);
            }

            InitNSE();

            enableVoicelines = base.Config.Bind<bool>(new ConfigDefinition("Settings", "Enable Voicelines"), true, new ConfigDescription("Enable voicelines when using the Commando Yuuka Skin."));
            enableVoicelines.SettingChanged += EnableVoicelines_SettingChanged;

            buttonTitle = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Blue Archive"), KeyboardShortcut.Empty);
            buttonIntro = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Introduction"), KeyboardShortcut.Empty);
            buttonKanpeki = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Kanpeki"), KeyboardShortcut.Empty);
            buttonSmart = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Smart"), KeyboardShortcut.Empty);
            buttonLogic = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Logic and Reason"), KeyboardShortcut.Empty);
            buttonFactor = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Factorize"), KeyboardShortcut.Empty);
            buttonMathTruth = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Math leads to truth"), KeyboardShortcut.Empty);
            buttonThanks = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Thanks"), KeyboardShortcut.Empty);
            buttonMuda = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Muda"), KeyboardShortcut.Empty);
            buttonIku = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Lets go"), KeyboardShortcut.Empty);
            buttonHurt = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Hurt"), KeyboardShortcut.Empty);
            buttonShout = base.Config.Bind<KeyboardShortcut>(new ConfigDefinition("Keybinds", "Shout"), KeyboardShortcut.Empty);

            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions"))
            {
                RiskOfOptionsCompat();
            }
        }

        private void EnableVoicelines_SettingChanged(object sender, EventArgs e)
        {
            RefreshNSE();
        }

        private void Start()
        {
            SoundBanks.Init();
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void RiskOfOptionsCompat()
        {
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.CheckBoxOption(enableVoicelines));
            RiskOfOptions.ModSettingsManager.SetModIcon(assetBundle.LoadAsset<Sprite>("flyingYuuka"));

            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonTitle));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonIntro));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonKanpeki));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonSmart));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonLogic));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonFactor));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonMathTruth));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonThanks));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonMuda));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonIku));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonHurt));
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.KeyBindOption(buttonShout));
        }

        private void OnLoad()
        {
            SkinDef yuukaSkin = null;
            SkinDef[] commandoSkins = SkinCatalog.FindSkinsForBody(BodyCatalog.FindBodyIndex("CommandoBody"));
            foreach (SkinDef skinDef in commandoSkins)
            {
                //nameToken is "ALICKET_SKIN_BAHYCSKINDEFINITION_NAME"
                if (skinDef.name == "BAHYCSkinDefinition")
                {
                    yuukaSkin = skinDef;
                    break;
                }
            }

            if (!yuukaSkin)
            {
                Debug.LogError("CommandoYuukaVoiceover: Commando Yuuka SkinDef not found. Voicelines will not work!");
            }
            else
            {
                VoiceoverInfo vo = new VoiceoverInfo(typeof(CommandoYuukaVoiceoverComponent), yuukaSkin, "CommandoBody");
                vo.selectActions += CommandoSelect;
            }

            RefreshNSE();
        }

        private void CommandoSelect(GameObject mannequinObject)
        {
            if (!enableVoicelines.Value) return;

            bool played = false;
            if (!playedSeasonalVoiceline)
            {
                if (System.DateTime.Today.Month == 1 && System.DateTime.Today.Day == 1)
                {
                    Util.PlaySound("Play_CommandoYuuka_Lobby_Newyear", mannequinObject);
                    played = true;
                }
                else if (System.DateTime.Today.Month == 3 && System.DateTime.Today.Day == 14)
                {
                    Util.PlaySound("Play_CommandoYuuka_Lobby_bday", mannequinObject);
                    played = true;
                }
                else if (System.DateTime.Today.Month == 10 && System.DateTime.Today.Day == 31)
                {
                    Util.PlaySound("Play_CommandoYuuka_Lobby_Halloween", mannequinObject);
                    played = true;
                }
                else if (System.DateTime.Today.Month == 12 && System.DateTime.Today.Day == 25)
                {
                    Util.PlaySound("Play_CommandoYuuka_Lobby_xmas", mannequinObject);
                    played = true;
                }

                if (played)
                {
                    playedSeasonalVoiceline = true;
                }
            }
            if (!played)
            {
                if (Util.CheckRoll(5f))
                {
                    Util.PlaySound("Play_CommandoYuuka_TitleDrop", mannequinObject);
                }
                else
                {
                    Util.PlaySound("Play_CommandoYuuka_Lobby", mannequinObject);
                }
            }
        }

        private void InitNSE()
        {
            CommandoYuukaVoiceoverComponent.nseSpecial = RegisterNSE("Play_CommandoYuuka_CommonSkill");
            CommandoYuukaVoiceoverComponent.nseBlock = RegisterNSE("Play_CommandoYuuka_Blocked");
            CommandoYuukaVoiceoverComponent.nseShrineFail = RegisterNSE("Play_CommandoYuuka_ShrineFail");
            CommandoYuukaVoiceoverComponent.nseShout = RegisterNSE("Play_CommandoYuuka_Shout");
            CommandoYuukaVoiceoverComponent.nseTitle = RegisterNSE("Play_CommandoYuuka_TitleDrop");
            CommandoYuukaVoiceoverComponent.nseIntro = RegisterNSE("Play_CommandoYuuka_Intro");
            CommandoYuukaVoiceoverComponent.nseHurt = RegisterNSE("Play_CommandoYuuka_TakeDamage");
            CommandoYuukaVoiceoverComponent.nseKanpeki = RegisterNSE("Play_CommandoYuuka_Kanpeki");
            CommandoYuukaVoiceoverComponent.nseSmart = RegisterNSE("Play_CommandoYuuka_Smart");
            CommandoYuukaVoiceoverComponent.nseLogic = RegisterNSE("Play_CommandoYuuka_Logic");
            CommandoYuukaVoiceoverComponent.nseFactor = RegisterNSE("Play_CommandoYuuka_Factor");
            CommandoYuukaVoiceoverComponent.nseThanks = RegisterNSE("Play_CommandoYuuka_Thanks");
            CommandoYuukaVoiceoverComponent.nseIku = RegisterNSE("Play_CommandoYuuka_Ikuwayo");
            CommandoYuukaVoiceoverComponent.nseMathTruth = RegisterNSE("Play_CommandoYuuka_MathTruth");
        }

        private NetworkSoundEventDef RegisterNSE(string eventName)
        {
            NetworkSoundEventDef nse = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            nse.eventName = eventName;
            Content.networkSoundEventDefs.Add(nse);
            nseList.Add(new NSEInfo(nse));
            return nse;
        }

        public void RefreshNSE()
        {
            foreach (NSEInfo nse in nseList)
            {
                nse.ValidateParams();
            }
        }

        public static List<NSEInfo> nseList = new List<NSEInfo>();
        public class NSEInfo
        {
            public NetworkSoundEventDef nse;
            public uint akId = 0u;
            public string eventName = string.Empty;

            public NSEInfo(NetworkSoundEventDef source)
            {
                this.nse = source;
                this.akId = source.akId;
                this.eventName = source.eventName;
            }

            private void DisableSound()
            {
                nse.akId = 0u;
                nse.eventName = string.Empty;
            }

            private void EnableSound()
            {
                nse.akId = this.akId;
                nse.eventName = this.eventName;
            }

            public void ValidateParams()
            {
                if (this.akId == 0u) this.akId = nse.akId;
                if (this.eventName == string.Empty) this.eventName = nse.eventName;

                if (!enableVoicelines.Value)
                {
                    DisableSound();
                }
                else
                {
                    EnableSound();
                }
            }
        }
    }
}
