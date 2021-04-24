![logo](https://github.com/PaoloM/Fiona/blob/main/Original%20assets/Fiona%20logo%20-%20small.png)

# Fiona

[![Build status](https://build.appcenter.ms/v0.1/apps/d7f40dde-1410-4946-82eb-9b5c207f84a0/branches/main/badge)](https://appcenter.ms)
 
A [Squeezebox/Logitech Media Server](https://www.mysqueezebox.com/download) controller for Windows 10.

![](Original%20assets/Screenshots/v0.4-prealpha/Screenshot%202021-04-18%20191217.png)

## What's new?

* Added a whole lot of screenshots for [v0.4](v0.4-prealpha-screenshots.md)

## Current features

* Autodiscover and connect to your Logitech Media Server/Squeezebox
* Navigate your music library by album and artist
* Individual queues for all your connected players
* Now Playing page with artist images
* 3rd party apps/plugins almost completely supported
* Radio support

## Roadmap/backlog

* Full 3rd party app/plugin support
* Queue management
* Favorites management
* More animations and transitions

## Tested plugins

These plugins have been tested and are working per spec:

* Spotty
* Band's Camp

## Known issues

1. Some info in the artis profiles are rendered as numbers instead of text https://github.com/PaoloM/Fiona/issues/4
1. The personalization setting "Windows default" sets the colors to the app dark mode, not the Windows' one (is it really an issue?)
1. Images transitions in the Now Playing page are not animated
1. Navigating back from album/artist details to the main lists does not bring you back to the previous scroll location https://github.com/PaoloM/Fiona/issues/5
1. Updating the queue with the same number of entries as the existing one does not update the queue visuals https://github.com/PaoloM/Fiona/issues/6

## Release notes

#### 04/23/21

* Added passing server and port in the command line (-s server -p port) for situations where autodiscovery on the local class C does not work

#### 04/18/21

* Added the _Private Networks_ capability to allow for some scenarios where local connectivity is limited
* First implementation of radio support, some images do not show for some reason, but navigation reliably works
* Fixed VisibilityOnHover helper to only show Play/Queue buttons when relevant
* Added the ability to start Fiona from the command line

#### 04/16/21

* Refactored all transport commands to BaseViewModel
* Improved speed entering AlbumDetails
* Updated the code to retrieve images from Discogs.com to allow for a proper UserAgent header

#### 04/15/21

* Apps
	* Cleaned up detection and navigation
	* Added search capabilities to Apps https://github.com/PaoloM/Fiona/issues/3
	* Added display of extra notes to some nodes
	* Added Play and Queue track when appropriate in text lists
* Foundational work to support system wide search, favorites, and radio
* Removed all traces of the FirstRun experience

#### 04/11/21

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

#### 03/29/21

* Created the supporting website at http://fionamusic.app
* Moved the link to the privacy policy to point to the new website

## Notes

* __Acrylic fix__ - Found a fix for Acrylic not appearing in the NavigationView on https://edi.wang/post/2018/10/9/fix-acrylicbrush-missing-navigationview-windows-10-17763 
* __Title bar customization__ - https://docs.microsoft.com/en-us/windows/uwp/design/shell/title-bar

## Attributions

* Portions of this code Copyright (c) 2010 Jeroen Vonk
* AlternatingRowsListView control by Ben Dewey
