using System;
using System.Collections;
using System.Collections.Generic;
using NCMS;
using UnityEngine;
using ReflectionUtility;

namespace FriendlyVillagers{
    [ModEntry]
    class Main : MonoBehaviour{

        void Awake(){
            Patches.init();
        }
    }
}