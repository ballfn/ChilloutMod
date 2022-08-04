using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Player;
using ComfyUI;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(Mod), ModBuildInfo.Name, ModBuildInfo.Version, ModBuildInfo.Author)]
[assembly: MelonGame("Alpha Blend Interactive", "ChilloutVR")]
[assembly: MelonColor(ConsoleColor.Cyan)]
namespace ComfyUI
{
    
    public static class ModBuildInfo
    {
        public const string Name = "ComfyUI";
        public const string Author = "ballfun";
        public const string Version = "1.0.0";
    }
    public class Mod : MelonMod
    {
        public static HarmonyLib.Harmony MyHarmony = new HarmonyLib.Harmony ("ComfyUI");
        
        public static MelonPreferences_Category MyPreferenceCategory;
        public static MelonPreferences_Entry<bool> GroundUI;
        public static MelonPreferences_Entry<bool> FollowUI;
        public static MelonPreferences_Entry<bool> FollowUIRotate;
        public static MelonPreferences_Entry<bool> DoGroundThreshold;
        
        public static MelonPreferences_Category MyPreferenceAdvanced;
        public static MelonPreferences_Entry<bool> UncomfyUI;
        
        public static MelonPreferences_Entry<float> FollowUIDistance;
        public static MelonPreferences_Entry<float> RotateUIThreshold;
        public static MelonPreferences_Entry<float> GroundThreshold;
        
        
        public override void OnApplicationStart()
        {
            MyPreferenceCategory = MelonPreferences.CreateCategory("ComfyUI");
            GroundUI = MyPreferenceCategory.CreateEntry("GroundUI", true,"Ground UI","Force UI to rotate towards the ground");
            DoGroundThreshold = MyPreferenceCategory.CreateEntry
                ("DoGroundThreshold", true, "DoRotate Threshold", "Rotate UI if your head is too rotated sideways");
            FollowUI = MyPreferenceCategory.CreateEntry("FollowUI", true,"Follow UI","UI follow you when you move around");
            FollowUIRotate= MyPreferenceCategory.CreateEntry("FollowUIRotate", false,"Follow UI Rotate","Also follow your head direction");

            MyPreferenceAdvanced = MelonPreferences.CreateCategory("ComfyUIAdvanced");
            UncomfyUI = MyPreferenceAdvanced.CreateEntry
                ("UncomfyUI", false,"UncomfyUI","blame all the funny man on the discord",true);
            FollowUIDistance = MyPreferenceAdvanced.CreateEntry
                ("FollowUIDistance", 1f, "FollowUI Distance", "The distance for the UI to start following you");
            RotateUIThreshold = MyPreferenceAdvanced.CreateEntry
                ("RotateUIThreshold", 60f, "RotateUI Threshold", "The threshold for the UI to start following your rotation");
            GroundThreshold = MyPreferenceAdvanced.CreateEntry
                ("GroundThreshold", 30f, "Ground Threshold", "The threshold for the UI to rotate");

            MyHarmony.Patch
            (
                typeof(ViewManager).GetMethod(nameof(ViewManager.UiStateToggle), new [] {typeof(bool)}),
                null,
                typeof(Mod).GetMethod(nameof(UiStateTogglePatch)).ToNewHarmonyMethod()
            );
            MyHarmony.Patch
            (
                typeof(ViewManager).GetMethod("Start",BindingFlags.NonPublic | BindingFlags.Instance ),
                null,
                typeof(Mod).GetMethod(nameof(ViewManagerStart)).ToNewHarmonyMethod()
            );
            MyHarmony.Patch
            (
                typeof(ViewManager).GetMethod(nameof(ViewManager.SetScale), new [] {typeof(float)}),
                typeof(Mod).GetMethod(nameof(ScalePatch)).ToNewHarmonyMethod()
            );
        }
        public static void ViewManagerStart(ViewManager __instance)
        {
            __instance.gameObject.AddComponent<ComfyUI.FancyUI>();
        }
        public static void ScalePatch(float __0)
        {
            FancyUI.ScaleFactor = __0;
            if (UncomfyUI.Value) FancyUI.ScaleFactor = 0;
        }
        public static void UiStateTogglePatch(bool __0 ,ViewManager __instance)
        {
            if (__0&&GroundUI.Value)
            {
                var transform = __instance.transform;
                Vector3 euler = ClampRotation(transform.eulerAngles);
                
                
                
                transform.eulerAngles = euler;
            }

            if (__0)
            {
                FancyUI.RotateForward = PlayerSetup.Instance._movementSystem.rotationPivot.forward;
            }
        }

        public static Vector3 ClampRotation(Vector3 euler)
        {
            if (!DoGroundThreshold.Value||(euler.z< GroundThreshold.Value||euler.z> 360-GroundThreshold.Value))
            {
                euler.z = 0;
            }
            if (UncomfyUI.Value) euler.z += 6;
            return euler;
        }
    }

    public class FancyUI : MonoBehaviour
    {
        public static float ScaleFactor = 1f;
        public static Vector3 RotateForward = Vector3.zero;
        public float hudAngleThreshold => Mod.RotateUIThreshold.Value;
        public float hudMinimumAngle = 0.5f;
        float snapDistance = 0.1f;

        private void Start()
        {
            MelonLogger.Msg("FancyUI started");
        }

        private Transform rotationPivot;
        private void FixedUpdate()
        {
            rotationPivot = PlayerSetup.Instance._movementSystem.rotationPivot;
            if (Mod.FollowUI.Value)
            {
                _UpdatePosition();
                if(Mod.FollowUIRotate.Value) _UpdateRotation();
            }
            
        }
        private bool _lerpPos = false;
        private Vector3 _velocity = Vector3.zero;
        private bool _rotateTowardsView = false;
        private Vector3 _snapPos= Vector3.positiveInfinity;
        public void _UpdatePosition()
        {
            Vector3 posToSpawn = rotationPivot.position + RotateForward * 1f * ScaleFactor;
            
            if (Vector3.Distance(transform.position, rotationPivot.position) > Mod.FollowUIDistance.Value)
                _lerpPos = true;
            
            if (Vector3.Distance(posToSpawn, _snapPos)<snapDistance)
            {
                posToSpawn = _snapPos;
            }
            else
            {
                _snapPos =  posToSpawn;
            }
            
            if (_lerpPos)
            {
                var distance = Vector3.Distance(transform.position, posToSpawn);
                if (distance > 5)
                {
                    transform.position = posToSpawn;
                    _lerpPos = false;
                }
                else
                {
                    if (distance < 0.01f) _lerpPos = false;
                    transform.position = Vector3.SmoothDamp(transform.position, posToSpawn, ref _velocity, 0.07f);
                }
            }
            else
            {
                _velocity = Vector3.zero;
            }
        }

        void _UpdateRotation()
        {
            Vector3 headRot = Mod.ClampRotation(rotationPivot.rotation.eulerAngles);
            RotateForward = rotationPivot.forward;
            float angleDifference = Quaternion.Angle(transform.rotation, Quaternion.Euler(headRot));

            if (!_rotateTowardsView)
            {
                if (angleDifference > hudAngleThreshold)
                {
                    _rotateTowardsView = true;
                }
            }
            else
            {
                if (angleDifference > hudMinimumAngle)
                {
                    _lerpPos = true;
                    var rot = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(headRot), 
                        angleDifference / (Time.deltaTime * 300f) + Time.deltaTime / angleDifference);
                    transform.rotation = rot;
                }
                else _rotateTowardsView = false;
            }

           
        }
    }
}