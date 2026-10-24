using System.Drawing;
using GTA;
using GTA.Native;
using GTA.UI;

namespace GameEnvironment.Core
{
    internal sealed class InactiveNotice
    {
        private readonly TextElement _text = new TextElement(string.Empty, new PointF(20f, 522f), 0.32f,
            Color.FromArgb(235, 255, 255, 255), GTA.UI.Font.ChaletLondon, Alignment.Left, true, true);

        public void Draw(EnvironmentMode mode)
        {
            string text = TextFor(mode);
            if (text == null || !CanDraw()) return;
            _text.Caption = text;
            _text.Draw();
        }

        private static bool CanDraw() =>
            !Function.Call<bool>(Hash.GET_IS_LOADING_SCREEN_ACTIVE)
            && Function.Call<bool>(Hash.IS_PLAYER_PLAYING, Game.Player)
            && Function.Call<bool>(Hash.IS_SCREEN_FADED_IN)
            && !Function.Call<bool>(Hash.IS_PLAYER_SWITCH_IN_PROGRESS)
            && !Game.IsPaused
            && !Function.Call<bool>(Hash.IS_PAUSE_MENU_ACTIVE)
            && !Function.Call<bool>(Hash.IS_HUD_HIDDEN)
            && !Function.Call<bool>(Hash.IS_RADAR_HIDDEN)
            && !Function.Call<bool>(Hash.IS_CUTSCENE_ACTIVE);

        private static string TextFor(EnvironmentMode mode)
        {
            switch (mode)
            {
                case EnvironmentMode.StorySave:
                    return "~y~GameEnvironment inactive:~s~ story save.~n~Choose ~b~New Game~s~ to start the sterile world.";
                case EnvironmentMode.Disabled:
                    return "~r~GameEnvironment disabled:~s~ unsupported game version.";
                default:
                    return null;
            }
        }
    }
}
