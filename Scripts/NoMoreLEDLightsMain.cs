// Project:         No More LED Lights mod for Daggerfall Unity (http://www.dfworkshop.net)
// Copyright:       Copyright (C) 2025 Kirk.O
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Author:          Kirk.O
// Created On: 	    1/4/2025, 9:00 AM
// Last Edit:		5/15/2025, 2:00 PM
// Version:			1.10
// Special Thanks:  
// Modifier:

using UnityEngine;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.Utility.ModSupport.ModSettings;
using System;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop;
using DaggerfallConnect;

namespace NoMoreLEDLights
{
    public partial class NoMoreLEDLightsMain : MonoBehaviour
    {
        public static NoMoreLEDLightsMain Instance;

        static Mod mod;

        // General Options
        public static bool AlterDungeonLights { get; set; }
        public static Color32 DungeonLightColor { get; set; }
        public static int DungeonLightIntensity { get; set; }

        public static bool AlterCityLights { get; set; }
        public static Color32 CityLightColor { get; set; }
        public static int CityLightIntensity { get; set; }

        public static bool AlterInteriorBuildingLights { get; set; }
        public static Color32 BuildingInteriorLightColor { get; set; }
        public static int BuildingInteriorLightIntensity { get; set; }

        public static bool AlterPlayerTorch { get; set; }
        public static Color32 PlayerTorchColor { get; set; }
        public static int PlayerTorchIntensity { get; set; }

        public static bool exceptionLogging = true;

        // Mod Compatibility Check Values
        public static bool DarkerDungeonsCheck { get; set; }

        public static bool changedPlayerTorch = false;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            mod = initParams.Mod;
            var go = new GameObject(mod.Title);
            go.AddComponent<NoMoreLEDLightsMain>(); // Add script to the scene.

            mod.LoadSettingsCallback = LoadSettings; // To enable use of the "live settings changes" feature in-game.

            mod.IsReady = true;
        }

        private void Start()
        {
            Debug.Log("Begin mod init: No More LED Lights");

            Instance = this;

            mod.LoadSettings();

            ModCompatibilityChecking();

            StartGameBehaviour.OnStartGame += UpdateLights_OnStartGame;
            SaveLoadManager.OnLoad += UpdateLights_OnSaveLoad;

            PlayerEnterExit.OnTransitionInterior += UpdateLights_OnTransitionInterior;
            PlayerEnterExit.OnTransitionExterior += UpdateLights_OnTransitionExterior;
            PlayerEnterExit.OnTransitionDungeonInterior += UpdateLights_OnTransitionDungeonInterior;
            PlayerEnterExit.OnTransitionDungeonExterior += UpdateLights_OnTransitionDungeonExterior;

            PlayerGPS.OnEnterLocationRect += UpdateLights_OnEnterLocationRect;

            Debug.Log("Finished mod init: No More LED Lights");
        }

        private static void LoadSettings(ModSettings modSettings, ModSettingsChange change)
        {
            AlterDungeonLights = mod.GetSettings().GetValue<bool>("GeneralSettings", "AllowAlteringDungeonLights");
            DungeonLightColor = mod.GetSettings().GetValue<Color32>("GeneralSettings", "DungeonLightColor");
            DungeonLightIntensity = mod.GetSettings().GetValue<int>("GeneralSettings", "DungeonLightIntensity");

            AlterCityLights = mod.GetSettings().GetValue<bool>("GeneralSettings", "AllowAlteringCityLights");
            CityLightColor = mod.GetSettings().GetValue<Color32>("GeneralSettings", "CityLightColor");
            CityLightIntensity = mod.GetSettings().GetValue<int>("GeneralSettings", "CityLightIntensity");

            AlterInteriorBuildingLights = mod.GetSettings().GetValue<bool>("GeneralSettings", "AllowAlteringBuildingInteriorLights");
            BuildingInteriorLightColor = mod.GetSettings().GetValue<Color32>("GeneralSettings", "BuildingInteriorLightColor");
            BuildingInteriorLightIntensity = mod.GetSettings().GetValue<int>("GeneralSettings", "BuildingInteriorLightIntensity");

            AlterPlayerTorch = mod.GetSettings().GetValue<bool>("GeneralSettings", "AllowAlteringPlayerTorch");
            PlayerTorchColor = mod.GetSettings().GetValue<Color32>("GeneralSettings", "PlayerTorchColor");
            PlayerTorchIntensity = mod.GetSettings().GetValue<int>("GeneralSettings", "PlayerTorchIntensity");

            exceptionLogging = true;

            UpdateSceneLights();
        }

        private void ModCompatibilityChecking()
        {
            // Darker Dungeons mod: https://www.nexusmods.com/daggerfallunity/mods/286
            Mod darkerDungeons = ModManager.Instance.GetModFromGUID("e911251d-00f9-4d0a-beed-546bd2a5624a");
            DarkerDungeonsCheck = darkerDungeons != null ? true : false;
        }

