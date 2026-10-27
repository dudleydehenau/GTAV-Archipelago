using System;
using GTA.Math;
using GTA.Native;

namespace MissionLauncher.Core
{
    internal sealed class GameGlobals
    {
        private const int MissionStride = 34;
        private GlobalsConfig _cfg;
        private int _replayControllerHash;

        public GameGlobals()
        {
            _cfg = GlobalsConfig.Candidates(GameBuild.Number)[0];
        }

        public bool Available { get; private set; }
        public bool CompletionAvailable { get; private set; }
        public string Status { get; private set; } = "not validated";
        public GlobalsConfig Config => _cfg;

        public void Validate()
        {
            Available = false;
            string first = null;
            foreach (GlobalsConfig candidate in GlobalsConfig.Candidates(GameBuild.Number))
            {
                string error = TryProfile(candidate);
                if (error == null) return;
                first = first ?? error;
            }
            Status = $"game build {GameBuild.Describe()}: {first}";
        }

        private string TryProfile(GlobalsConfig candidate)
        {
            try
            {
                int definitions = Read(candidate.StatDefinitions);
                int active = Read(candidate.ActiveStats);
                int missions = Read(candidate.MissionTable);
                if (definitions != candidate.StatDefinitionCount || active != candidate.ActiveStatMax || missions < 90 || missions > 200)
                    return $"unexpected array sizes for build {candidate.Build} (stats={definitions}/{candidate.StatDefinitionCount}, active={active}/{candidate.ActiveStatMax}, missions={missions}). Unsupported game build?";
                _cfg = candidate;
                _replayControllerHash = Function.Call<int>(Hash.GET_HASH_KEY, "mission_repeat_controller");
                Available = true;
                int storyRecords = Read(_cfg.StoryCompletion);
                int strangerRecords = Read(_cfg.StrangersCompletion);
                CompletionAvailable = storyRecords == _cfg.StoryCompletionCount && strangerRecords == _cfg.StrangersCompletionCount;
                string completion = CompletionAvailable
                    ? "OK"
                    : $"unexpected ({storyRecords}/{_cfg.StoryCompletionCount}, {strangerRecords}/{_cfg.StrangersCompletionCount})";
                Status = $"OK (game build {GameBuild.Describe()}, addresses {_cfg.Build}, {missions} missions, {definitions} stats, completion records {completion})";
                return null;
            }
            catch (Exception ex)
            {
                return "read error: " + ex.Message;
            }
        }

        public int MissionCount() => Read(_cfg.MissionTable);

        public string MissionCode(int index)
        {
            try
            {
                string code = GlobalVariable.Get(MissionField(index, 8)).Read<string>();
                return string.IsNullOrEmpty(code) ? null : code;
            }
            catch
            {
                return null;
            }
        }

        public int MissionScriptHash(int index) => Read(MissionField(index, 6));

        public bool IsStoryReplayable(int index) => (Read(MissionField(index, 15)) & (1 << 5)) == 0;

        public int CompletionOrder(int type, int index)
        {
            if (!CompletionAvailable || index < 0) return -1;
            bool story = type == 1;
            int count = story ? _cfg.StoryCompletionCount : _cfg.StrangersCompletionCount;
            if (index >= count) return -1;
            int root = story ? _cfg.StoryCompletion : _cfg.StrangersCompletion;
            try
            {
                return Read(root + 1 + index * 6 + 3);
            }
            catch
            {
                return -1;
            }
        }

        public bool IsReplayControllerRunning() =>
            Function.Call<int>(Hash.GET_NUMBER_OF_THREADS_RUNNING_THE_SCRIPT_WITH_THIS_HASH, _replayControllerHash) > 0;

        public bool IsReplayRequested() => (Read(_cfg.ReplayFlags) & 1) != 0;

        public void ReadReplayRequest(out int type, out int index)
        {
            index = Read(_cfg.ReplayRequest);
            type = Read(_cfg.ReplayRequest + 1);
        }

        public void RequestReplay(int type, int index, Vector3 returnPosition)
        {
            Write(_cfg.ReplayRequest, index);
            Write(_cfg.ReplayRequest + 1, type);
            GlobalVariable.Get(_cfg.ReplayReturnPosition).Write(returnPosition.X);
            GlobalVariable.Get(_cfg.ReplayReturnPosition + 1).Write(returnPosition.Y);
            GlobalVariable.Get(_cfg.ReplayReturnPosition + 2).Write(returnPosition.Z);
            Write(_cfg.ReplayFlags, Read(_cfg.ReplayFlags) | 1);
        }

        public string ReadFailReason()
        {
            try
            {
                return GlobalVariable.Get(_cfg.FailReason).Read<string>() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public void ClearFailReason()
        {
            try
            {
                GlobalVariable.Get(_cfg.FailReason).WriteString(string.Empty, 16);
            }
            catch
            {
            }
        }

        public bool MissionPassedFlag() => Read(_cfg.MissionPassedFlag) != 0;

        public int RampageHeader => _cfg.RampageRecords > 0 ? Read(_cfg.RampageRecords) : -1;

        public void ClearRampageRecords()
        {
            if (_cfg.RampageRecords <= 0 || _cfg.RampageCount <= 0 || Read(_cfg.RampageRecords) != _cfg.RampageCount) return;
            for (int i = 0; i < _cfg.RampageCount * 2; i++) Write(_cfg.RampageRecords + 1 + i, 0);
        }

        public int[] RampageRecords()
        {
            if (_cfg.RampageRecords <= 0 || _cfg.RampageCount <= 0 || Read(_cfg.RampageRecords) != _cfg.RampageCount) return null;
            var values = new int[_cfg.RampageCount * 2];
            for (int i = 0; i < values.Length; i++) values[i] = Read(_cfg.RampageRecords + 1 + i);
            return values;
        }

        public int ActiveStatCount() => Math.Min(Read(_cfg.ActiveStatCount), _cfg.ActiveStatMax);
        public int ActiveStatBase(int slot) => _cfg.ActiveStats + 1 + slot * 9;
        public int StatDefinitionBase(int statId) => _cfg.StatDefinitions + 1 + statId * 13;
        public int StatDefinitionCount => _cfg.StatDefinitionCount;

        public static int Read(int index) => GlobalVariable.Get(index).Read<int>();
        public static float ReadFloat(int index) => GlobalVariable.Get(index).Read<float>();
        private static void Write(int index, int value) => GlobalVariable.Get(index).Write(value);

        private int MissionField(int index, int field) => _cfg.MissionTable + 1 + index * MissionStride + field;
    }
}
