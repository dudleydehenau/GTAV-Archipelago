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
        private StoryGlobalsConfig _cfg;
        private int _repeatControllerHash;
        private int _prologueHash;

        public StoryGlobals()
        {
            _cfg = StoryGlobalsConfig.Candidates(GameBuild.Number)[0];
        }

        public bool Available { get; private set; }
        public string Status { get; private set; } = "not validated";
        public int PrologueIndex { get; private set; } = -1;
        public int PrologueScriptHash => _prologueHash;

        public void Validate()
        {
            Available = false;
            string first = null;
            foreach (StoryGlobalsConfig candidate in StoryGlobalsConfig.Candidates(GameBuild.Number))
            {
                string error = TryProfile(candidate);
                if (error == null) return;
                first = first ?? error;
            }
            Status = $"game build {GameBuild.Describe()}: {first}";
        }

        private string TryProfile(StoryGlobalsConfig candidate)
        {
            try
            {
                int flags = Read(candidate.FlowFlags);
                int strands = Read(candidate.Strands);
                int characters = Read(candidate.CharacterAvailable);
                int completion = Read(candidate.StoryCompletion);
                if (flags != candidate.FlowFlagCount || strands != candidate.StrandCount || characters != 3 || completion != candidate.StoryCompletionCount)
                    return $"unexpected array sizes for build {candidate.Build} (flow flags={flags}/{candidate.FlowFlagCount}, strands={strands}/{candidate.StrandCount}, characters={characters}/3, completion={completion}/{candidate.StoryCompletionCount}). Unsupported game build?";
                int calls = Read(candidate.Communications);
                int priorities = Read(candidate.Communications + CharacterPriority);
                if (calls != 9 || priorities != 3)
                    return $"unexpected communication queue for build {candidate.Build} (calls={calls}/9, priorities={priorities}/3). Unsupported game build?";
                _cfg = candidate;
                _repeatControllerHash = unchecked((int)StringHash.AtStringHash("mission_repeat_controller", 0u));
                _prologueHash = unchecked((int)StringHash.AtStringHash("prologue1", 0u));
                PrologueIndex = FindMissionByScript(_prologueHash);
                Available = true;
                Status = $"OK (game build {GameBuild.Describe()}, addresses {_cfg.Build}, prologue = mission index {PrologueIndex})";
                return null;
            }
            catch (Exception ex)
            {
                return "read error: " + ex.Message;
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

        private const int MinigameOffset = 99 + 219;

        public bool MinigamesSupported
        {
            get
            {
                int count = Read(_cfg.FlowState + MinigameOffset);
                return count >= 1 && count <= 64;
            }
        }

        public int MinigameBits
        {
            get => Read(_cfg.FlowState + MinigameOffset + 1);
            set => Write(_cfg.FlowState + MinigameOffset + 1, value);
        }

        private const int OffroadUnlocked = 9932;
        private const int TriathlonUnlocked = 10934;
        private const int StreetRaceUnlocked = 15895;

        public bool OpenRaceProgress()
        {
            bool changed = false;
            changed |= Raise(_cfg.FlowState + OffroadUnlocked, 5);
            changed |= Raise(_cfg.FlowState + TriathlonUnlocked, 2);
            int street = _cfg.FlowState + StreetRaceUnlocked;
            int bits = Read(street);
            if ((bits & ~0x1F) == 0 && bits != 0x1F)
            {
                Write(street, 0x1F);
                changed = true;
            }
            return changed;
        }

        private bool Raise(int index, int max)
        {
            int value = Read(index);
            if (value < -1 || value >= max) return false;
            Write(index, max);
            return true;
        }

        public int ShopCount => ShopsSupported ? Read(_cfg.ShopFlags) : 0;

        private const int StaticBlipCount = 263;
        private const int StaticBlipStride = 23;

        private int StaticBlipBase => _cfg.Build == 3725 ? 34155 : 34195;

        public bool StaticBlipsSupported
        {
            get
            {
                int root = StaticBlipBase;
                return Read(root) == StaticBlipCount && Read(root + 1) == 3 && Read(root + 1 + 12) == 3;
            }
        }

        public int StaticBlipSprite(int index) => Read(StaticBlipBase + 1 + index * StaticBlipStride + 13);

        public bool StaticBlipFinished(int index) => (StaticBlipFlags(index) & (1 << 20)) != 0;

        public int StaticBlipFlags(int index) => Read(StaticBlipBase + 1 + index * StaticBlipStride + 11);

        public int StaticBlipCountValue => StaticBlipCount;

        public GTA.Math.Vector3 StaticBlipPosition(int index)
        {
            int at = StaticBlipBase + 1 + index * StaticBlipStride;
            for (int slot = 0; slot < 3; slot++)
            {
                int p = at + 1 + slot * 3;
                var v = new GTA.Math.Vector3(GlobalVariable.Get(p).Read<float>(), GlobalVariable.Get(p + 1).Read<float>(), GlobalVariable.Get(p + 2).Read<float>());
                if (v.X != 0f || v.Y != 0f) return v;
            }
            return GTA.Math.Vector3.Zero;
        }

        public int StaticBlipHandle(int index) => Read(StaticBlipBase + 1 + index * StaticBlipStride + 19);

        public void SetStaticBlipHandle(int index, int handle) => Write(StaticBlipBase + 1 + index * StaticBlipStride + 19, handle);

        public void ActivateStaticBlip(int index)
        {
            int at = StaticBlipBase + 1 + index * StaticBlipStride + 11;
            Write(at, Read(at) | 1 | (1 << 3) | (1 << 15) | (1 << 18));
            Write(StaticBlipBase - 3, 1);
        }

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

        public bool ShopsSupported
        {
            get
            {
                if (_cfg.ShopFlags <= 0) return false;
                int count = Read(_cfg.ShopFlags);
                return count > 38 && count <= 200;
            }
        }

        public int GetShopFlags(int shop) => Read(_cfg.ShopFlags + 1 + shop);

        public void SetShopFlags(int shop, int flags) => Write(_cfg.ShopFlags + 1 + shop, flags);

        private const int DoorCount = 228;

        private int DoorStates => _cfg.Build == 3725 ? 122172 : 122231;

        public bool DoorsSupported => Read(DoorStates) == DoorCount;

        public int DoorCountValue => DoorCount;

        public int GetDoorState(int door) => Read(DoorStates + 1 + door);

        public void SetDoorState(int door, int state) => Write(DoorStates + 1 + door, state);

        public int GetBuildingState(int building) => Read(_cfg.BuildingStates + 1 + building);

        public void SetBuildingState(int building, int state)
        {
            Write(_cfg.BuildingStates + 1 + building, state);
            if (_cfg.BuildingDeferred > 0) Write(_cfg.BuildingDeferred + 1 + building, 0);
            Write(_cfg.BuildingDirty + 1 + building, 1);
        }

        public bool ContactsSupported => _cfg.Build == 3725 || _cfg.Build == 3889;

        private int CharSheet => _cfg.Build == 3725 ? 2339 : 2349;

        private int SavedCharSheet => _cfg.Build == 3725 ? 114904 + 28058 : 114963 + 28060;

        public int AddContacts(int[] contacts)
        {
            const int stride = 29;
            int added = 0;
            foreach (int contact in contacts)
                for (int player = 0; player < 3; player++)
                {
                    if (contact == player) continue;
                    int known = CharSheet + 1 + contact * stride + 13 + player;
                    int named = CharSheet + 1 + contact * stride + 25 + player;
                    if (Read(known) == 1 && Read(named) == 1) continue;
                    Write(known, 1);
                    Write(named, 1);
                    if (contact < 162)
                    {
                        Write(SavedCharSheet + 1 + contact * stride + 13 + player, 1);
                        Write(SavedCharSheet + 1 + contact * stride + 25 + player, 1);
                    }
                    added++;
                }
            return added;
        }

        public bool RaiseFlowFlag(int flag)
        {
            int index = _cfg.FlowFlags + 1 + flag;
            if (Read(index) != 0) return false;
            Write(index, 1);
            return true;
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
