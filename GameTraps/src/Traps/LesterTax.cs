using System;
using GameTraps.Core;
using GTA;
using GTA.Native;

namespace GameTraps.Traps
{
    internal sealed class LesterTax : Trap
    {
        private static readonly string[] English =
        {
            "Took ${0} from your account. Consulting fee. You'd have blown it on a Faggio anyway.",
            "${0}, my cut. Like on every score. You didn't read the fine print, did you?",
            "That ramp outside my house doesn't pay for itself. ${0}. Thanks, buddy.",
            "Moved ${0} into BAWSAQ for you. Betta Pharmaceuticals. Trust me. Don't check.",
            "Surveillance equipment isn't free. ${0}. Nice shirt today, by the way.",
            "Call it a stupidity tax. ${0}. You contribute a lot.",
            "${0} for the hacking. What hacking? Exactly. That's how good I am.",
        };

        public LesterTax(TrapsConfig config) : base(config)
        {
        }

        private LesterTaxConfig Settings => Config.LesterTax;
        public override string Id => TrapIds.LesterTax;
        public override string Title => "Lester's cut";
        public override string Subtitle => "Consulting fees.";
        public override bool Enabled => Settings.Enabled;

        public override void Start()
        {
            int money = Game.Player.Money;
            int loss = (int)Math.Min(money, Math.Max(Settings.Minimum, (long)money * Math.Max(0, Settings.Percent) / 100));
            if (loss <= 0) return;
            loss = Wallet.Take(loss);
            if (loss <= 0) return;
            Log.Info($"Lester took {loss} (was {money}).");
            string amount = loss.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture);
            string text = string.Format(Spawner.Pick(English), amount);
            Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "Text_Arrive_Tone", "Phone_SoundSet_Default", true);
            GameText.Feed("CHAR_LESTER", "Lester", "Invoice", text);
        }

        public override bool Update() => false;

        public override void Stop(bool interrupted)
        {
        }
    }
}
