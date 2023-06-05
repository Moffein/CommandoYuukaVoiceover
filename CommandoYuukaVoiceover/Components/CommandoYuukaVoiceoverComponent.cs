using UnityEngine;
using RoR2;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace CommandoYuukaVoiceover.Components
{
    public class CommandoYuukaVoiceoverComponent : BaseVoiceoverComponent
    {
        public static List<SkinDef> requiredSkinDefs = new List<SkinDef>();

        public static NetworkSoundEventDef nseSpecial;
        public static NetworkSoundEventDef nseBlock;
        public static NetworkSoundEventDef nseShrineFail;

        public static ItemIndex ScepterIndex;

        private float lowHealthCooldown = 0f;
        private float blockedCooldown = 0f;
        private float specialCooldown = 0f;
        private float levelCooldown = 0f;
        private float shrineOfChanceFailCooldown = 0f;
        private bool acquiredScepter = false;

        protected override void Awake()
        {
            spawnVoicelineDelay = 3f;
            if (Run.instance && Run.instance.stageClearCount == 0)
            {
                spawnVoicelineDelay = 6.5f;
            }
            base.Awake();
        }

        protected override void Start()
        {
            base.Start();
            if (inventory && inventory.GetItemCount(ScepterIndex) > 0) acquiredScepter = true;
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

        public override void PlayPrimaryAuthority() { }

        public override void PlaySecondaryAuthority() { }

        public override void PlaySpawn()
        {
            //Use Title Drop in character select instead.
            /*if (Util.CheckRoll(1f))
            {
                TryPlaySound("Play_CommandoYuuka_TitleDrop", 1f, true);
            }
            else
            {
                TryPlaySound("Play_CommandoYuuka_Spawn", 5f, true);
            }*/
            TryPlaySound("Play_CommandoYuuka_Spawn", 5f, true);
        }

        public override void PlaySpecialAuthority()
        {
            if (specialCooldown > 0f) return;
            if (Util.CheckRoll(30f))
            {
                bool played = TryPlayNetworkSound(nseSpecial, 1.7f, false);
                if (played) specialCooldown = 10f;
            }
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

        public override void PlayUtilityAuthority()
        {
            TryPlaySound("Play_CommandoYuuka_Shout", 0f, false);
        }

        public override void PlayDamageBlockedServer()
        {
            if (!NetworkServer.active || blockedCooldown > 0f) return;
            //bool playedBlocked = TryPlaySound("Play_CommandoYuuka_Blocked", 0.75f, false);
            bool played = TryPlayNetworkSound(nseBlock, 0.75f, false);
            if (played) blockedCooldown = 10f;
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
            if (CommandoYuukaVoiceoverComponent.ScepterIndex != ItemIndex.None && itemIndex == CommandoYuukaVoiceoverComponent.ScepterIndex)
            {
                PlayAcquireScepter();
            } 
            else
            {
                PlayAcquireLegendary();
            }
        }

        public void PlayAcquireScepter()
        {
            if (acquiredScepter) return;
            TryPlaySound("Play_CommandoYuuka_AcquireScepter", 3.7f, false);
            acquiredScepter = true;
        }

        public void PlayAcquireLegendary()
        {
            TryPlaySound("Play_CommandoYuuka_FindLegendary", 5.75f, false);
        }

        public void PlayShrineOfChanceFailServer()
        {
            if (!NetworkServer.active || shrineOfChanceFailCooldown > 0f) return;
            if (Util.CheckRoll(15f))
            {
                bool played = TryPlayNetworkSound(nseShrineFail, 4.5f, false);
                if (played) shrineOfChanceFailCooldown = 60f;
            }
        }
    }
}
