using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using NeoModLoader.api;
using NeoModLoader.General;
using UnityEngine;
using ReflectionUtility;
using HarmonyLib;
using ai;
using ai.behaviours;

namespace FriendlyVillagers 
{
    class Patches : MonoBehaviour
    {
        public static Harmony harmony = new Harmony("cd.mymod.wb.friendlyvillagers");
        public static ModConfig conf = null;

        public static void init(ModConfig theConf) {
            conf = theConf;
            harmony.Patch(
                AccessTools.Method(typeof(City), "isWelcomedToJoin"),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(Patches), "isWelcomedToJoin_Prefix"))
            );
            harmony.Patch(
                AccessTools.Method(typeof(BaseSimObject), "canAttackTarget"),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(Patches), "canAttackTarget_Prefix"))
            );
            
            harmony.Patch(
                AccessTools.Method(typeof(Culture), "createCulture"),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(Patches), "createCulture_Postfix"))
            );
        }

        public static bool isWelcomedToJoin_Prefix(Actor pActor, City __instance, ref bool __result)
        {
            bool baseMod = (bool) conf["FV"]["baseMod"].GetValue();

            if (pActor.kingdom == __instance.kingdom)
            {
                __result = true;
                return false;
            }
            if (pActor.isSameSubspecies(__instance.getMainSubspecies()))
            {
                __result = true;
                return false;
            }
            if (!__instance.hasCulture())
            {
                __result = false;
                return false;
            }
            if (__instance.culture.hasTrait("xenophobic"))
            {
                __result = false;
                return false;
            }
            if (pActor.hasCultureTrait("xenophobic"))
            {
                __result = false;
                return false;
            }
            if (baseMod || __instance.culture.hasTrait("xenophiles"))
            {
                if (!pActor.hasCulture())
                {
                    __result = true;
                    return false;
                }
                if (baseMod || pActor.hasCultureTrait("xenophiles"))
                {
                    __result = true;
                    return false;
                }
            }
            if (__instance.isSameSpeciesAsActor(pActor) || baseMod)
            {
                __result = true;
                return false;
            }
            __result = false;
            return false;
        }

        public static bool canAttackTarget_Prefix(BaseSimObject pTarget, bool pCheckForFactions, bool pAttackBuildings, BaseSimObject __instance, ref bool __result)
        {
            bool baseMod = (bool) conf["FV"]["baseMod"].GetValue();
            if (!__instance.isAlive())
            {
                __result = false;
                return false;
            }
            if (!pTarget.isAlive())
            {
                __result = false;
                return false;
            }
            bool tThisIsActor = __instance.isActor();
            if (pTarget.isBuilding() && !pAttackBuildings)
            {
                if (!tThisIsActor || !__instance.a.asset.unit_zombie)
                {
                    __result = false;
                    return false;
                }
                if (!pTarget.kingdom.asset.brain)
                {
                    __result = false;
                    return false;
                }
            }
            string tSpeciesID;
            WeaponType tAttackType;
            if (tThisIsActor)
            {
                if (__instance.a.asset.skip_fight_logic)
                {
                    __result = false;
                    return false;
                }
                tSpeciesID = __instance.a.asset.id;
                tAttackType = __instance.a._attack_asset.attack_type;
            }
            else
            {
                tSpeciesID = __instance.b.kingdom.getSpecies();
                tAttackType = WeaponType.Range;
            }
            if (pTarget.isActor())
            {
                Actor tActorTarget = pTarget.a;
                if (!tActorTarget.asset.can_be_killed_by_stuff)
                {
                    __result = false;
                    return false;
                }
                if (tActorTarget.isInsideSomething())
                {
                    __result = false;
                    return false;
                }
                if (tActorTarget.isFlying() && tAttackType == WeaponType.Melee)
                {
                    __result = false;
                    return false;
                }
                if (tActorTarget.ai.action != null && tActorTarget.ai.action.special_prevent_can_be_attacked)
                {
                    __result = false;
                    return false;
                }
                if (tActorTarget.isInMagnet())
                {
                    __result = false;
                    return false;
                }
                if (pCheckForFactions && __instance.areFoes(pTarget) && tActorTarget.isKingdomCiv() && __instance.isKingdomCiv() && !__instance.hasStatusTantrum() && !tActorTarget.hasStatusTantrum())
                {
                    bool tXenophobicAny = (tThisIsActor && __instance.a.hasXenophobic()) || tActorTarget.hasXenophobic();
                    bool tXenophileAny = (tThisIsActor && __instance.a.hasXenophiles()) || tActorTarget.hasXenophiles();
                    bool tSameCulture = tThisIsActor && __instance.a.culture == tActorTarget.culture;
                    bool tSameSpecies = tSpeciesID == tActorTarget.asset.id;
                    bool tIgnoreCivilians = ((baseMod || tSameSpecies || tXenophileAny) && !tXenophobicAny) || (tSameCulture && tSameSpecies);

                    if (!WorldLawLibrary.world_law_angry_civilians.isEnabled()) {
                        if (tActorTarget.profession_asset.is_civilian && tIgnoreCivilians) {
                            __result = false;
                            return false;
                        }

                        if (tThisIsActor && __instance.a.profession_asset.is_civilian && tIgnoreCivilians) {
                            __result = false;
                            return false;
                        }
                    }
                }
                if (pCheckForFactions && tThisIsActor && __instance.a.hasCannibalism() && __instance.a.isSameSpecies(tActorTarget))
                {
                    Family tFamilyThis = __instance.a.family;
                    Family tFamilyTarget = tActorTarget.family;
                    if (tFamilyTarget == null || tFamilyThis == null)
                    {
                        __result = false;
                        return false;
                    }
                    if (__instance.a.hasFamily())
                    {
                        if (tFamilyTarget == tFamilyThis)
                        {
                            __result = false;
                            return false;
                        }
                        if (!tFamilyTarget.areMostUnitsHungry() && !tFamilyThis.areMostUnitsHungry())
                        {
                            __result = false;
                            return false;
                        }
                    }
                }
            }
            else
            {
                Building tBuildingTarget = pTarget.b;
                if (__instance.isKingdomCiv() && tBuildingTarget.asset.city_building && tBuildingTarget.asset.tower
                    && !tBuildingTarget.isCiv() && tThisIsActor && __instance.a.profession_asset.is_civilian && !WorldLawLibrary.world_law_angry_civilians.isEnabled()
                    && (baseMod || tBuildingTarget.kingdom.getSpecies() == __instance.kingdom.getSpecies()))
                {
                    __result = false;
                    return false;
                }
            }
            if (tThisIsActor)
            {
                ActorAsset tActorAsset = __instance.a.asset;
                if (!__instance.a.isWaterCreature() || !__instance.a.hasRangeAttack())
                {
                    if (__instance.a.isWaterCreature() && !tActorAsset.force_land_creature)
                    {
                        if (!pTarget.isInLiquid())
                        {
                            __result = false;
                            return false;
                        }
                        if (!pTarget.current_tile.isSameIsland(__instance.current_tile))
                        {
                            __result = false;
                            return false;
                        }
                    }
                    else if (tAttackType == WeaponType.Melee && pTarget.isInLiquid() && !__instance.a.isWaterCreature())
                    {
                        __result = false;
                        return false;
                    }
                }
            }
            __result = true;
            return false;
        }

        public static void createCulture_Postfix(Actor pActor, bool pAddDefaultTraits, Culture __instance) {
            bool preventXenophobe = (bool) conf["FV"]["preventXenophobe"].GetValue();

            if (preventXenophobe) {
                __instance.removeTrait("xenophobic");
            }
        }
    }
}