        public static void UpdateSceneLights()
        {
            try
            {
                if (SaveLoadManager.Instance.LoadInProgress)
                    return;

                float dungeonLightRealIntensity = (float)(DungeonLightIntensity / 10f);
                float cityLightRealIntensity = (float)(CityLightIntensity / 10f);
                float buildingInteriorLightRealIntensity = (float)(BuildingInteriorLightIntensity / 10f);
                float playerTorchRealIntensity = (float)(PlayerTorchIntensity / 10f);

                if (AlterPlayerTorch)
                {
                    Light playerLightSource = GameManager.Instance.PlayerObject.GetComponent<EnablePlayerTorch>().PlayerTorch.GetComponent<Light>();
                    if (playerLightSource != null)
                    {
                        playerLightSource.color = PlayerTorchColor;
                        playerLightSource.intensity = playerTorchRealIntensity;
                        changedPlayerTorch = true;
                    }
                }
                else
                {
                    if (changedPlayerTorch)
                    {
                        Light playerLightSource = GameManager.Instance.PlayerObject.GetComponent<EnablePlayerTorch>().PlayerTorch.GetComponent<Light>();
                        if (playerLightSource != null)
                        {
                            playerLightSource.color = new Color(1, 1, 1);
                            playerLightSource.intensity = 1.25f;
                            changedPlayerTorch = false;
                        }
                    }
                }

                if (DarkerDungeonsCheck)
                {
                    if (AlterDungeonLights)
                    {
                        Light[] ddLights;
                        ddLights = Resources.FindObjectsOfTypeAll(typeof(Light)) as Light[];
                        for (int i = 0; i < ddLights.Length; i++)
                        {
                            if (ddLights[i].name == "ImprovedDungeonLight")
                            {
                                ddLights[i].color = DungeonLightColor;
                                ddLights[i].intensity = dungeonLightRealIntensity;
                            }
                        }
                    }
                }

                Light[] lights;
                lights = FindObjectsOfType<Light>();
                for (int i = 0; i < lights.Length; i++)
                {
                    if (AlterDungeonLights && (lights[i].name == "DaggerfallLight [Dungeon]" || lights[i].name == "DaggerfallLight [Dungeon](Clone)"))
                    {
                        lights[i].color = DungeonLightColor;
                        lights[i].intensity = dungeonLightRealIntensity;
                    }

                    if (AlterCityLights && (lights[i].name == "DaggerfallLight [City]" || lights[i].name == "DaggerfallLight [City](Clone)"))
                    {
                        lights[i].color = CityLightColor;
                        lights[i].intensity = cityLightRealIntensity;
                    }

                    if (AlterInteriorBuildingLights && (lights[i].name == "DaggerfallLight [Interior]" || lights[i].name == "DaggerfallLight [Interior](Clone)"))
                    {
                        lights[i].color = BuildingInteriorLightColor;
                        lights[i].intensity = buildingInteriorLightRealIntensity;
                    }
                }

                if (AlterDungeonLights && DaggerfallUnity.Instance.Option_DungeonLightPrefab != null)
                {
                    DaggerfallUnity.Instance.Option_DungeonLightPrefab.color = DungeonLightColor;
                    DaggerfallUnity.Instance.Option_DungeonLightPrefab.intensity = dungeonLightRealIntensity;
                }
                else if (DaggerfallUnity.Instance.Option_DungeonLightPrefab != null)
                {
                    DaggerfallUnity.Instance.Option_DungeonLightPrefab.range = 5;
                    DaggerfallUnity.Instance.Option_DungeonLightPrefab.color = new Color(1, 1, 1);
                    DaggerfallUnity.Instance.Option_DungeonLightPrefab.intensity = 0.8f;
                }

                if (AlterCityLights && DaggerfallUnity.Instance.Option_CityLightPrefab != null)
                {
                    DaggerfallUnity.Instance.Option_CityLightPrefab.color = CityLightColor;
                    DaggerfallUnity.Instance.Option_CityLightPrefab.intensity = cityLightRealIntensity;
                }
                else if (DaggerfallUnity.Instance.Option_CityLightPrefab != null)
                {
                    DaggerfallUnity.Instance.Option_CityLightPrefab.range = 18;
                    DaggerfallUnity.Instance.Option_CityLightPrefab.color = new Color(1, 1, 1);
                    DaggerfallUnity.Instance.Option_CityLightPrefab.intensity = 1;
                }

                // I don't think this Interior Light Prefab is actually ever used? Atleast it might have become obsolete at some point when another
                // method for adding interior lights to building scenes was added, not 100% certain though, just keeping here anyways.
                if (AlterInteriorBuildingLights && DaggerfallUnity.Instance.Option_InteriorLightPrefab != null)
                {
                    DaggerfallUnity.Instance.Option_InteriorLightPrefab.color = BuildingInteriorLightColor;
                    DaggerfallUnity.Instance.Option_InteriorLightPrefab.intensity = buildingInteriorLightRealIntensity;
                }
                else if (DaggerfallUnity.Instance.Option_InteriorLightPrefab != null)
                {
                    DaggerfallUnity.Instance.Option_InteriorLightPrefab.range = 15;
                    DaggerfallUnity.Instance.Option_InteriorLightPrefab.color = new Color(1, 1, 1);
                    DaggerfallUnity.Instance.Option_InteriorLightPrefab.intensity = 1;
                }
            }
            catch (Exception e)
            {
                if (exceptionLogging)
                {
                    Debug.LogErrorFormat("[ERROR] No More LED Lights: An exception has occured: " + e.ToString());
                    exceptionLogging = false;
                }
            }
        }

        public static void UpdateLights_OnStartGame(object sender, EventArgs e)
        {
            UpdateSceneLights();
        }

        public static void UpdateLights_OnSaveLoad(SaveData_v1 saveData)
        {
            UpdateSceneLights();
        }

        public void UpdateLights_OnTransitionInterior(PlayerEnterExit.TransitionEventArgs args)
        {
            UpdateSceneLights();
        }

        public void UpdateLights_OnTransitionExterior(PlayerEnterExit.TransitionEventArgs args)
        {
            UpdateSceneLights();
        }

        public void UpdateLights_OnTransitionDungeonInterior(PlayerEnterExit.TransitionEventArgs args)
        {
            UpdateSceneLights();
        }

        public void UpdateLights_OnTransitionDungeonExterior(PlayerEnterExit.TransitionEventArgs args)
        {
            UpdateSceneLights();
        }

        public void UpdateLights_OnEnterLocationRect(DFLocation location)
        {
            UpdateSceneLights();
        }
    }
}
