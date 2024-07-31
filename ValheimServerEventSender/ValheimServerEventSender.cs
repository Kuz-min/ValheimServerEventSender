using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
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
        public const string MOD_VERSION = "0.2.0";
        public const string JSON_CONTENT_TYPE = "application/json";


        public static void OnServerStarted(string serverName, string worldName)
        {
            if (_instance != null)
            {
                _instance._serverName = serverName;
                _instance._worldName = worldName;
                _instance.SendServerEvent(EventType.ServerStarted);
            }
        }
        public static void OnPeerConnected(string userName)
        {
            if (_instance != null && !string.IsNullOrEmpty(userName))
            {
                _instance.SendPeerEvent(EventType.PeerConnected, userName);
            }
        }
        public static void OnPeerDisconnected(string userName)
        {
            if (_instance != null && !string.IsNullOrEmpty(userName))
            {
                _instance.SendPeerEvent(EventType.PeerDisconnected, userName);
            }
        }


        private void SendServerEvent(EventType eventType)
        {
            var e = new BaseEvent() { Event = eventType.ToString() };
            SendEvent(e);
        }


        private void SendPeerEvent(EventType eventType, string userName)
        {
            var e = new PeerEvent() { Event = eventType.ToString(), UserName = userName };
            SendEvent(e);
        }


        private void SendEvent(BaseEvent e)
        {
            e.ServerName = _serverName;
            e.WorldName = _worldName;
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

            _uri = Config.Bind("General.Server", "Uri", "http://127.0.0.1:8888/api/valheim/event");

            _harmony = new Harmony(MOD_ID);
            _harmony.PatchAll();
        }


        private static ValheimServerEventSender? _instance;
        private ConfigEntry<string>? _uri;
        private Harmony? _harmony;
        private string? _serverName;
        private string? _worldName;
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
        public string? ServerName;
        public string? WorldName;
    }
    [Serializable]
    class PeerEvent : BaseEvent
    {
        public string? UserName;
    }


    [HarmonyPatch(typeof(ZNet))]
    static class ZNet_Patch
    {
        private static string GetServerName() => AccessTools.StaticFieldRefAccess<ZNet, string>("m_ServerName");

        [HarmonyPostfix]
        [HarmonyPatch("Start")]
        private static void Start_Postfix(ZNet __instance)
        {
            try
            {
                ValheimServerEventSender.OnServerStarted(GetServerName(), __instance.GetWorldName());
            }
            catch (Exception e)
            {
                ZLog.LogError($"[{ValheimServerEventSender.MOD_ID}]: {e.Message}");
            }
        }
    }


    [HarmonyPatch(typeof(ZDOMan))]
    static class ZDOMan_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch("AddPeer", typeof(ZNetPeer))]
        private static void AddPeer_Postfix(ZNetPeer netPeer)
        {
            try
            {
                if (netPeer != null)
                    ValheimServerEventSender.OnPeerConnected(netPeer.m_playerName);
            }
            catch (Exception e)
            {
                ZLog.LogError($"[{ValheimServerEventSender.MOD_ID}]: {e.Message}");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("RemovePeer", typeof(ZNetPeer))]
        private static void RemovePeer_Postfix(ZNetPeer netPeer)
        {
            try
            {
                if (netPeer != null)
                    ValheimServerEventSender.OnPeerDisconnected(netPeer.m_playerName);
            }
            catch (Exception e)
            {
                ZLog.LogError($"[{ValheimServerEventSender.MOD_ID}]: {e.Message}");
            }
        }
    }
}