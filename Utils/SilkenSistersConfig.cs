using BepInEx.Configuration;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SilkenSisters.Utils
{
    internal class SilkenSistersConfig
    {

        internal ConfigEntry<KeyCode> modifierKey;
        internal ConfigEntry<KeyCode> actionKey;
        
        internal ConfigEntry<float> syncWaitTime;
        internal ConfigEntry<float> syncDelay;
        internal ConfigEntry<float> syncGatherDistance;
        internal ConfigEntry<float> syncTeleDistance;
        internal ConfigEntry<float> syncRangeDistance;
        
        internal ConfigEntry<int> MaxHP;
        internal ConfigEntry<int> P2HP;
        internal ConfigEntry<int> P3HP;
        internal ConfigEntry<int> ParryCooldown;
        
        internal ConfigEntry<float> ParryBaitDistance;
        internal ConfigEntry<float> DefenseParryDistance;
        
        public ConfigEntry<bool> syncedFight;

        internal void BindConfig(ConfigFile Config)
        {
            modifierKey = Config.Bind(
                "Keybinds",
                "Modifier",
                KeyCode.LeftAlt,
                "Modifier"
            );

            syncedFight = Config.Bind(
                "General",
                "SyncedFight",
                false,
                "Use the Synced patterns for the boss fights. Playtest version"
            );

            syncWaitTime = Config.Bind(
                "Sync fight",
                "Idle time",
                1.5f,
                "Debug config for defining how long they will wait for each other to finish their actions"
            );

            syncDelay = Config.Bind(
                "Sync fight",
                "Delay time",
                0.5f,
                "Debug config for defining how the anti-synchronous actions will be delayed"
            );

            syncGatherDistance = Config.Bind(
                "Sync fight",
                "Gather Distance",
                1.75f,
                "Debug config for defining how close lace and phantom must be for attacking"
            );

            syncTeleDistance = Config.Bind(
                "Sync fight",
                "Tele Distance",
                8f,
                "Debug Config that defines how far lace and phantom must be for teleportation move."
            );

            syncRangeDistance = Config.Bind(
                "Sync fight",
                "Range Distance",
                6f,
                "Debug Config that defines the checking distance between the siblings and hornet."
            );

            MaxHP = Config.Bind(
                "Sync fight",
                "Max HP",
                1500,
                "Debug Config that defines max pooled HP."
            );

            P2HP = Config.Bind(
                "Sync fight",
                "P2 HP",
                1100,
                "Debug Config that defines pooled hp p2 shift."
            );

            P3HP = Config.Bind(
                "Sync fight",
                "P3 HP",
                600,
                "Debug Config that defines pooled hp p3 shift."
            );

            ParryCooldown = Config.Bind(
                "Sync fight",
                "Parry CoolDown",
                3,
                "Debug Config that defines the number of attacks between each parry attempt."
            );

            ParryBaitDistance = Config.Bind(
                "Sync fight",
                "Parry Bait Distance",
                3f,
                "Debug Config that defines the distance at which they tp for parrybait."
            );

            DefenseParryDistance = Config.Bind(
                "Sync fight",
                "Defense Parry Distance",
                2f,
                "Debug Config that defines the distance at which they tp for defend parry."
            );

        }


    }
}
