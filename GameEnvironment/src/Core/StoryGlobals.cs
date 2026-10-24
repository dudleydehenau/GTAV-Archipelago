using System;
using System.Text;
using GTA;
using GTA.Native;

namespace GameEnvironment.Core
{
    internal enum SaveKind
    {
        Unknown,
        NewGame,
        Sterile,
        Story,
    }

    internal sealed class StoryGlobals
    {
        private const int MissionStride = 34;
        private const int CompletionStride = 6;
        private const int StrandStride = 3;
        private const int StrandActive = 1 << 1;
        private const int StrandComplete = 1 << 2;
        private const int NoMission = 15;
        private const int CallCount = 136;
        private const int TextCount = 198;
        private const int EmailCount = 650;
        private const int ChatCount = 764;
        private const int ConversationCount = 866;
        private const int QueuedChatCount = 910;
        private const int CharacterPriority = 919;
        private readonly StoryGlobalsConfig _cfg;
        private int _repeatControllerHash;
        private int _prologueHash;

        public StoryGlobals(StoryGlobalsConfig cfg)
        {
            _cfg = cfg;
        }

        public bool Available { get; private set; }
        public string Status { get; private set; } = "not validated";
        public int PrologueIndex { get; private set; } = -1;
        public int PrologueScriptHash => _prologueHash;

        public void Validate()
        {
            Available = false;
            try
            {
                int flags = Read(_cfg.FlowFlags);
                int strands = Read(_cfg.Strands);
                int characters = Read(_cfg.CharacterAvailable);
                int completion = Read(_cfg.StoryCompletion);
                if (flags != _cfg.FlowFlagCount || strands != _cfg.StrandCount || characters != 3 || completion != _cfg.StoryCompletionCount)
                {
                    Status = $"unexpected array sizes (flow flags={flags}/{_cfg.FlowFlagCount}, strands={strands}/{_cfg.StrandCount}, characters={characters}/3, completion={completion}/{_cfg.StoryCompletionCount}). Unsupported game build?";
                    return;
                }
                int calls = Read(_cfg.Communications);
                int priorities = Read(_cfg.Communications + CharacterPriority);
                if (calls != 9 || priorities != 3)
                {
                    Status = $"unexpected communication queue (calls={calls}/9, priorities={priorities}/3). Unsupported game build?";
                    return;
                }
                _repeatControllerHash = unchecked((int)StringHash.AtStringHash("mission_repeat_controller", 0u));
                _prologueHash = unchecked((int)StringHash.AtStringHash("prologue1", 0u));
                PrologueIndex = FindMissionByScript(_prologueHash);
                Available = true;
                Status = $"OK (prologue = mission index {PrologueIndex})";
            }
            catch (Exception ex)
            {
                Status = "read error: " + ex.Message;
            }
        }

        public SaveKind Classify()
        {
            if (!FlowActive || !AnyStrandActive()) return SaveKind.Unknown;
            int completed = CompletedStoryMissions();
            if (completed == 0 && FlowHalted) return SaveKind.Sterile;
            if (completed == 0 && !FlowHalted && !IsPrologueCompleted) return SaveKind.NewGame;
            return SaveKind.Story;
        }

        public string Describe()
        {
            var sb = new StringBuilder();
            sb.Append($"flow={Read(_cfg.FlowState)} halted={Read(_cfg.FlowState + 1)} strands=");
            int active = 0;
            for (int i = 0; i < _cfg.StrandCount; i++)
                if ((StrandBits(i) & StrandActive) != 0) active++;
            sb.Append(active);
            sb.Append($" main strand bits={StrandBits(_cfg.MainStrand)} completed={CompletedStoryMissions()} prologue done={IsPrologueCompleted}");
            sb.Append(" introduced=");
            for (int c = 0; c < 3; c++) sb.Append(IsCharacterIntroduced((PlayableCharacter)c) ? '1' : '0');
            sb.Append(" available=");
            for (int c = 0; c < 3; c++) sb.Append(IsCharacterAvailable((PlayableCharacter)c) ? '1' : '0');
            sb.Append($" running mission={RunningMission} prologue thread={IsScriptRunning(_prologueHash)}");
            sb.Append($" flow busy={Read(_cfg.FlowBusy)} switch blocked={Read(_cfg.SwitchBlocked)}");
            sb.Append($" mission type={Read(_cfg.MissionType)} candidate={Read(_cfg.MissionCandidate)} selector disabled={Read(_cfg.SelectorDisabled)}");
            sb.Append($" comms={PendingCommunications()} priority=");
            for (int c = 0; c < 3; c++) sb.Append(Read(_cfg.Communications + CharacterPriority + 1 + c));
            return sb.ToString();
        }

        public bool FlowActive => Read(_cfg.FlowState) != 0;

        public bool FlowHalted
        {
            get => Read(_cfg.FlowState + 1) != 0;
            set => Write(_cfg.FlowState + 1, value ? 1 : 0);
        }

        public int RunningMission
        {
            get => Read(_cfg.RunningMission);
            set => Write(_cfg.RunningMission, value);
        }

        public string DescribeWheel()
        {
            var sb = new StringBuilder("wheel globals:");
            foreach (int index in _cfg.WheelDiagnostics) sb.Append($" {index}={Read(index)}");
            return sb.ToString();
        }

        public bool EnableSelector()
        {
            if (Read(_cfg.SelectorDisabled) == 0) return false;
            Write(_cfg.SelectorDisabled, 0);
            return true;
        }

