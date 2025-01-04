// using System;
// using NCMS;
// using UnityEngine;
// using ReflectionUtility;

// namespace FriendlyVillagers
// {
//     class PowerButtson : MonoBehaviour 
//     {
//         public static void init() {
//             PowersTab 
//             int index = 0;
//             int xPos = 72;
//             int yPos = 18;
//             int gap = 35;

//             PowerButtons.CreateButton(
//                 "friendlyvillagers_dej",
//                 "",
//                 "Friendly Villagers",
//                 "Villagers Won't Be Killed in Different-Race Wars",
//                 new Vector2(xPos + (index*gap), yPos),
//                 ButtonType.GodPower,
                
//             );
//         }

//         private static PowersTab getPowersTab(string id)
// 		{
// 			GameObject gameObject = GameObjects.FindEvenInactive("Tab_" + id);
// 			return gameObject.GetComponent<PowersTab>();
// 		}
//     }
// }