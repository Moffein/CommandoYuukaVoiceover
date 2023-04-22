using BepInEx;
using BepInEx.Configuration;
using CommandoYuukaVoiceover.Components;
using R2API;
using RoR2;
using RoR2.Audio;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CommandoYuukaVoiceover
{
    [BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Alicket.HayaseYuukaCommando")]
    [BepInDependency("com.bepis.r2api")]
    [BepInPlugin("com.Schale.CommandoYuukaVoiceover", "CommandoYuukaVoiceover", "1.0.0")]
    [R2API.Utils.R2APISubmoduleDependency(nameof(SoundAPI), nameof(ContentAddition))]
    public class CommandoYuukaVoiceover : BaseUnityPlugin
    {
        public static ConfigEntry<bool> enableVoicelines;
        private static SurvivorDef commandoSurvivorDef;

        public void Awake()
        {
            BaseVoiceoverComponent.Init();
            RoR2.RoR2Application.onLoad += OnLoad;

            using (var bankStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("CommandoYuukaVoiceover.CommandoYuukaSounds.bnk"))
            {
                var bytes = new byte[bankStream.Length];
                bankStream.Read(bytes, 0, bytes.Length);
                R2API.SoundAPI.SoundBanks.Add(bytes);
            }

            CommandoYuukaVoiceoverComponent.nseSpecial = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            CommandoYuukaVoiceoverComponent.nseSpecial.eventName = "Play_CommandoYuuka_CommonSkill";
            R2API.ContentAddition.AddNetworkSoundEventDef(CommandoYuukaVoiceoverComponent.nseSpecial);

            CommandoYuukaVoiceoverComponent.nseBlock = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            CommandoYuukaVoiceoverComponent.nseBlock.eventName = "Play_CommandoYuuka_Blocked";
            R2API.ContentAddition.AddNetworkSoundEventDef(CommandoYuukaVoiceoverComponent.nseBlock);

            enableVoicelines = base.Config.Bind<bool>(new ConfigDefinition("Settings", "Enable Voicelines"), true, new ConfigDescription("Enable voicelines when using the Commando Yuuka Skin."));
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions"))
            {
                RiskOfOptionsCompat();
            }

            commandoSurvivorDef = Addressables.LoadAssetAsync<SurvivorDef>("RoR2/Base/Commando/Commando.asset").WaitForCompletion();
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void RiskOfOptionsCompat()
        {
            RiskOfOptions.ModSettingsManager.AddOption(new RiskOfOptions.Options.CheckBoxOption(enableVoicelines));
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
                        //Check SkinDef
                        BodyIndex bodyIndexFromSurvivorIndex = SurvivorCatalog.GetBodyIndexFromSurvivorIndex(self.currentSurvivorDef.survivorIndex);
                        int skinIndex = (int)self.currentLoadout.bodyLoadoutManager.GetSkinIndex(bodyIndexFromSurvivorIndex);
                        SkinDef safe = HG.ArrayUtils.GetSafe<SkinDef>(BodyCatalog.GetBodySkins(bodyIndexFromSurvivorIndex), skinIndex);
                        if (CommandoYuukaVoiceoverComponent.requiredSkinDefs.Contains(safe))
                        {
                            bool flag = true;
                            if (flag)
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
            }

            CommandoYuukaVoiceoverComponent.ScepterIndex = ItemCatalog.FindItemIndex("ITEM_ANCIENT_SCEPTER");
        }
    }
}
