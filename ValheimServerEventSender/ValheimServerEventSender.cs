using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace ValheimServerEventSender
{
    [BepInPlugin(MOD_ID, MOD_NAME, MOD_VERSION)]
    [BepInProcess("valheim_server.exe")]
    public class ValheimServerEventSender : BaseUnityPlugin
    {
        public const string MOD_ID = "ValheimServerEventSender";
        public const string MOD_NAME = "Valheim Server Event Sender";
        public const string MOD_VERSION = "0.1.0";
        public const string JSON_CONTENT_TYPE = "application/json";


        public static void OnServerStarted() => _instance?.SendServerEvent(EventType.ServerStarted);
        public static void OnPeerConnected(string userId) => _instance?.SendPeerEvent(EventType.PeerConnected, userId);
        public static void OnPeerDisconnected(string userId) => _instance?.SendPeerEvent(EventType.PeerDisconnected, userId);


        private void SendServerEvent(EventType eventType)
        {
            var e = new BaseEvent() { Event = eventType.ToString() };
            SendEvent(e);
        }


        private void SendPeerEvent(EventType eventType, string userId)
        {
            var e = new PeerEvent() { Event = eventType.ToString(), UserId = userId };
            SendEvent(e);
        }


        private void SendEvent(BaseEvent e)
        {
            var uri = new Uri(_uri?.Value);
            var json = JsonUtility.ToJson(e);
            StartCoroutine(RequestCoroutine(uri, json));
        }


        private IEnumerator RequestCoroutine(Uri url, string json)
        {
            var request = UnityWebRequest.Post(url, json, JSON_CONTENT_TYPE);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                ZLog.Log($"[{MOD_ID}]: Successful request to {url}");
            }
            else
            {
                ZLog.LogWarning($"[{MOD_ID}]: Error in request to {url}: {request.error}");
            }
        }


        private void Awake()
        {
            _instance = this;

            _uri = Config.Bind("General.Server", "Uri", "http://127.0.0.1:8888/api/valheim-servers/0/event");

            _harmony = new Harmony(MOD_ID);
            _harmony.PatchAll();
        }


        private static ValheimServerEventSender? _instance;
        private ConfigEntry<string>? _uri;
        private Harmony? _harmony;
    }


    enum EventType
    {
        Ping,
        ServerStarted,
        PeerConnected,
        PeerDisconnected,
    }
    static class EventTypeExtension
    {
        public static string ToString(this EventType e) => Enum.GetName(typeof(EventType), e);
    }


    [Serializable]
    class BaseEvent
    {
        public string Event = EventType.Ping.ToString();
    }
    [Serializable]
    class PeerEvent : BaseEvent
    {
        public string UserId = string.Empty;
    }


    [HarmonyPatch(typeof(ZSteamMatchmaking))]
    static class ZSteamMatchmaking_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch("OnSteamServersConnected")]
        private static void OnSteamServersConnected_Postfix()
        {
            try
            {
                ValheimServerEventSender.OnServerStarted();
            }
            catch (Exception e)
            {
                ZLog.LogError($"[{ValheimServerEventSender.MOD_ID}]: {e.Message}");
            }
        }

    }


    [HarmonyPatch(typeof(ZSteamSocket))]
    static class ZSteamSocket_Patch
    {
        static ZSteamSocket_Patch()
        {
            _ids = new Dictionary<HSteamNetConnection, string>();
            MethodInfo methodInfo = typeof(ZSteamSocket).GetMethod("FindSocket", BindingFlags.NonPublic | BindingFlags.Static);
            FindSocket = (Func<HSteamNetConnection, ZSteamSocket>)methodInfo.CreateDelegate(typeof(Func<HSteamNetConnection, ZSteamSocket>));
        }

        private static readonly Func<HSteamNetConnection, ZSteamSocket> FindSocket;

        [HarmonyPostfix]
        [HarmonyPatch("OnStatusChanged")]
        private static void OnStatusChanged_Postfix(SteamNetConnectionStatusChangedCallback_t data)
        {
            try
            {
                if (data.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected &&
                    data.m_eOldState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connecting)
                {
                    var id = FindSocket(data.m_hConn).GetHostName();
                    _ids.Add(data.m_hConn, id);
                    ValheimServerEventSender.OnPeerConnected(id);
                }
                if (data.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally ||
                    data.m_info.m_eState == ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer)
                {
                    if (_ids.TryGetValue(data.m_hConn, out var id))
                    {
                        _ids.Remove(data.m_hConn);
                        ValheimServerEventSender.OnPeerDisconnected(id);
                    }
                }
            }
            catch (Exception e)
            {
                ZLog.LogError($"[{ValheimServerEventSender.MOD_ID}]: {e.Message}");
            }
        }

        private static readonly Dictionary<HSteamNetConnection, string> _ids;
    }
}