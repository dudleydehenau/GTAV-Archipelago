using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;

namespace GameArchipelago.Core
{
    internal sealed class Notifier
    {
        private const int MaxQueued = 24;
        private const string Contact = "Archipelago";
        private const string Picture = "CHAR_DEFAULT";
        private readonly Queue<Entry> _queue = new Queue<Entry>();
        private readonly LinkedList<string> _log = new LinkedList<string>();
        private readonly int _delay;
        private readonly int _logSize;
        private int _nextAt;
        private int _dropped;

        public Notifier(int delay, int logSize)
        {
            _delay = delay;
            _logSize = logSize;
        }

        public IEnumerable<string> History => _log;

        private sealed class Entry
        {
            public string Sender;
            public string Subject;
            public string Text;
        }

        public void Post(string text, bool important = false) => Enqueue(null, null, text, important);

        public void Message(string sender, string subject, string text, bool important = false) =>
            Enqueue(string.IsNullOrWhiteSpace(sender) ? Contact : sender, subject ?? string.Empty, text, important);

        private void Enqueue(string sender, string subject, string text, bool important)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            if (_queue.Count >= MaxQueued && !important)
            {
                _dropped++;
                return;
            }
            _queue.Enqueue(new Entry { Sender = sender, Subject = subject, Text = text });
        }

        public void Remember(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            _log.AddFirst($"{DateTime.Now:HH:mm} {Clean(text)}");
            while (_log.Count > _logSize) _log.RemoveLast();
        }

        public void Process()
        {
            if (Game.GameTime < _nextAt) return;
            if (_queue.Count == 0)
            {
                if (_dropped > 0)
                {
                    ShowMessage(Contact, "More messages", $"{_dropped} more. Read them all in F6 > Archipelago log.");
                    _dropped = 0;
                    _nextAt = Game.GameTime + _delay;
                }
                return;
            }
            if (Function.Call<bool>(Hash.GET_IS_LOADING_SCREEN_ACTIVE) || !GTA.UI.Screen.IsFadedIn) return;
            Entry entry = _queue.Dequeue();
            if (entry.Sender == null) Show(entry.Text);
            else ShowMessage(entry.Sender, entry.Subject, entry.Text);
            _nextAt = Game.GameTime + _delay;
        }

        public static void ShowMessage(string sender, string subject, string text)
        {
            try
            {
                Function.Call(Hash.REQUEST_STREAMED_TEXTURE_DICT, Picture, false);
                bool picture = Function.Call<bool>(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED, Picture);
                Function.Call(Hash.BEGIN_TEXT_COMMAND_THEFEED_POST, "STRING");
                Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, Trim(text, 98));
                if (picture)
                    Function.Call(Hash.END_TEXT_COMMAND_THEFEED_POST_MESSAGETEXT, Picture, Picture, false, 1, Trim(Escape(sender), 40), Trim(subject, 60));
                Function.Call(Hash.END_TEXT_COMMAND_THEFEED_POST_TICKER, false, true);
            }
            catch (Exception ex)
            {
                Log.Error("Could not show a message.", ex);
            }
        }

        private static string Trim(string text, int max) => string.IsNullOrEmpty(text) || text.Length <= max ? text ?? string.Empty : text.Substring(0, max - 3) + "...";

        public static void Show(string text)
        {
            try
            {
                GTA.UI.Notification.PostTicker("~h~AP~h~  " + text, false);
            }
            catch (Exception ex)
            {
                Log.Error("Could not show a notification.", ex);
            }
        }

        public static string Clean(string text)
        {
            var result = new System.Text.StringBuilder();
            bool code = false;
            foreach (char c in text)
            {
                if (c == '~')
                {
                    code = !code;
                    continue;
                }
                if (!code) result.Append(c);
            }
            return result.ToString();
        }

        public static string Escape(string text) => string.IsNullOrEmpty(text) ? string.Empty : text.Replace("~", "-");
    }
}
