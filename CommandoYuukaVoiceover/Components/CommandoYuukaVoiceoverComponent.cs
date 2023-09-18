using UnityEngine;
using RoR2;
using System.Collections.Generic;
using UnityEngine.Networking;
using BaseVoiceoverLib;

namespace CommandoYuukaVoiceover.Components
{
    public class CommandoYuukaVoiceoverComponent : BaseVoiceoverComponent
    {
        public static NetworkSoundEventDef nseShout, nseSpecial, nseBlock, nseShrineFail, nseTitle, nseIntro, nseHurt, nseKanpeki, nseSmart, nseLogic, nseFactor, nseThanks, nseIku, nseMathTruth;

        private float lowHealthCooldown = 0f;
        private float blockedCooldown = 0f;
        private float specialCooldown = 0f;
        private float levelCooldown = 0f;
        private float shrineOfChanceFailCooldown = 0f;
        private bool acquiredScepter = false;

        protected override void Start()
        {
            base.Start();
            if (inventory && inventory.GetItemCount(scepterIndex) > 0) acquiredScepter = true;
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (lowHealthCooldown > 0f) lowHealthCooldown -= Time.fixedDeltaTime;
            if (blockedCooldown > 0f) blockedCooldown -= Time.fixedDeltaTime;
            if (specialCooldown > 0f) specialCooldown -= Time.fixedDeltaTime;
            if (levelCooldown > 0f) levelCooldown -= Time.fixedDeltaTime;
            if (shrineOfChanceFailCooldown > 0f) levelCooldown -= Time.fixedDeltaTime;
        }

        public override void PlayDeath()
        {
            TryPlaySound("Play_CommandoYuuka_Defeat", 5f, true);
        }

        public override void PlayHurt(float percentHPLost)
        {
            if (percentHPLost >= 0.1f)
            {
                TryPlaySound("Play_CommandoYuuka_TakeDamage", 0f, false);
            }
        }

        public override void PlayJump() { }

        public override void PlayLowHealth()
        {
            if (lowHealthCooldown > 0f) return;
            bool playedLowHealth = TryPlaySound("Play_CommandoYuuka_LowHealth", 1.95f, false);
            if (playedLowHealth) lowHealthCooldown = 60f;
        }

        public override void PlaySpawn()
        {
            TryPlaySound("Play_CommandoYuuka_Spawn", 5f, true);
        }

        public override void PlaySpecialAuthority(GenericSkill skill)
        {
            if (specialCooldown > 0f) return;
            bool played = TryPlayNetworkSound(nseSpecial, 1.7f, false);
            if (played) specialCooldown = 20f;
        }

        public override void PlayTeleporterFinish()
        {
            TryPlaySound("Play_CommandoYuuka_Victory", 3.8f, false);
        }

        //This one is forced to play
        public override void PlayVictory()
        {
            TryPlaySound("Play_CommandoYuuka_Victory", 3.8f, true);
        }

        public override void PlayTeleporterStart()
        {
            TryPlaySound("Play_CommandoYuuka_TeleporterStart", 1.95f, false);
        }

        public override void PlayUtilityAuthority(GenericSkill skill)
        {
            TryPlayNetworkSound(nseShout, 0f, false);
        }

        public override void PlayDamageBlockedServer()
        {
            if (!NetworkServer.active || blockedCooldown > 0f) return;
            //bool playedBlocked = TryPlaySound("Play_CommandoYuuka_Blocked", 0.75f, false);
            bool played = TryPlayNetworkSound(nseBlock, 0.75f, false);
            if (played) blockedCooldown = 30f;
        }

        public override void PlayLevelUp()
        {
            if (levelCooldown > 0f) return;
            bool played = TryPlaySound("Play_CommandoYuuka_LevelUp", 7f, false);
            if (played) levelCooldown = 60f;
        }

