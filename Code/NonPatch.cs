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
    class NonPatch : MonoBehaviour
    {

        public static ModConfig conf = null;
        public static void init(ModConfig theConf) {
            conf = theConf;

            initXenoLoyalty();
        }

        public static void initXenoLoyalty() {
            bool baseMod = (bool) conf["FV"]["baseMod"].GetValue();

            //Modify existing species loyalty method to neutralize loyalty if culture has xenophile trait.
            LoyaltyAsset speciesAsset = AssetManager.loyalty_library.get("species");
            if (speciesAsset != null)
            {
                speciesAsset.calc = delegate(City city) {
                    int result = 0;
                    if (city.isCapitalCity())
                    {
                        return 0;
                    }
                    if (city.kingdom.hasCapital())
                    {
                        if (city.kingdom.capital.getSpecies() == city.getSpecies())
                        {
                            result = 0;
                        }
                        else
                        {
                            result = -15;
                            if (city.hasLeader() && (city.leader.hasXenophobic() || (city.kingdom.hasKing() && city.kingdom.king.hasXenophobic())))
                            {
                                result = -40;
                            }else if (baseMod 
                                        && (city.hasLeader() 
                                        && (city.leader.hasXenophiles() || (city.kingdom.hasKing() && city.kingdom.king.hasXenophiles()))
                                        )
                                    ) {
                                result = 0;
                            }
                        }
                    }
                    return result;
                };
                AssetManager.loyalty_library.dict["species"] = speciesAsset;
            }



            //Add new loyalty asset to give positive loyalty with xenophile trait for mixed-species empires.
            LoyaltyAsset newSpeciesAsset = new LoyaltyAsset {
                id = "species_xenophile",
                translation_key = "loyalty_species_different",
                //translation_key_negative = "loyalty_species_different",
                calc = delegate(City city) {
                    int result = 0;
                    if (city.isCapitalCity())
                    {
                        return 0;
                    }
                    if (city.kingdom.hasCapital())
                    {
                        if (city.kingdom.capital.getSpecies() == city.getSpecies())
                        {
                            result = 0;
                        }
                        else
                        {
                            if (baseMod 
                                        && (city.hasLeader() 
                                        && (city.leader.hasXenophiles() || (city.kingdom.hasKing() && city.kingdom.king.hasXenophiles()))
                                        )
                                    ) {
                                result = 15;
                            }
                        }
                    }
                    return result;
                }
            };
            AssetManager.loyalty_library.add(newSpeciesAsset);
        }

    }
}
