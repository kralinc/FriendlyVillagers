using System;
using System.IO;
using System.Reflection;
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
                AccessTools.Method(typeof(BehFindLover), "checkIfPossibleLover"),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(Patches), "checkIfPossibleLover_Prefix"))
            );
            harmony.Patch(
                AccessTools.Method(typeof(Actor), "canFallInLoveWith"),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(Patches), "canFallInLoveWith_Prefix"))
            );
            harmony.Patch(
                AccessTools.Method(typeof(City), "updateConquest"),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(Patches), "updateConquest_Prefix"))
            );
            /*harmony.Patch(
                AccessTools.Method(typeof(City), "addZone"),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(Patches), "addZone_Prefix"))
            );*/
            harmony.Patch(
                AccessTools.Method(typeof(BaseSimObject), "canAttackTarget"),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(Patches), "canAttackTarget_Prefix"))
            );
        }

        public static bool isWelcomedToJoin_Prefix(Actor pActor, City __instance, ref bool __result)
        {
            bool allowSubspecies = (bool) conf["FV"]["allowSubspecies"].GetValue();
            bool allowSpecies = (bool) conf["FV"]["allowSpecies"].GetValue();
            bool allowCulture = (bool) conf["FV"]["allowCultures"].GetValue();

            if (pActor.kingdom == __instance.kingdom)
            {
                __result = true;
                return false;
            }
            if (allowSubspecies || pActor.isSameSubspecies(__instance.getMainSubspecies()))
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
            if (allowCulture || __instance.culture.hasTrait("xenophiles"))
            {
                if (!pActor.hasCulture())
                {
                    __result = true;
                    return false;
                }
                if (pActor.hasCultureTrait("xenophiles"))
                {
                    __result = true;
                    return false;
                }
            }
            if (allowSpecies || __instance.isSameSpeciesAsActor(pActor))
            {
                __result = true;
                return false;
            }
            __result = false;
            return false;
        }

        public static bool checkIfPossibleLover_Prefix(Actor pActor, Actor pTarget, ref bool __result)
        {
            bool allowSubspecies = (bool) conf["FV"]["allowSubspecies"].GetValue();
            bool allowSpecies = (bool) conf["FV"]["allowSpecies"].GetValue();


            if (pTarget == pActor)
            {
                __result = false;
                return false;
            }
            if (!(allowSubspecies || allowSpecies) && !pTarget.hasSubspecies())
            {
                __result = false;
                return false;
            }
            if (!pTarget.isAlive())
            {
                __result = false;
                return false;
            }
            if (!pTarget.canFallInLoveWith(pActor))
            {
                __result = false;
                return false;
            }
            __result = true;
            return false;
        }

        public static bool canFallInLoveWith_Prefix(Actor pTarget, Actor __instance, ref bool __result) {
            bool allowRelationships = (bool) conf["FV"]["allowRelationships"].GetValue();

            if (__instance.hasLover())
            {
                __result = false;
                return false;
            }
            if (!__instance.isAdult())
            {
                __result = false;
                return false;
            }
            if (!__instance.isBreedingAge())
            {
                __result = false;
                return false;
            }
            if (!__instance.subspecies.needs_mate)
            {
                __result = false;
                return false;
            }
            if (!allowRelationships && !__instance.isSameSpecies(pTarget))
            {
                __result = false;
                return false;
            }
            if (!allowRelationships && !__instance.isSameSubspecies(pTarget.subspecies))
            {
                __result = false;
                return false;
            }
            if (!__instance.isSameSpecies(pTarget) && ((!pTarget.isSapient() && !__instance.isSapient()) || (pTarget.isSapient() != __instance.isSapient())))
            {
                __result = false;
                return false;
            }
            if (!__instance.subspecies.isPartnerSuitableForReproduction(__instance, pTarget))
            {
                __result = false;
                return false;
            }
            if (pTarget.hasLover())
            {
                __result = false;
                return false;
            }
            if (!pTarget.isAdult())
            {
                __result = false;
                return false;
            }
            if (!pTarget.isBreedingAge())
            {
                __result = false;
                return false;
            }
            if (__instance.isSapient() && __instance.hasFamily())
            {
                __instance.isRelatedTo(pTarget);
                __result = true;
                return false;
            }
            __result = true;
            return false;
        }

        public static bool updateConquest_Prefix(Actor pActor, City __instance) {
            bool allowSpecies = (bool) conf["FV"]["allowSpecies"].GetValue();
            if (!allowSpecies) {
                return true;
            }
            if (pActor.isKingdomCiv() && (allowSpecies || !(pActor.kingdom.getSpecies() != __instance.kingdom.getSpecies())) && (pActor.kingdom == __instance.kingdom || pActor.kingdom.isEnemy(__instance.kingdom)))
            {
                __instance.addCapturePoints(pActor, 1);
            }
            return false;
        }

        public static bool canAttackTarget_Prefix(BaseSimObject pTarget, bool pCheckForFactions, BaseSimObject __instance, ref bool __result)
        {
            bool allowSubspecies = (bool) conf["FV"]["allowSubspecies"].GetValue();
            bool allowSpecies = (bool) conf["FV"]["allowSpecies"].GetValue();
            bool allowCulture = (bool) conf["FV"]["allowCultures"].GetValue();

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
            string tSpeciesID = string.Empty;
            bool tThisIsActor = __instance.isActor();
            WeaponType tAttackType;
            if (tThisIsActor)
            {
                if (__instance.a.asset.skip_fight_logic)
                {
                    __result = false;
                    return false;
                }
                tSpeciesID = __instance.a.asset.id;
                tAttackType = __instance.a.s_type_attack;
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
                if (pCheckForFactions && __instance.areFoes(pTarget) && tActorTarget.isKingdomCiv() && __instance.isKingdomCiv() && !__instance.hasStatusTantrum() && !tActorTarget.hasStatusTantrum() && !WorldLawLibrary.world_law_angry_civilians.isEnabled())
                {
                    if ((allowSpecies || tActorTarget.asset.id == tSpeciesID) && tActorTarget.profession_asset.is_civilian)
                    {
                        __result = false;
                        return false;
                    }
                    if ((allowSpecies || tActorTarget.asset.id == tSpeciesID) && tThisIsActor && __instance.a.profession_asset.is_civilian)
                    {
                        __result = false;
                        return false;
                    }
                }
                if (pCheckForFactions && tThisIsActor && __instance.kingdom.asset.attack_each_other_when_hungry && (allowSpecies || __instance.a.isSameSpecies(tActorTarget)))
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
                if (__instance.isKingdomCiv() && tBuildingTarget.asset.city_building && tBuildingTarget.asset.tower && !tBuildingTarget.isCiv() && tThisIsActor && __instance.a.profession_asset.is_civilian && !WorldLawLibrary.world_law_angry_civilians.isEnabled() && (allowSpecies || tBuildingTarget.kingdom.getSpecies() == __instance.kingdom.getSpecies()))
                {
                    __result = false;
                    return false;
                }
                if (tThisIsActor)
                {
                    if (__instance.a.asset.can_attack_buildings)
                    {
                        __result = true;
                        return false;
                    }
                    if (__instance.a.asset.can_attack_brains && pTarget.kingdom.asset.brain)
                    {
                        __result = true;
                        return false;
                    }
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
/*
        public static bool addZone_Prefix(TileZone pZone, City __instance) {
            bool modEnabled = (bool) conf["FV"]["enableMod"].GetValue();
            if (!modEnabled) {
                return true;
            }
            if (__instance.zones.Contains(pZone))
            {
                __result = false;
                return false;
            }
            if (pZone.city != null)
            {
                pZone.city.removeZone(pZone);
            }
            __instance.zones.Add(pZone);
            pZone.setCity(__instance);
            __instance.updateCityCenter();
            if (World.world.city_zone_helper.city_place_finder.hasPossibleZones())
            {
                World.world.city_zone_helper.city_place_finder.setDirty();
            }
            __instance.setStatusDirty();
            Toolbox.temp_list_buildings_2.Clear();
            Toolbox.temp_list_buildings_2.AddRange(pZone.abandoned);
            for (int i = 0; i < Toolbox.temp_list_buildings_2.Count; i++)
            {
                Building building = Toolbox.temp_list_buildings_2[i];
                if (building.asset.cityBuilding && building.data.state == BuildingState.CivAbandoned)
                {
                    __instance.addBuilding(building);
                    building.retake();
                }
            }

            return false;
        }
    */
    }
}
