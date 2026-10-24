using System;
using System.Globalization;
using System.Text.RegularExpressions;
using GTA;
using GTA.Native;

namespace GameEnvironment.Core
{
    internal static class GameText
    {
        private static readonly Regex FormatCodes = new Regex("~[a-zA-Z0-9_]*~", RegexOptions.Compiled);

        public static string Clean(string text) => string.IsNullOrEmpty(text) ? text : FormatCodes.Replace(text, string.Empty).Trim();

        public static bool Exists(string label)
        {
            if (string.IsNullOrEmpty(label)) return false;
            try
            {
                return Function.Call<bool>(Hash.DOES_TEXT_LABEL_EXIST, label);
            }
            catch
            {
                return false;
            }
        }

        public static string Get(string label, string fallback = null)
        {
            try
            {
                if (Exists(label))
                {
                    string text = Game.GetLocalizedString(label);
                    if (!string.IsNullOrWhiteSpace(text)) return Clean(text);
                }
            }
            catch (Exception)
            {
            }
            return fallback ?? label ?? string.Empty;
        }

        public static string Money(int amount)
        {
            string digits = amount.ToString("#,0", CultureInfo.InvariantCulture);
            return "$" + digits;
        }
    }
}
