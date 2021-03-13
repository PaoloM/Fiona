# Fiona
 A Squeezebox/Logitech Media Server controller for Windows 10

 Portion of this code Copyright (c) 2010 Jeroen Vonk

[![Build status](https://build.appcenter.ms/v0.1/apps/c1bf4304-517e-47af-8e39-5e78f367f15b/branches/main/badge)](https://appcenter.ms)

 # Messages

## Get server status

    {
      "id": 1,
      "method": "slim.request",
      "params": [
        "",
        [
          "serverstatus" 
        ]
      ]
    }

## Get all albums

    {
      "id": 1,
      "method": "slim.request",
      "params": [
        "",
        [
          "albums",
		  "0"
        ]
      ]
    }

## Get all artists

    {
      "id": 1,
      "method": "slim.request",
      "params": [
        "",
        [
          "artists",
	      "0"
        ]
      ]
    }

## Get players

    {
      "id": 1,
      "method": "slim.request",
      "params": [
        "",
        [
          "players", 
          "0"
        ]
      ]
    }

## Get player status

    {
      "id": 1,
      "method": "slim.request",
      "params": [
        "b8:27:eb:ac:f0:6b",
        [
          "status"
        ]
      ]
    }

## Get songs from album

    {
      "id": 1,
      "method": "slim.request",
      "params": [
        "b8:27:eb:ac:f0:6b",
        [
          "songs",
          "0",
          "100000",
          "album_id:3200"
        ]
      ]
    }

## Get songinfo

    {
      "id": 1,
      "method": "slim.request",
      "params": [
        "b8:27:eb:ac:f0:6b",
        [
          "songinfo",
		  "0",
		  "100",
		  "track_id:-176729260"
        ]
      ]
    }

## Get mixer volume

    {
      "id": 1,
      "method": "slim.request",
      "params": [
        "b8:27:eb:ac:f0:6b",
        [
          "mixer",
		  "volume",
		  "?"
        ]
      ]
    }

## Set mixer volume

    {
      "id": 1,
      "method": "slim.request",
      "params": [
        "b8:27:eb:ac:f0:6b",
        [
          "mixer",
		  "volume",
		  "30"
        ]
      ]
    }

# Notes

## Acrylic fix

Found a fix for Acrylic not appearing in the NavigationView on https://edi.wang/post/2018/10/9/fix-acrylicbrush-missing-navigationview-windows-10-17763 

## Title bar customization

https://docs.microsoft.com/en-us/windows/uwp/design/shell/title-bar
