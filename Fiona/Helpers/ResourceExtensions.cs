using System;
using System.Runtime.InteropServices;

using Windows.ApplicationModel.Resources;

namespace Fiona.Helpers
{
    internal static class ResourceExtensions
    {
        private static ResourceLoader _resLoader = new ResourceLoader();

        public static string GetLocalized(this string resourceKey)
        {
            string r = _resLoader.GetString(resourceKey);
            return r;
        }
    }
}