        protected override void Inventory_onItemAddedClient(ItemIndex itemIndex)
        {
            base.Inventory_onItemAddedClient(itemIndex);
            if (scepterIndex != ItemIndex.None && itemIndex == scepterIndex)
            {
                PlayAcquireScepter();
            } 
            else
            {
                ItemDef id = ItemCatalog.GetItemDef(itemIndex);
                if (id && id.deprecatedTier == ItemTier.Tier3)
                {
                    PlayAcquireLegendary();
                }
            }
        }

        public void PlayAcquireScepter()
        {
            if (acquiredScepter) return;
            TryPlaySound("Play_CommandoYuuka_AcquireScepter", 3.7f, true);
            acquiredScepter = true;
        }

        public void PlayAcquireLegendary()
        {
            TryPlaySound("Play_CommandoYuuka_FindLegendary", 5.75f, false);
        }

        public override void PlayShrineOfChanceFailServer()
        {
            if (!NetworkServer.active || shrineOfChanceFailCooldown > 0f) return;
            if (Util.CheckRoll(15f))
            {
                bool played = TryPlayNetworkSound(nseShrineFail, 4.5f, false);
                if (played) shrineOfChanceFailCooldown = 60f;
            }
        }

        protected override void CheckInputs()
        {
            base.CheckInputs();
            if (BaseVoiceoverLib.Utils.GetKeyPressed(CommandoYuukaVoiceover.buttonTitle))
            {
                TryPlayNetworkSound(nseTitle, 0.8f, false);
                return;
            }
            if (BaseVoiceoverLib.Utils.GetKeyPressed(CommandoYuukaVoiceover.buttonIntro))
            {
                TryPlayNetworkSound(nseIntro, 7f, false);
                return;
            }
            if (BaseVoiceoverLib.Utils.GetKeyPressed(CommandoYuukaVoiceover.buttonKanpeki))
            {
                TryPlayNetworkSound(nseKanpeki, 2.3f, false);
                return;
            }
            if (BaseVoiceoverLib.Utils.GetKeyPressed(CommandoYuukaVoiceover.buttonSmart))
            {
                TryPlayNetworkSound(nseSmart, 1.6f, false);
                return;
            }
            if (BaseVoiceoverLib.Utils.GetKeyPressed(CommandoYuukaVoiceover.buttonLogic))
            {
                TryPlayNetworkSound(nseLogic, 1.6f, false);
                return;
            }
            if (BaseVoiceoverLib.Utils.GetKeyPressed(CommandoYuukaVoiceover.buttonFactor))
            {
                TryPlayNetworkSound(nseFactor, 3f, false);
                return;
            }
            if (BaseVoiceoverLib.Utils.GetKeyPressed(CommandoYuukaVoiceover.buttonMathTruth))
            {
                TryPlayNetworkSound(nseMathTruth, 3.4f, false);
                return;
            }
            if (BaseVoiceoverLib.Utils.GetKeyPressed(CommandoYuukaVoiceover.buttonMuda))
            {
                TryPlayNetworkSound(nseBlock, 0.5f, false);
                return;
            }
            if (BaseVoiceoverLib.Utils.GetKeyPressed(CommandoYuukaVoiceover.buttonThanks))
            {
                TryPlayNetworkSound(nseThanks, 0.8f, false);
                return;
            }
            if (BaseVoiceoverLib.Utils.GetKeyPressed(CommandoYuukaVoiceover.buttonIku))
            {
                TryPlayNetworkSound(nseIku, 0.6f, false);
                return;
            }
            if (BaseVoiceoverLib.Utils.GetKeyPressed(CommandoYuukaVoiceover.buttonHurt))
            {
                TryPlayNetworkSound(nseHurt, 0.1f, false);
                return;
            }
            if (BaseVoiceoverLib.Utils.GetKeyPressed(CommandoYuukaVoiceover.buttonShout))
            {
                TryPlayNetworkSound(nseShout, 0.1f, false);
                return;
            }
        }
    }
}
