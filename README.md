# MVirus

MVirus is an unofficial modification for [7 Days to Die](https://7daystodie.com/). It downloads mods from the server to the client. The mod should be installed on both the client and the server to work.

# Supported mods:
The mod supports all mods that do not use client-side DLLs. This includes:
* Custom models, textures, and sounds from `*.unity3d` files
* UI atlases for items
* POI

# Compatibility:
* EAC (Easy Anti-Cheat) is not supported. Disable it in the server settings and game launcher.
* The mod has been tested on v1.2 (b27) and v1.3 (b9). We have no information about older game versions.
* MVirus works on dedicated servers and local hosts (from version 1.0.0).

# Installation

## Client

1. Download the latest [release](https://github.com/TheNormalnij/7DTD-MVirus/releases).
2. Unpack the archive into your `7 Days to Die/Mods` folder.
3. Enable mod sharing in `config.xml` if you want to share your mods.

## Server

1. Download the latest [release](https://github.com/TheNormalnij/7DTD-MVirus/releases).
2. Unpack the archive into your `7 Days to Die Dedicated Server/Mods` folder.
3. Enable mod sharing in `config.xml`.

# Configuration:
By default, MVirus is configured to download mods only. You can customize the configuration in `config.xml` based on your needs.

## You want to download mods only
* This option is enabled by default.
* Set the `ShareMods` property to `false`.
* This fixes a protocol error when a friend without MVirus tries to connect to your local server.

## You want to share mods with your friends on LAN or the same Wi-Fi network
* Set the `ShareMods` property to `true`.
* Set the `FileTransferType` property to `0` to enable the internal HTTP server.
* Disable all compression options.
* Disable the `CacheAllRemoteFiles` property to save disk space.
* Allow the internal HTTP server port (specified in `HttpPort`) in your firewall rules.

## You host a server (local or dedicated) without an additional HTTP server
* You need a public IP address for this.
* Set the `ShareMods` property to `true`.
* Set the `FileTransferType` property to `0` to enable the internal HTTP server.
* Enable static compression in `StaticCompression`.
* Open the port for the internal HTTP server on your router. Check the port in the `HttpPort` property.
* Allow the port in your firewall rules.

## You have an external HTTP server for files
* Set the `ShareMods` property to `true`.
* Set the `FileTransferType` property to `1` to use an external HTTP server.
* Update the `ExternalHTTPServerAddr` property with your HTTP server address.
* Enable static compression in `StaticCompression`.
* Enable `CacheAllRemoteFiles`.
* Sync the `HttpServerFiles` folder with your HTTP server.

## You host a local server without a public IP
* This option provides poor download speeds for your players compared to an HTTP server.
* Set the `ShareMods` property to `true`.
* Set the `FileTransferType` property to `2`.
* Enable static compression in `StaticCompression`.

# Support

Create an issue in this repository to report a bug or request a feature.
You can also support the project with a [donation](https://thenormalnij.de/donate.html).