        public bool ReleaseMissionCandidate()
        {
            if (Read(_cfg.MissionType) == NoMission && Read(_cfg.MissionCandidate) == -1) return false;
            Write(_cfg.MissionCandidate, -1);
            foreach (int index in _cfg.MissionStateResets) Write(index, 0);
            Write(_cfg.MissionType, NoMission);
            return true;
        }

        public int PendingCommunications()
        {
            int b = _cfg.Communications;
            return Read(b + CallCount) + Read(b + TextCount) + Read(b + EmailCount) + Read(b + ChatCount)
                + Read(b + ConversationCount) + Read(b + QueuedChatCount);
        }

        public bool HasCommunicationPriority()
        {
            for (int c = 0; c < 3; c++)
                if (Read(_cfg.Communications + CharacterPriority + 1 + c) != 0) return true;
            return false;
        }

        public void ClearCommunications()
        {
            int b = _cfg.Communications;
            Write(b + CallCount, 0);
            Write(b + TextCount, 0);
            Write(b + EmailCount, 0);
            Write(b + ChatCount, 0);
            Write(b + ConversationCount, 0);
            Write(b + QueuedChatCount, 0);
            Write(b + 911, -1);
            Write(b + 912, 0);
            Write(b + 913, 0);
            Write(b + 914, 0);
            Write(b + 915, -1);
            Write(b + 916, 0);
            Write(b + 917, 0);
            Write(b + 918, -1);
            Write(b + 923, 0);
            for (int c = 0; c < 3; c++) Write(b + CharacterPriority + 1 + c, 0);
        }

        public bool ClearFlowBusy()
        {
            if (Read(_cfg.FlowBusy) == 0) return false;
            Write(_cfg.FlowBusy, 0);
            return true;
        }

        public bool IsRepeatPlay => (Read(_cfg.RepeatFlags) & 1) != 0;

        public bool IsRepeatControllerRunning => IsScriptRunning(_repeatControllerHash);

        public bool IsPrologueRunning => IsScriptRunning(_prologueHash);

        public bool IsPrologueCompleted => PrologueIndex >= 0 && Read(CompletionBase(PrologueIndex)) != 0;

        public int CompletedStoryMissions()
        {
            int count = 0;
            for (int i = 0; i < _cfg.StoryCompletionCount; i++)
                if (Read(CompletionBase(i)) != 0) count++;
            return count;
        }

        public bool AnyStrandActive()
        {
            for (int i = 0; i < _cfg.StrandCount; i++)
                if ((StrandBits(i) & StrandActive) != 0) return true;
            return false;
        }

        public void EnsureMainStrandActive()
        {
            if (_cfg.MainStrand < 0 || _cfg.MainStrand >= _cfg.StrandCount) return;
            int address = StrandBase(_cfg.MainStrand);
            int bits = Read(address);
            if ((bits & StrandActive) != 0) return;
            Write(address, bits | StrandActive);
            Log.Info($"Main strand {_cfg.MainStrand} marked active (bits {bits} -> {bits | StrandActive}).");
        }

        public bool IsMainStrandComplete => _cfg.MainStrand >= 0 && _cfg.MainStrand < _cfg.StrandCount && (StrandBits(_cfg.MainStrand) & StrandComplete) != 0;

        public bool IsCharacterIntroduced(PlayableCharacter character) => Read(IntroducedFlag(character)) != 0;

        public void SetCharacterIntroduced(PlayableCharacter character)
        {
            int address = IntroducedFlag(character);
            if (Read(address) == 0) Write(address, 1);
        }

        public bool IsCharacterAvailable(PlayableCharacter character) => Read(_cfg.CharacterAvailable + 1 + (int)character) != 0;

        public void SetCharacterAvailable(PlayableCharacter character, bool available) =>
            Write(_cfg.CharacterAvailable + 1 + (int)character, available ? 1 : 0);

        public bool BuildingsSupported =>
            _cfg.BuildingStates > 0 && _cfg.BuildingDirty > 0 &&
            Read(_cfg.BuildingStates) == _cfg.BuildingCount && Read(_cfg.BuildingDirty) == _cfg.BuildingCount &&
            (_cfg.BuildingDeferred <= 0 || Read(_cfg.BuildingDeferred) == _cfg.BuildingCount);

        public int GetBuildingState(int building) => Read(_cfg.BuildingStates + 1 + building);

        public void SetBuildingState(int building, int state)
        {
            Write(_cfg.BuildingStates + 1 + building, state);
            if (_cfg.BuildingDeferred > 0) Write(_cfg.BuildingDeferred + 1 + building, 0);
            Write(_cfg.BuildingDirty + 1 + building, 1);
        }

        public static bool IsScriptRunning(int hash) =>
            Function.Call<int>(Hash.GET_NUMBER_OF_THREADS_RUNNING_THE_SCRIPT_WITH_THIS_HASH, hash) > 0;

        private int StrandBits(int strand) => Read(StrandBase(strand));
        private int StrandBase(int strand) => _cfg.Strands + 1 + strand * StrandStride;
        private int CompletionBase(int mission) => _cfg.StoryCompletion + 1 + mission * CompletionStride;
        private int IntroducedFlag(PlayableCharacter character) => _cfg.FlowFlags + 1 + _cfg.CharacterIntroducedFlags[(int)character];

        private int FindMissionByScript(int scriptHash)
        {
            int count = Read(_cfg.MissionTable);
            if (count <= 0 || count > 200) return -1;
            for (int i = 0; i < count; i++)
                if (Read(_cfg.MissionTable + 1 + i * MissionStride + 6) == scriptHash) return i;
            return -1;
        }

        private static int Read(int index) => GlobalVariable.Get(index).Read<int>();
        private static void Write(int index, int value) => GlobalVariable.Get(index).Write(value);
    }
}
