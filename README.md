![logo](https://github.com/PaoloM/Fiona/blob/main/Original%20assets/Fiona%20logo%20-%20small.png)

# Fiona

[![Build status](https://build.appcenter.ms/v0.1/apps/d7f40dde-1410-4946-82eb-9b5c207f84a0/branches/main/badge)](https://appcenter.ms)
 
A [Squeezebox/Logitech Media Server](https://www.mysqueezebox.com/download) controller for Windows 10.

![](Original%20assets/Screenshots/v0.4-prealpha/Screenshot%202021-04-18%20191217.png)

## What's new?

* Fiona asks for your server's address when it cannot find one on the network, instead of coming up empty
* Much faster and more reliable server discovery
* Artist images in the Now Playing page now crossfade

## Screenshots 

* [v0.4](v0.4-prealpha-screenshots.md)

## Current features

* Autodiscover and connect to your Logitech Media Server/Squeezebox, or enter its address yourself
* Navigate your music library by album and artist
* Individual queues for all your connected players
* Now Playing page with artist images
* 3rd party apps/plugins almost completely supported
* Radio support
* Initial favorites support

## Roadmap/backlog

v0.5

* Queue management: direct select and play (todo)
* Favorites management (ongoing)
* Format and bitrate display for tracks (experimental)

v.Next

* Full 3rd party app/plugin support
* More animations and transitions

## Tested plugins

These plugins have been tested and are working per spec:

* Spotty
* Band's Camp
* Mixcloud
* YouTube

## Known issues

1. Some info in the artis profiles are rendered as numbers instead of text https://github.com/PaoloM/Fiona/issues/4
1. The personalization setting "Windows default" sets the colors to the app dark mode, not the Windows' one (is it really an issue?)
1. Navigating back from album/artist details to the main lists does not bring you back to the previous scroll location https://github.com/PaoloM/Fiona/issues/5
1. Updating the queue with the same number of entries as the existing one does not update the queue visuals https://github.com/PaoloM/Fiona/issues/6

## Release notes

#### 09/25/26 - v0.5.2

Startup and server discovery, largely rewritten:

* The address from the command line, or from your last session, is now checked against a short timeout *before* the window opens, and the slow LAN sweep runs with the window already up. Windows terminates an app that has not activated its window within a few seconds, which is what the sweep on the startup path was risking
* A saved address is verified rather than trusted, so a server that has moved or been switched off no longer leaves you looking at an empty library
* When no server can be found, Fiona asks for its address instead of starting up empty. The prompt takes `host:port`, can search the network again, and keeps what you last typed in front of you
* The LAN sweep probes every address at once, so it takes about as long as one timeout rather than one timeout per address
* The sweep now covers every local adapter's subnet instead of guessing one, so a WSL or Hyper-V adapter on the default route no longer sends the whole sweep to the wrong subnet

Fixed:

* Crash on startup whenever a server had not been resolved yet (`NullReferenceException` in `BaseViewModel.Albums`) - the data service answers `null` with no server configured, and three bound properties dereferenced it
* The player picker stayed empty when the server was only found after the window had opened: the shell sits outside the navigation frame, so re-navigating did not refresh it
* Navigating to an album by ID threw when the library was not loaded
* Ending the LAN sweep early raised one exception per queued probe - around 190 of them per successful sweep

Also:

* Artist images in the Now Playing page crossfade instead of cutting (known issue 3)
* The album and artist lists are fetched once and cached. They were re-fetched from the server on every read, from XAML bindings, which meant a blocking round trip per rendered page. Favorites stay uncached, since they are edited from inside the app
* New signing certificate: the previous one expired on 05/22/25 and failed every build with APPX0108

#### 11/15/21 - v0.5

* Changed the app colors to the Windows 11 default ones
* Initial Favorites support (add track/album, view all favorites, remove an entry)

#### 04/23/21 - v0.4

* Added passing server and port in the command line (-s server -p port) for situations where autodiscovery on the local class C does not work

#### 04/18/21 - v0.3

* Added the _Private Networks_ capability to allow for some scenarios where local connectivity is limited
* First implementation of radio support, some images do not show for some reason, but navigation reliably works
* Fixed VisibilityOnHover helper to only show Play/Queue buttons when relevant
* Added the ability to start Fiona from the command line

#### 04/16/21 - v0.2

* Refactored all transport commands to BaseViewModel
* Improved speed entering AlbumDetails
* Updated the code to retrieve images from Discogs.com to allow for a proper UserAgent header

#### 04/15/21 - v0.1

* Apps
	* Cleaned up detection and navigation
	* Added search capabilities to Apps https://github.com/PaoloM/Fiona/issues/3
	* Added display of extra notes to some nodes
	* Added Play and Queue track when appropriate in text lists
* Foundational work to support system wide search, favorites, and radio
* Removed all traces of the FirstRun experience

#### 04/11/21 - prerelease 2

* Added a function to prettify the bio coming from Discogs.com. Still some work to do on links by ID (they will appear as numbers in the artist bio)
* Added LMS LAN autodiscovery. Now Fiona will scan your LAN to find an available Logitech Media Server install, no need to enter the IP of your sever anymore
* Added "Play all" in the Artist page
* Added "Shuffle all" in the Artist page
* Added "Add all to queue" in the Artist page
* Added "View on Discogs.com" in the Artist page
* Tweaked the colors for the light theme
* Added playlist shuffle control
* Added playlist repeat control
* Unified the transport control between navigation pages and Now Playing page

#### 03/29/21 - prerelease 1

* Created the supporting website at http://fionamusic.app
* Moved the link to the privacy policy to point to the new website

## Notes

* __Acrylic fix__ - Found a fix for Acrylic not appearing in the NavigationView on https://edi.wang/post/2018/10/9/fix-acrylicbrush-missing-navigationview-windows-10-17763 
* __Title bar customization__ - https://docs.microsoft.com/en-us/windows/uwp/design/shell/title-bar

## Attributions

* Portions of this code Copyright (c) 2010 Jeroen Vonk
* AlternatingRowsListView control by Ben Dewey
