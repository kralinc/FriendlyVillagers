using System;
using System.Collections;
using System.Collections.Generic;
using NeoModLoader.api;
using UnityEngine;
using ReflectionUtility;

namespace FriendlyVillagers{
    public class ModClass : BasicMod<ModClass>
    {
        protected override void OnModLoad()
        {
            Patches.init(this.GetConfig());
            LogInfo("Friendly Villagers Mod Loaded");
        }
    }
}