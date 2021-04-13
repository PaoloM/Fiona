![logo](https://github.com/PaoloM/Fiona/blob/main/Original%20assets/Fiona%20logo%20-%20small.png)

# Fiona

[![Build status](https://build.appcenter.ms/v0.1/apps/d7f40dde-1410-4946-82eb-9b5c207f84a0/branches/main/badge)](https://appcenter.ms)
 
A [Squeezebox/Logitech Media Server](https://www.mysqueezebox.com/download) controller for Windows 10

## Current features

* Autodiscovery and connection to your Logitech Media Server/Squeezebox
* Navigate your music library by album and artist
* Individual queues for all your connected players
* Now Playing page with artist images
* 3rd party apps/plugins (in preview)

## Roadmap/backlog

* Full 3rd party app/plugin support
* Radio support
* Queue management
* Favorites management
* More animations and transitions

## Known issues

1. App navigation is incomplete and might suddenly crash the app
2. App text entry is not implemented (i.e. search in apps cannot be executed) https://github.com/PaoloM/Fiona/issues/3
3. Some info in the artis profiles are rendered as numbers instead of text https://github.com/PaoloM/Fiona/issues/4
4. The personalization setting "Windows default" sets the colors to the app dark mode, not the Windows' one (is it really an issue?)
5. Images transitions in the Now Playing page are not animated
6. Navigating back from album/artist details to the main lists does not bring you back to the previous scroll location https://github.com/PaoloM/Fiona/issues/5
7. Updating the queue with the same number of entries as the existing one does not update the queue visuals https://github.com/PaoloM/Fiona/issues/6

## Release notes

#### 04/11/2021

* Added a function to prettify the bio coming from Discogs.com. Still some work to do on links by ID (they will appear as numbers in the artist bio)
* Added LMS LAN autodiscovery. Now Fiona will scan your LAN to find an available Logitech Media Server install, no more need to enter the IP of your sever anymore
* Added "Play all" in the Artist page
* Added "Shuffle all" in the Artist page
* Added "Add all to queue" in the Artist page
* Added "View on Discogs.com" in the Artist page
* Tweaked the colors for the light theme
* Added playlist shuffle control
* Added playlist repeat control
* Unified the transport control between navigation pages and Now Playing page

#### 03/29/2021

* Created the supporting website at http://fionamusic.app
* Moved the link to the privacy policy to point to the new website

## Notes

#### Acrylic fix

Found a fix for Acrylic not appearing in the NavigationView on https://edi.wang/post/2018/10/9/fix-acrylicbrush-missing-navigationview-windows-10-17763 

#### Title bar customization

https://docs.microsoft.com/en-us/windows/uwp/design/shell/title-bar

## Attributions

 Portions of this code Copyright (c) 2010 Jeroen Vonk