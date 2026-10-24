using System;
using System.Globalization;
using System.Text.RegularExpressions;
using GTA;
using GTA.Native;

namespace GameTraps.Core
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

        public static void Feed(string icon, string sender, string subject, string text)
        {
            Function.Call(Hash.BEGIN_TEXT_COMMAND_THEFEED_POST, "CELL_EMAIL_BCON");
            for (int i = 0; i < text.Length; i += 90)
                Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, text.Substring(i, Math.Min(90, text.Length - i)));
            Function.Call(Hash.END_TEXT_COMMAND_THEFEED_POST_MESSAGETEXT, icon, icon, false, 1, sender, subject);
            Function.Call(Hash.END_TEXT_COMMAND_THEFEED_POST_TICKER, false, true);
        }
    }
}
