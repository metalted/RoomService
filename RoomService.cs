﻿using System;
using System.Collections.Generic;
using UnityEngine;
using ZeepkistClient;

namespace RoomService
{
    public static class RoomService
    {       
        //Called when the round starts (in the beginning).
        public static Action OnRoundStart;
        //Called when the round ends (to podium).
        public static Action OnRoundEnd;
        //Called when a player joins the game.
        public static Action<RoomServicePlayer> OnPlayerJoined;
        //Called when a player leaves the game.
        public static Action<RoomServicePlayer> OnPlayerLeft;
        //Called right after the config is loaded, to execute all the OnLoad functions.
        public static Action OnConfigLoad;
        //Called right after the config is unloaded, for clean up.
        public static Action OnConfigUnload;

        //The config that is currently active.
        public static RoomServiceConfig CurrentConfig;
        //The tracker keeps track of players, points and best times.
        public static RSRoomTracker tracker;

        //Initialize is called when the plugin starts.
        public static void Initialize()
        {
            //Create a new tracker.
            tracker = new RSRoomTracker();

            //Subscribe to all the events.
            ZeepkistNetwork.LeaderboardUpdated += () =>
            {
                tracker.ProcessRoomState(ZeepkistNetwork.PlayerList, ZeepSDK.Level.LevelApi.CurrentLevel);
            };

            ZeepSDK.Multiplayer.MultiplayerApi.PlayerJoined += (player) =>
            {
                RoomServicePlayer rsPlayer = tracker.AddPlayer(player);
                OnPlayerJoined?.Invoke(rsPlayer);
            };

            ZeepSDK.Multiplayer.MultiplayerApi.PlayerLeft += (player) =>
            {
                RoomServicePlayer rsPlayer = tracker.GetPlayer(player.SteamID);
                if (rsPlayer != null)
                {
                    rsPlayer.IsOnline = false;
                    OnPlayerLeft?.Invoke(rsPlayer);
                }
            };

            ZeepSDK.Racing.RacingApi.LevelLoaded += () =>
            {
                tracker.ProcessRoomState(ZeepkistNetwork.PlayerList, ZeepSDK.Level.LevelApi.CurrentLevel);
                OnRoundStart?.Invoke();
            };

            ZeepSDK.Racing.RacingApi.RoundEnded += () =>
            {
                OnRoundEnd?.Invoke();
            };

            ZeepSDK.Multiplayer.MultiplayerApi.JoinedRoom += () =>
            {
                tracker.ProcessRoomState(ZeepkistNetwork.PlayerList, ZeepSDK.Level.LevelApi.CurrentLevel);
            };

            ZeepSDK.Multiplayer.MultiplayerApi.DisconnectedFromGame += () =>
            {
                tracker.SetAllPlayersNetworkState(false);
            };           
        }

        public static void ClearSubscriptions()
        {
            OnRoundStart = null;
            OnRoundEnd = null;
            OnPlayerJoined = null;
            OnPlayerLeft = null;
            OnConfigLoad = null;
            OnConfigUnload = null;
        }

        public static void LoadConfig(RoomServiceConfig config)
        {
            //Make sure all subscriptions to events are removed.
            ClearSubscriptions();
            // Save a reference to the config.
            CurrentConfig = config;
            //Load the config
            config.Load();
            //All events have been processed, invoke the OnLoad
            OnConfigLoad?.Invoke();
        }

        public static void UnloadConfig()
        {
            if(CurrentConfig != null)
            {
                OnConfigUnload?.Invoke();
                CurrentConfig = null;
            }

            ClearSubscriptions();
        }

