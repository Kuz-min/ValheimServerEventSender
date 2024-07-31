# Valheim Server Event Sender
This mod sends POST requests with a JSON body to the url defined in the configuration, when:
* the server is started and ready for connections
```
{"Event":"ServerStarted","ServerName":"Example","WorldName":"Example"}
```
* players connect
```
{"Event":"PeerConnected","ServerName":"Example","WorldName":"Example","UserName":"Example"}
```
* players disconect
```
{"Event":"PeerDisconnected","ServerName":"Example","WorldName":"Example","UserName":"Example"}
```
# Installation
1. Install the BepInEx mod loader
2. Put ValheimServerEventSender.dll into <ValheimServerDirectory>\BepInEx\plugins
3. Put ValheimServerEventSender.cfg into <ValheimServerDirectory>\BepInEx\config
4. Set your url in the ValheimServerEventSender.cfg file
