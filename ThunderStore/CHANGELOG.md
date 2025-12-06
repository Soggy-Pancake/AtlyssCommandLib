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