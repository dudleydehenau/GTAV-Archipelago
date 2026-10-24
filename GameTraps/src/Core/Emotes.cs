using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;

namespace GameTraps.Core
{
    internal static class Emotes
    {
        private const string FallbackScenario = "WORLD_HUMAN_CHEERING";
        private static readonly HashSet<string> Missing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static void Play(Ped ped, Emote emote)
        {
            if (ped == null || !ped.Exists() || emote == null) return;
            Function.Call(Hash.CLEAR_PED_TASKS, ped);
            if (!string.IsNullOrEmpty(emote.Dict) && !string.IsNullOrEmpty(emote.Name) && LoadDict(emote.Dict))
            {
                Function.Call(Hash.TASK_PLAY_ANIM, ped, emote.Dict, emote.Name, 4f, -4f, -1, 1, 0f, false, false, false);
                return;
            }
            Function.Call(Hash.TASK_START_SCENARIO_IN_PLACE, ped, string.IsNullOrEmpty(emote.Scenario) ? FallbackScenario : emote.Scenario, 0, true);
        }

        public static void PlayRandom(Ped ped, IList<Emote> emotes)
        {
            if (emotes == null || emotes.Count == 0) return;
            Play(ped, Spawner.Pick(emotes));
        }

        private static bool LoadDict(string dict)
        {
            if (Missing.Contains(dict)) return false;
            if (!Function.Call<bool>(Hash.DOES_ANIM_DICT_EXIST, dict))
            {
                Missing.Add(dict);
                Log.Warn($"Animation dictionary '{dict}' does not exist, a scenario is used instead.");
                return false;
            }
            if (Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, dict)) return true;
            Function.Call(Hash.REQUEST_ANIM_DICT, dict);
            int deadline = Environment.TickCount + 1000;
            while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, dict) && Environment.TickCount < deadline) Script.Wait(0);
            return Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, dict);
        }
    }

    internal sealed class EntityTracker
    {
        private readonly List<Entity> _entities = new List<Entity>();

        public IReadOnlyList<Entity> All => _entities;

        public T Add<T>(T entity) where T : Entity
        {
            if (entity != null) _entities.Add(entity);
            return entity;
        }

        public void DeleteAll()
        {
            foreach (Entity entity in _entities)
            {
                if (entity == null || !entity.Exists()) continue;
                entity.AttachedBlip?.Delete();
                entity.Delete();
            }
            _entities.Clear();
        }

        public void Forget()
        {
            foreach (Entity entity in _entities)
            {
                if (entity == null || !entity.Exists()) continue;
                entity.AttachedBlip?.Delete();
                entity.MarkAsNoLongerNeeded();
            }
            _entities.Clear();
        }

        public void ReleaseAll()
        {
            foreach (Entity entity in _entities)
            {
                if (entity == null || !entity.Exists()) continue;
                entity.AttachedBlip?.Delete();
                if (entity is Ped ped && !ped.IsDead)
                {
                    ped.BlockPermanentEvents = false;
                    Function.Call(Hash.CLEAR_PED_TASKS, ped);
                    Function.Call(Hash.TASK_WANDER_STANDARD, ped, 10f, 10);
                }
                entity.MarkAsNoLongerNeeded();
            }
            _entities.Clear();
        }
    }

    internal static class ScreenEffects
    {
        private static int _clearAt = -1;

        public static void Start(string effect, bool loop)
        {
            if (string.IsNullOrEmpty(effect)) return;
            _clearAt = -1;
            Function.Call(Hash.ANIMPOSTFX_PLAY, effect, 0, loop);
        }

        public static void Stop(string effect)
        {
            if (!string.IsNullOrEmpty(effect)) Function.Call(Hash.ANIMPOSTFX_STOP, effect);
        }

        public static void FadeOut(string loop, string outro, int clearAfter)
        {
            Stop(loop);
            if (!string.IsNullOrEmpty(outro)) Function.Call(Hash.ANIMPOSTFX_PLAY, outro, 0, false);
            _clearAt = Game.GameTime + clearAfter;
        }

        public static void ClearAll()
        {
            _clearAt = -1;
            Function.Call(Hash.ANIMPOSTFX_STOP_ALL);
            Function.Call(Hash.STOP_GAMEPLAY_CAM_SHAKING, true);
            Function.Call(Hash.CLEAR_TIMECYCLE_MODIFIER);
        }

        public static void Process()
        {
            if (_clearAt >= 0 && Game.GameTime >= _clearAt) ClearAll();
        }

        public static void Light(float strength)
        {
            if (strength <= 0f) return;
            _clearAt = -1;
            Function.Call(Hash.SET_TIMECYCLE_MODIFIER, "drug_flying_base");
            Function.Call(Hash.SET_TIMECYCLE_MODIFIER_STRENGTH, strength);
            Function.Call(Hash.SHAKE_GAMEPLAY_CAM, "DRUNK_SHAKE", 0.3f);
        }

        public static void Drunk(bool on)
        {
            if (on)
            {
                Function.Call(Hash.SHAKE_GAMEPLAY_CAM, "DRUNK_SHAKE", 1.5f);
                Function.Call(Hash.SET_TIMECYCLE_MODIFIER, "drug_flying_base");
                Function.Call(Hash.SET_TIMECYCLE_MODIFIER_STRENGTH, 1f);
            }
            else
            {
                Function.Call(Hash.STOP_GAMEPLAY_CAM_SHAKING, true);
                Function.Call(Hash.CLEAR_TIMECYCLE_MODIFIER);
            }
        }
    }
}
