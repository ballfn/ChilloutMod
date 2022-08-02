using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.VideoPlayer;
using cohtml.Net;
using ComfyUI;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(Mod), ModBuildInfo.Name, ModBuildInfo.Version, ModBuildInfo.Author)]
[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonColor(ConsoleColor.DarkGreen)]
namespace ComfyUI
{
    
    public static class ModBuildInfo
    {
        public const string Name = "ComfyUI";
        public const string Author = "ballfun";
        public const string Version = "0.1.0";
    }
    public class Mod : MelonMod
    {
        public static HarmonyLib.Harmony MyHarmony = new HarmonyLib.Harmony ("ComfyUI");
        
        public static MelonPreferences_Category MyPreferenceCategory;
        public static MelonPreferences_Entry<bool> GroundUI;
        public static MelonPreferences_Entry<bool> DoRotateThreshold;
        public static MelonPreferences_Entry<float> RotateThreshold;
        
        public override void OnApplicationStart()
        {
            
            MyPreferenceCategory = MelonPreferences.CreateCategory("ComfyUI");
            GroundUI = MyPreferenceCategory.CreateEntry("GroundUI", true,"Ground UI","Force UI to rotate towards the ground");
            DoRotateThreshold = MyPreferenceCategory.CreateEntry
                ("DoRotateThreshold", true, "DoRotateThreshold", "Rotate UI if your head is too rotated sideways");
            RotateThreshold = MyPreferenceCategory.CreateEntry
                ("RotateThreshold", 30f, "RotateThreshold", "The threshold for the UI to rotate");

            MyHarmony.Patch
            (
                typeof(ViewManager).GetMethod(nameof(ViewManager.UiStateToggle), new [] {typeof(bool)}),
                null,
                typeof(Mod).GetMethod(nameof(UiStateTogglePatch)).ToNewHarmonyMethod()
            );
        }

        public static void UiStateTogglePatch(bool __0 ,ViewManager __instance)
        {
            if (__0&&GroundUI.Value)
            {
                var transform = __instance.transform;
                Vector3 euler = transform.eulerAngles;
                if (!DoRotateThreshold.Value||(euler.z< RotateThreshold.Value||euler.z> 360-RotateThreshold.Value))
                {
                    euler.z = 0;
                }
                transform.eulerAngles = euler;
            }
        }
    }
}
