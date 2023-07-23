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
    [BepInDependency("com.Alicket.HayaseYuukaCommando")]
    [BepInPlugin("com.Schale.CommandoYuukaVoiceover", "CommandoYuukaVoiceover", "1.2.5")]
    public class CommandoYuukaVoiceover : BaseUnityPlugin
    {
        public static ConfigEntry<bool> enableVoicelines;
        private static SurvivorDef commandoSurvivorDef;

        public static bool playedSeasonalVoiceline = false;
        public static AssetBundle assetBundle;

        public void Awake()
        {
            Files.PluginInfo = this.Info;
            BaseVoiceoverComponent.Init();
            RoR2.RoR2Application.onLoad += OnLoad;
            new Content().Initialize();

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CommandoYuukaVoiceover.commandoyuukavoiceoverbundle"))
            {
                assetBundle = AssetBundle.LoadFromStream(stream);
            }

            CommandoYuukaVoiceoverComponent.nseSpecial = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            CommandoYuukaVoiceoverComponent.nseSpecial.eventName = "Play_CommandoYuuka_CommonSkill";
            Content.networkSoundEventDefs.Add(CommandoYuukaVoiceoverComponent.nseSpecial);

            CommandoYuukaVoiceoverComponent.nseBlock = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            CommandoYuukaVoiceoverComponent.nseBlock.eventName = "Play_CommandoYuuka_Blocked";
            Content.networkSoundEventDefs.Add(CommandoYuukaVoiceoverComponent.nseBlock);

            CommandoYuukaVoiceoverComponent.nseShrineFail = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            CommandoYuukaVoiceoverComponent.nseShrineFail.eventName = "Play_CommandoYuuka_ShrineFail";
            Content.networkSoundEventDefs.Add(CommandoYuukaVoiceoverComponent.nseShrineFail);

            CommandoYuukaVoiceoverComponent.nseShout = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            CommandoYuukaVoiceoverComponent.nseShout.eventName = "Play_CommandoYuuka_Shout";
            Content.networkSoundEventDefs.Add(CommandoYuukaVoiceoverComponent.nseShout);

            enableVoicelines = base.Config.Bind<bool>(new ConfigDefinition("Settings", "Enable Voicelines"), true, new ConfigDescription("Enable voicelines when using the Commando Yuuka Skin."));
            enableVoicelines.SettingChanged += EnableVoicelines_SettingChanged;

            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions"))
            {
                RiskOfOptionsCompat();
            }

            commandoSurvivorDef = Addressables.LoadAssetAsync<SurvivorDef>("RoR2/Base/Commando/Commando.asset").WaitForCompletion();
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
        }

        private void AttachVoiceoverComponent(On.RoR2.CharacterBody.orig_Start orig, CharacterBody self)
        {
            orig(self);
            if (self)// && self.bodyIndex == BodyCatalog.FindBodyIndex("CommandoBody")  //Not needed since we can just check skinIndex
            {
                if (CommandoYuukaVoiceoverComponent.requiredSkinDefs.Contains(SkinCatalog.GetBodySkinDef(self.bodyIndex, (int)self.skinIndex)))
                {
                    BaseVoiceoverComponent existingVoiceoverComponent = self.GetComponent<BaseVoiceoverComponent>();
                    if (!existingVoiceoverComponent) self.gameObject.AddComponent<CommandoYuukaVoiceoverComponent>();
                }
            }
        }

        private void OnLoad()
        {
            bool foundSkin = false;
            SkinDef[] commandoSkins = SkinCatalog.FindSkinsForBody(BodyCatalog.FindBodyIndex("CommandoBody"));
            foreach (SkinDef skinDef in commandoSkins)
            {
                //nameToken is "ALICKET_SKIN_BAHYCSKINDEFINITION_NAME"
                if (skinDef.name == "BAHYCSkinDefinition")
                {
                    foundSkin = true;
                    CommandoYuukaVoiceoverComponent.requiredSkinDefs.Add(skinDef);
                    break;
                }
            }

            if (!foundSkin)
            {
                Debug.LogError("CommandoYuukaVoiceover: Commando Yuuka SkinDef not found. Voicelines will not work!");
            }
            else
            {
                On.RoR2.CharacterBody.Start += AttachVoiceoverComponent;

                On.RoR2.SurvivorMannequins.SurvivorMannequinSlotController.RebuildMannequinInstance += (orig, self) =>
                {
                    orig(self);
                    if (self.currentSurvivorDef == commandoSurvivorDef)
                    {
                        //Loadout isn't loaded first time this is called, so we need to manually get it.
                        //Probably not the most elegant way to resolve this.
                        if (self.loadoutDirty)
                        {
                            if (self.networkUser)
                            {
                                self.networkUser.networkLoadout.CopyLoadout(self.currentLoadout);
                            }
                        }

                        //Check SkinDef
                        BodyIndex bodyIndexFromSurvivorIndex = SurvivorCatalog.GetBodyIndexFromSurvivorIndex(self.currentSurvivorDef.survivorIndex);
                        int skinIndex = (int)self.currentLoadout.bodyLoadoutManager.GetSkinIndex(bodyIndexFromSurvivorIndex);
                        SkinDef safe = HG.ArrayUtils.GetSafe<SkinDef>(BodyCatalog.GetBodySkins(bodyIndexFromSurvivorIndex), skinIndex);
                        if (CommandoYuukaVoiceoverComponent.requiredSkinDefs.Contains(safe) && enableVoicelines.Value)
                        {
                            bool played = false;
                            if (!playedSeasonalVoiceline)
                            {
                                if (System.DateTime.Today.Month == 1 && System.DateTime.Today.Day == 1)
                                {
                                    Util.PlaySound("Play_CommandoYuuka_Lobby_Newyear", self.mannequinInstanceTransform.gameObject);
                                    played = true;
                                }
                                else if (System.DateTime.Today.Month == 3 && System.DateTime.Today.Day == 14)
                                {
                                    Util.PlaySound("Play_CommandoYuuka_Lobby_bday", self.mannequinInstanceTransform.gameObject);
                                    played = true;
                                }
                                else if (System.DateTime.Today.Month == 10 && System.DateTime.Today.Day == 31)
                                {
                                    Util.PlaySound("Play_CommandoYuuka_Lobby_Halloween", self.mannequinInstanceTransform.gameObject);
                                    played = true;
                                }
                                else if (System.DateTime.Today.Month == 12 && System.DateTime.Today.Day == 25)
                                {
                                    Util.PlaySound("Play_CommandoYuuka_Lobby_xmas", self.mannequinInstanceTransform.gameObject);
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
                                    Util.PlaySound("Play_CommandoYuuka_TitleDrop", self.mannequinInstanceTransform.gameObject);
                                }
                                else
                                {
                                    Util.PlaySound("Play_CommandoYuuka_Lobby", self.mannequinInstanceTransform.gameObject);
                                }
                            }
                        }
                    }
                };

                On.RoR2.ShrineChanceBehavior.AddShrineStack += (orig, self, activator) =>
                {
                    int successes = self.successfulPurchaseCount;
                    orig(self, activator);

                    //No change in successes = fail
                    if (NetworkServer.active && self.successfulPurchaseCount == successes)
                    {
                        if (activator)
                        {
                            CommandoYuukaVoiceoverComponent vo = activator.GetComponent<CommandoYuukaVoiceoverComponent>();
                            if (vo)
                            {
                                vo.PlayShrineOfChanceFailServer();
                            }
                        }
                    }
                };
            }

            CommandoYuukaVoiceoverComponent.ScepterIndex = ItemCatalog.FindItemIndex("ITEM_ANCIENT_SCEPTER");

            //Do this in OnLoad since the NSE have been fully loaded by this point.
            nseList.Add(new NSEInfo(CommandoYuukaVoiceoverComponent.nseBlock));
            nseList.Add(new NSEInfo(CommandoYuukaVoiceoverComponent.nseShout));
            nseList.Add(new NSEInfo(CommandoYuukaVoiceoverComponent.nseShrineFail));
            nseList.Add(new NSEInfo(CommandoYuukaVoiceoverComponent.nseSpecial));
            RefreshNSE();
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