        public static void SubscribeToEvent(string eventName, string functionName, List<string> parameters)
        {
            switch (eventName)
            {
                default:
                    Debug.LogError($"{eventName} is not a valid event name.");
                    break;
                case "OnLoad":
                    if (RoomServiceActions.ActionMap.ContainsKey(functionName))
                    {
                        OnConfigLoad += () =>
                        {
                            RoomServiceContext context = CreateContext();
                            RoomServiceActions.ActionMap[functionName]?.Invoke(parameters, context);
                        };                       
                    }
                    else
                    {
                        Debug.LogError($"Unknown function name in OnLoad event: {functionName}");
                    }
                    break;
                case "OnUnload":
                    if (RoomServiceActions.ActionMap.ContainsKey(functionName))
                    {
                        OnConfigUnload += () =>
                        {
                            RoomServiceContext context = CreateContext();
                            RoomServiceActions.ActionMap[functionName]?.Invoke(parameters, context);
                        };
                    }
                    else
                    {
                        Debug.LogError($"Unknown function name in OnUnload event: {functionName}");
                    }
                    break;
                case "OnPlayerJoined":
                    if (RoomServiceActions.ActionMap.ContainsKey(functionName))
                    {
                        OnPlayerJoined += player =>
                        {
                            RoomServiceContext context = CreateContext(player:player);
                            RoomServiceActions.ActionMap[functionName]?.Invoke(parameters, context);
                        };
                    }
                    else
                    {
                        Debug.LogError($"Unknown function name in OnPlayerJoined event: {functionName}");
                    }
                    break;
                case "OnPlayerLeft":
                    if (RoomServiceActions.ActionMap.ContainsKey(functionName))
                    {
                        OnPlayerLeft += (player) =>
                        {
                            RoomServiceContext context = CreateContext(player:player);
                            RoomServiceActions.ActionMap[functionName]?.Invoke(parameters, context);
                        };
                    }
                    else
                    {
                        Debug.LogError($"Unknown function name in OnPlayerLeft event: {functionName}");
                    }
                    break;
                case "OnRoundStart":
                    if (RoomServiceActions.ActionMap.ContainsKey(functionName))
                    {
                        OnRoundStart += () =>
                        {
                            RoomServiceContext context = CreateContext();
                            RoomServiceActions.ActionMap[functionName]?.Invoke(parameters, context);
                        };
                    }
                    else
                    {
                        Debug.LogError($"Unknown function name in OnRoundStart event: {functionName}");
                    }
                    break;
                case "OnRoundEnd":
                    if (RoomServiceActions.ActionMap.ContainsKey(functionName))
                    {
                        OnRoundEnd += () =>
                        {
                            RoomServiceContext context = CreateContext();
                            RoomServiceActions.ActionMap[functionName]?.Invoke(parameters, context);
                        };
                    }
                    else
                    {
                        Debug.LogError($"Unknown function name in OnRoundEnd event: {functionName}");
                    }
                    break;                
            }
        }      

        public static RoomServiceContext CreateContext(RoomServicePlayer player = null, RoomServiceLevel level = null, RoomServiceResult result = null)
        {
            RoomServiceContext ctx = new RoomServiceContext(CurrentConfig?.Parameters ?? new Dictionary<string, string>());
            bool setPlayer = false;
            bool setLevel = false;

            ZeepkistLobby currentLobby = ZeepkistNetwork.CurrentLobby;
            if(currentLobby != null)
            {
                if(currentLobby.Playlist != null)
                {
                    int length = currentLobby.Playlist.Count;
                    int index = currentLobby.CurrentPlaylistIndex;

                    ctx.SetPlaylistData(length, index + 1);
                }
            }
            
            if (result != null)
            {
                ctx.AddResult(result);

                RoomServicePlayer rsPlayer = tracker.GetPlayer(result.SteamID);
                if (rsPlayer != null)
                {
                    ctx.AddPlayer(rsPlayer);
                    setPlayer = true;
                }

                RoomServiceLevel rsLevel = tracker.GetLevel(result.UID);
                if (rsLevel != null)
                {
                    ctx.AddLevel(rsLevel);
                    setLevel = true;
                }
            }

            if (player != null && !setPlayer)
            {
                ctx.AddPlayer(player);
            }

            if (!setLevel)
            {
                if (level != null)
                {
                    ctx.AddLevel(level);
                }
                else
                {
                    RoomServiceLevel rsLevel = tracker.GetCurrentLevel();
                    if (rsLevel != null)
                    {
                        ctx.AddLevel(rsLevel);
                    }
                }
            }

            return ctx;
        }
    }
}
