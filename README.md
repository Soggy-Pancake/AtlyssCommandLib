# Atlyss Command Library

## Requirements

- CodeYapper (Used to provide clients all root providers/commands that should be forwarded to the server)

## Goals of this project

1. Prevent people from putting failed chatcolor commands in chat like this `/chatcolor aaff21`, then requiring someone to tell them that they need to include the hashtag.
2. Provide a nice interface to register chat or console commands.

## How it works

This uses patches to capture chat messages before they are forwarded to the server. If it detects that it starts with a `/` then it begins to parse as a command. CodeYapper is required as anything that is unknown simply prints the default help message and the server will never see it as I am assuming the server is vanilla and will chuck any failed command straight into chat.

## Usage

It is recommended that you put all of your commands under your own command provider. A command provider can be thought of as a command folder. **Any name conflicts result in the command or provider silently failing and being ignored**

```
\-- YourMod (Provider)
    +-- Command1
    +-- Command2
    \-- CommandGroup (Provider)
        +-- Grouped1
        +-- Grouped2
```

First in your imports add

```csharp
using AtlyssCommandLib.API;
using static AtlyssCommandLib.API.Utils;
```

There are static methods to register your commands to the 'root' provider that contains this library's included built in commands and vanilla commands.

### Commands

Commands can be registered using the `RegisterCommand` method, either using the static one that registers to the root provider or by calling the method on your own provider. All commands default to being client side, however you can specify a client-side, server-side, or console command. For commands that might do local checks before forwarding to the server you can mark it as client and server side. Commands that are both client are server side have different functionality for their returns.

Commands recieve a Caller and a string array containing the arguments.

```csharp
delegate bool CommandCallback(Caller caller, string[] args);
```

As a server or client command returning false will result in showing the help message for your method.

> Note: Returning true as a server+client side command will forward the command to the server.

### Providers

A command provider is essentially a directory that can contain commands and other providers.

To create a provider use: `CommandProvider cp = new CommandProvider(<Prefix>, <HelpMsg>, [RootProvider])`
Providing the root provider is optional but can save time registering it to your target provider without needing to explicitly register it yourself. **If the target root provider is null it will autoregister itself to the root.**

## License

The license is LGPLv3
