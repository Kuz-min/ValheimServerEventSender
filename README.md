# Valheim Server Event Sender
This mod sends POST requests with a JSON body to the url defined in the configuration, when:
* the server is started and ready for connections
```
{"Event":"ServerStarted"}
```
* players connect
```
{"Event":"PeerConnected","UserId":"<SteamId>"}
```
* players disconect
```
{"Event":"PeerDisconnected","UserId":"<SteamId>"}
```
# Installation
1. Install the BepInEx mod loader
2. Put ValheimServerEventSender.dll into <ValheimServerDirectory>\BepInEx\plugins
3. Put ValheimServerEventSender.cfg into <ValheimServerDirectory>\BepInEx\config
4. Set your url in the ValheimServerEventSender.cfg file