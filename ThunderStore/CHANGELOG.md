## 0.0.6
- Make sure chatcolors loads before commandlib

## 0.0.5
- Add option to send failed commands to server anyway (defaults to false)
	- Allows server side mods like HostModeration to still recieve commands
- Fix some bad namespaces
- Update game version to 12026.a2

## 0.0.4
- Always parse like in 0.0.2 but delays killing the message so other mods can still parse it
	- Parses commands first and if fails adds a flag to prevent sending to server
	- This allows other mods to still parse the message even if it's a command
		- Any mod that would forward the message to the server tho would still break
- Clients no longer send a server command list packet
- Server command list is now sent through p2p
- CommandOptions is now optional again when registering commands
		
## 0.0.3
- Make patches run last so other mods can recieve commands

## 0.0.2
- Ignore `//` since some people use it in rp
- Fix console commands being broken
- only print full help message when /help is called and not when a command fails
- better homebrewery compatibility

## 0.0.1
- **WARNING: BETA RELEASE**
- Server side commands have NOT been well tested so expect bugs.
- This hooks into every chat message and should remove any failed commands before they get sent to the server.
- `/chatcolor` command added as a test for a client and server side command. This will be removed whenever some command library becomes standard or chatcolor prevents failed commands from entering chat.
- Suggestions to improve the API are welcome. Open an issue on github or use the Atlyss Modding Discord server.