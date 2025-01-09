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
        public static bool turnOnFriendlyVillagers = true;
        public static ModConfig conf = null;

        public static void init(ModConfig theConf) {
            conf = theConf;
            harmony.Patch(
                AccessTools.Method(typeof(BaseSimObject), "canAttackTarget"),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(Patches), "canAttackTarget_Prefix"))
            );
            harmony.Patch(
                AccessTools.Method(typeof(City), "updateConquest"),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(Patches), "updateConquest_Prefix"))
            );
            harmony.Patch(
                AccessTools.Method(typeof(BehFindSameRaceActor), "execute"),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(Patches), "findSameRaceActor_Prefix"))
            );
            harmony.Patch(
                AccessTools.Method(typeof(BehJoinCity), "isPossibleToJoin"),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(Patches), "isPossibleToJoin_Prefix"))
            );
            harmony.Patch(
                AccessTools.Method(typeof(BehFindEmptyNearbyCity), "getEmptyCity"),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(Patches), "getEmptyCity_Prefix"))
            );
        }

        public static bool getEmptyCity_Prefix(WorldTile pFromTile, Race pRace, ref City __result)
        {
            BehaviourActionActor.temp_cities.Clear();
            foreach (City item in BehaviourActionBase<Actor>.world.cities.list)
            {
                if (item.getTile() != null && !(Toolbox.DistVec3(pFromTile.posV, item.cityCenter) > 200f) && item.status.population <= 40 && (item.status.population <= 5 || item.status.housingFree != 0) && item.getTile().isSameIsland(pFromTile))
                {
                    BehaviourActionActor.temp_cities.Add(item);
                }
            }
            City random = Toolbox.getRandom(BehaviourActionActor.temp_cities);
            BehaviourActionActor.temp_cities.Clear();
            __result = random;
            return false;
        }

        public static bool isPossibleToJoin_Prefix(Actor pActor, ref bool __result)
        {
            City city = pActor.currentTile.zone.city;
            if (city == null)
            {
                __result = false;
                return false;
            }
            if (city == pActor.city)
            {
                __result = false;
                return false;
            }
            if (city.kingdom != pActor.kingdom && !pActor.kingdom.isNomads())
            {
                __result = false;
                return false;
            }
            if (pActor.city != null)
            {
                if (pActor.isKing())
                {
                    __result = false;
                    return false;
                }
                if (pActor.isCityLeader())
                {
                    __result = false;
                    return false;
                }
                if (pActor.city.getPopulationTotal() < city.getPopulationTotal())
                {
                    __result = false;
                    return false;
                }
            }
            //WorldBoxConsole.Console.print("isPossibleToJoin for " + city.data.name + " resolved True for " + pActor.asset.race);
            __result = true;
            return false;
        }

        public static bool findSameRaceActor_Prefix(Actor pActor, ref BehResult __result)
        {
            Actor beh_actor_target = null;
            pActor.currentTile.region.island.actors.ShuffleOne();
            foreach (Actor actor in pActor.currentTile.region.island.actors)
            {
                if (!(actor == pActor) && actor.isAlive() && actor.currentTile.isSameIsland(pActor.currentTile) && !(actor.data.created_time > pActor.data.created_time))
                {
                    beh_actor_target = actor;
                }
            }
            pActor.beh_actor_target = beh_actor_target;
            if (pActor.beh_actor_target == null)
            {
                __result = BehResult.Stop;
                return false;
            }
            __result = BehResult.Continue;
            return false;
        }

        public static bool updateConquest_Prefix(Actor pActor, City __instance) {
            if (pActor.kingdom.isCiv() && (pActor.kingdom == __instance.kingdom || pActor.kingdom.isEnemy(__instance.kingdom)))
            {
                __instance.addCapturePoints(pActor, 1);
                return false;
            }
            return false;
        }

        public static bool canAttackTarget_Prefix(BaseSimObject pTarget, BaseSimObject __instance, ref bool __result)
        {

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
            bool flag = __instance.isActor();
            Race race;
            WeaponType weaponType;
            bool raceFlag = false;
            if (flag)
            {
                if (__instance.a.asset.skipFightLogic)
                {
                    __result = false;
                    return false;
                }
                race = __instance.a.race;
                weaponType = __instance.a.s_attackType;
            }
            else
            {
                race = __instance.b.kingdom.race;
                weaponType = WeaponType.Range;
            }
            if (pTarget.isActor())
            {
                Actor actor = pTarget.a;
                if (!actor.asset.canBeKilledByStuff)
                {
                    __result = false;
                    return false;
                }
                if (actor.isInsideSomething())
                {
                    __result = false;
                    return false;
                }
                if (actor.ai.action != null && actor.ai.action.special_prevent_can_be_attacked)
                {
                    __result = false;
                    return false;
                }
                if (actor.isInMagnet())
                {
                    __result = false;
                    return false;
                }
                if (flag && __instance.a.s_attackType == WeaponType.Melee && pTarget.zPosition.y > 0f)
                {
                    __result = false;
                    return false;
                }
                if (!actor.kingdom.asset.mad && !__instance.kingdom.asset.mad && !World.world.worldLaws.world_law_angry_civilians.boolVal)
                {
                    if ((actor.race == race || turnOnFriendlyVillagers) && actor.professionAsset.is_civilian)
                    {
                        __result = false;
                        return false;
                    }
                    if ((actor.race == race || turnOnFriendlyVillagers) && flag && (__instance.a.professionAsset.is_civilian))
                    {
                        __result = false;
                        return false;
                    }
                }
                if (actor.isFlying() && weaponType == WeaponType.Melee)
                {
                    __result = false;
                    return false;
                }
            }
            else
            {
                Building building = pTarget.b;
                if (__instance.kingdom.isCiv() && building.asset.cityBuilding && !building.asset.tower && flag && __instance.a.professionAsset.is_civilian && !World.world.worldLaws.world_law_angry_civilians.boolVal && (building.kingdom.race == __instance.kingdom.race || turnOnFriendlyVillagers))
                {
                    __result = false;
                    return false;
                }
                else if (__instance.kingdom.isNomads() && building.asset.cityBuilding)
                {
                    __result = false;
                    return false;
                }
                if (flag)
                {
                    __result = __instance.a.asset.canAttackBuildings || (__instance.a.asset.canAttackBrains && pTarget.kingdom.asset.brain);
                    return false;
                }
            }
            if (flag)
            {
                if (__instance.a.asset.oceanCreature && !__instance.a.asset.landCreature)
                {
                    if (!pTarget.isInLiquid())
                    {
                        __result = false;
                        return false;
                    }
                    if (!pTarget.currentTile.isSameIsland(__instance.currentTile))
                    {
                        __result = false;
                        return false;
                    }
                }
                else if (weaponType == WeaponType.Melee && pTarget.isInLiquid() && !__instance.a.asset.oceanCreature)
                {
                    __result = false;
                    return false;
                }
            }
            __result = true;
            return false;
        }
    }
}
