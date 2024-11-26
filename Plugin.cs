﻿using BepInEx;
using System;
using ZeepSDK;
using ZeepkistClient;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HarmonyLib;

namespace RoomService
{
    [BepInPlugin(pluginGUID, pluginName, pluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string pluginGUID = "com.metalted.zeepkist.roomservice";
        public const string pluginName = "RoomService";
        public const string pluginVersion = "1.0";

        private void Awake()
        {
            Harmony harmony = new Harmony(pluginGUID);
            harmony.PatchAll();

            RoomService.Initialize();

            // Plugin startup logic
            Logger.LogInfo($"At your service!");
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Keypad9))
            {
                RoomServiceConfig config = RoomServiceConfigLoader.LoadConfig(@"C:\Data\config.json");
                RoomService.LoadConfig(config);
            }

            if(Input.GetKeyDown(KeyCode.Keypad3))
            {
                RoomService.UnloadConfig();
            }
        }
    }
}