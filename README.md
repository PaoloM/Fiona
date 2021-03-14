# Fiona
 A Squeezebox/Logitech Media Server controller for Windows 10



[![Build status](https://build.appcenter.ms/v0.1/apps/c1bf4304-517e-47af-8e39-5e78f367f15b/branches/main/badge)](https://appcenter.ms)

# Notes

## API Keys

Create a __APIKeys.local.cs__ class in the Fiona.Core/Helpers folder

    namespace Fiona.Core.Helpers
    {
        public static partial class APIKeys
        {
            static APIKeys()
            {
                AppCenter = "";
                LMSServerName = "";
                LMSServerPort = 9000;
                DiscogsConsumerKey = "";
                DiscogsConsumerSecret = "";
            }
        }
    }

And add your keys.

## Acrylic fix

Found a fix for Acrylic not appearing in the NavigationView on https://edi.wang/post/2018/10/9/fix-acrylicbrush-missing-navigationview-windows-10-17763 

## Title bar customization

https://docs.microsoft.com/en-us/windows/uwp/design/shell/title-bar

# Attributions

 Portions of this code Copyright (c) 2010 Jeroen Vonk
