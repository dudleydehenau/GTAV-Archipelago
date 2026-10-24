using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;

namespace GameTraps.Core
{
    internal static class TrevorLine
    {
        private const int MaxLines = 70;
        private static readonly Model TrevorModel = new Model("player_two");

        private const int RingTime = 2500;
        private const int CallTimeout = 30000;
        private const int MinCallTime = 1500;
        private static int _callEndsAt = -1;
        private static int _callStartedAt;
        private static int _quietSince = -1;
        private const int QuietTime = 1200;

        public static bool CallActive => _callEndsAt >= 0;

        public static void Play(DialogueLine line)
        {
            if (line == null || !line.Enabled) return;
            try
            {
                if (!LoadBlock(line))
                {
                    Log.Warn($"Text block {line.Block} could not be loaded, Trevor's line skipped.");
                    return;
                }
                bool isTrevor = Game.Player.Character.Model.Hash == TrevorModel.Hash;
                string firstSubtitle = Label(line.Root + "_1");
                if (!isTrevor) Ring();
                int lines = Build(line, isTrevor ? Game.Player.Character : null);
                if (lines == 0)
                {
                    if (firstSubtitle != null) GTA.UI.Screen.ShowSubtitle("~o~Trevor:~s~ " + GameText.Clean(firstSubtitle), 3500);
                    return;
                }
                bool interrupt = Flag(line.Root + "IF", 0);
                if (isTrevor) Function.Call(Hash.START_SCRIPT_CONVERSATION, true, true, false, interrupt);
                else
                {
                    Function.Call(Hash.TASK_USE_MOBILE_PHONE, Game.Player.Character, true);
                    Function.Call(Hash.START_SCRIPT_PHONE_CONVERSATION, true, true);
                    _callStartedAt = Game.GameTime;
                    _quietSince = -1;
                    _callEndsAt = Game.GameTime + CallTimeout;
                }
                Log.Info($"Trevor's line started ({lines} line(s), {(isTrevor ? "in person" : "phone call")}).");
            }
            catch (Exception ex)
            {
                Log.Error("Trevor's line could not be played.", ex);
            }
        }

        private static readonly int[] PhoneControls = { 27, 172, 173, 174, 175, 176, 177, 178, 179, 180, 181, 202 };

        public static void Tick()
        {
            if (_callEndsAt < 0) return;
            LockPhone();
            if (Game.GameTime < _callStartedAt + MinCallTime) return;
            if (Game.GameTime >= _callEndsAt)
            {
                HangUp();
                return;
            }
            if (Function.Call<bool>(Hash.IS_SCRIPTED_CONVERSATION_ONGOING))
            {
                _quietSince = -1;
                return;
            }
            if (_quietSince < 0) _quietSince = Game.GameTime;
            if (Game.GameTime - _quietSince >= QuietTime) HangUp(false);
        }

        public static void HangUp() => HangUp(true);

        private static void HangUp(bool cut)
        {
            if (_callEndsAt < 0) return;
            _callEndsAt = -1;
            _quietSince = -1;
            if (cut) Function.Call(Hash.STOP_SCRIPTED_CONVERSATION, false);
            Function.Call(Hash.TASK_USE_MOBILE_PHONE, Game.Player.Character, false);
            Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "Hang_Up", "Phone_SoundSet_Michael", true);
            Log.Info("Trevor hung up.");
        }

        private static void Ring()
        {
            Ped player = Game.Player.Character;
            GameText.Feed("CHAR_TREVOR", "Trevor", "Incoming call", "Trevor is calling...");
            Function.Call(Hash.PLAY_PED_RINGTONE, "Remote_Ring", player, true);
            int until = Game.GameTime + RingTime;
            while (Game.GameTime < until)
            {
                LockPhone();
                Script.Wait(0);
            }
            Function.Call(Hash.STOP_PED_RINGTONE, player);
            Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "Menu_Accept", "Phone_SoundSet_Default", true);
        }

        private static void LockPhone()
        {
            foreach (int control in PhoneControls) Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, control, true);
        }

        private static bool LoadBlock(DialogueLine line)
        {
            if (Function.Call<bool>(Hash.HAS_THIS_ADDITIONAL_TEXT_LOADED, line.Block, line.Slot)) return true;
            Function.Call(Hash.REQUEST_ADDITIONAL_TEXT, line.Block, line.Slot);
            int deadline = Environment.TickCount + 3000;
            while (!Function.Call<bool>(Hash.HAS_THIS_ADDITIONAL_TEXT_LOADED, line.Block, line.Slot) && Environment.TickCount < deadline) Script.Wait(0);
            return Function.Call<bool>(Hash.HAS_THIS_ADDITIONAL_TEXT_LOADED, line.Block, line.Slot);
        }

        private static int Build(DialogueLine line, Ped speakerPed)
        {
            string speakers = Label(line.Root + "SL") ?? string.Empty;
            string flags = Label(line.Root + "LF") ?? string.Empty;
            var registered = new HashSet<int>();
            Function.Call(Hash.CREATE_NEW_SCRIPTED_CONVERSATION);
            int count = 0;
            for (int i = 0; i < MaxLines; i++)
            {
                string subtitle = line.Root + "_" + (i + 1);
                string audio = Label(subtitle + "A");
                if (audio == null) break;
                int speaker = Code(speakers, i * 3, line.Speaker);
                int listener = Code(speakers, i * 3 + 1, -1);
                int volume = Code(speakers, i * 3 + 2, 0);
                if (speaker >= 0 && registered.Add(speaker))
                    Function.Call(Hash.ADD_PED_TO_CONVERSATION, speaker, speakerPed == null ? 0 : speakerPed.Handle, line.Voice);
                Function.Call(Hash.ADD_LINE_TO_CONVERSATION, speaker, audio,
                    Function.Call<bool>(Hash.DOES_TEXT_LABEL_EXIST, subtitle) ? subtitle : audio,
                    listener, volume, false,
                    Bit(flags, i * 7), Bit(flags, i * 7 + 1), Bit(flags, i * 7 + 2), Audibility(flags, i * 7 + 3),
                    Bit(flags, i * 7 + 4), Bit(flags, i * 7 + 5), Bit(flags, i * 7 + 6));
                Log.Info($"Trevor's line {i + 1}: audio '{audio}', speaker {speaker}, listener {listener}, volume {volume}.");
                count++;
            }
            return count;
        }

        private static string Label(string label)
        {
            if (!Function.Call<bool>(Hash.DOES_TEXT_LABEL_EXIST, label)) return null;
            string text = Function.Call<string>(Hash.GET_FILENAME_FOR_AUDIO_CONVERSATION, label);
            return string.IsNullOrEmpty(text) ? null : text;
        }

        private static bool Flag(string label, int index)
        {
            string text = Label(label);
            return text != null && index < text.Length && text[index] != '0';
        }

        private static bool Bit(string text, int index) => index < text.Length && text[index] != '0';

        private static int Audibility(string text, int index)
        {
            if (index >= text.Length) return 0;
            switch (text[index])
            {
                case '0':
                case '1':
                    return 1;
                case '2':
                    return 2;
                case '3':
                    return 3;
                default:
                    return 0;
            }
        }

        private static int Code(string text, int index, int fallback)
        {
            if (index >= text.Length) return fallback;
            char c = text[index];
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'A' && c <= 'Z') return c - 'A' + 10;
            return -1;
        }
    }
}
