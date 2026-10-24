using System;
using System.Collections.Generic;

namespace MissionLauncher
{
    public enum ReplayCategory
    {
        Story = 1,
        StrangersAndFreaks = 7,
    }

    public enum MissionMarkerState
    {
        Hidden = 0,
        Available = 1,
        Replay = 2,
    }

    public enum ReplayOutcome
    {
        Passed,
        Failed,
        Abandoned,
    }

    public enum MissionMedal
    {
        None,
        Bronze,
        Silver,
        Gold,
    }

    public sealed class ReplayMission
    {
        internal ReplayMission(ReplayCategory category, int number, int gameIndex, string code, string titleLabel, string scriptName, int scriptHash)
        {
            Category = category;
            GameIndex = gameIndex;
            Number = number;
            Code = code;
            TitleLabel = titleLabel;
            ScriptName = scriptName;
            ScriptHash = scriptHash;
            Title = code;
        }

        public ReplayCategory Category { get; }
        public int GameIndex { get; }
        public int Number { get; }
        public string Id => Category == ReplayCategory.Story ? Number.ToString("00") : Number.ToString();
        public string Code { get; }
        public string TitleLabel { get; }
        public string ScriptName { get; }
        public string Title { get; internal set; }
        public int CompletionOrder { get; internal set; } = -1;
        public bool IsCompleted => CompletionOrder >= 0;
        internal int ScriptHash { get; }

        public int ObjectiveId(int position) => Number * 100 + position;

        public string ObjectiveCode(int position) => ObjectiveId(position).ToString(Category == ReplayCategory.Story ? "0000" : "00000");

        public override string ToString() => $"{Id} {Title}";
    }

    public sealed class MissionObjective
    {
        internal MissionObjective(int statId, string label, string text, int statType, int value, int target, bool lowerIsBetter, bool invalidated, bool passed)
        {
            StatId = statId;
            Label = label;
            Text = text;
            StatType = statType;
            Value = value;
            Target = target;
            LowerIsBetter = lowerIsBetter;
            Invalidated = invalidated;
            Passed = passed;
        }

        public int Id { get; internal set; }
        public string Code { get; internal set; } = string.Empty;
        public int Position { get; internal set; }
        public int StatId { get; }
        public string Label { get; }
        public string Text { get; }
        public int StatType { get; }
        public int Value { get; }
        public int Target { get; }
        public bool LowerIsBetter { get; }
        public bool Invalidated { get; }
        public bool Passed { get; }

        public string FormatValue(int value)
        {
            switch (StatType)
            {
                case 1:
                case 2:
                case 4:
                case 5:
                case 17:
                    TimeSpan t = TimeSpan.FromMilliseconds(Math.Max(0, value));
                    return t.TotalMinutes >= 1
                        ? $"{(int)t.TotalMinutes}:{t.Seconds:00}.{t.Milliseconds / 100}"
                        : $"{t.Seconds}.{t.Milliseconds / 100}s";
                case 9:
                    return value + " %";
                default:
                    return value.ToString();
            }
        }

        public override string ToString() =>
            $"{Code} [{(Passed ? "OK" : "X")}] {Text}: {FormatValue(Value)} (target {(LowerIsBetter ? "<" : ">=")} {FormatValue(Target)})";
    }

    public sealed class ReplayAttemptEventArgs : EventArgs
    {
        internal ReplayAttemptEventArgs(ReplayMission mission, int attempt)
        {
            Mission = mission;
            Attempt = attempt;
        }

        public ReplayMission Mission { get; }
        public int Attempt { get; }
    }

    public sealed class ReplayResultEventArgs : EventArgs
    {
        internal ReplayResultEventArgs(ReplayMission mission, int attempt, ReplayOutcome outcome, MissionMedal medal, int percent,
            IReadOnlyList<MissionObjective> objectives, string failLabel, string failText, TimeSpan duration)
        {
            Mission = mission;
            Attempt = attempt;
            Outcome = outcome;
            Medal = medal;
            Percent = percent;
            Objectives = objectives ?? Array.Empty<MissionObjective>();
            FailReasonLabel = failLabel;
            FailReasonText = failText;
            Duration = duration;
        }

        public ReplayMission Mission { get; }
        public int Attempt { get; }
        public ReplayOutcome Outcome { get; }
        public MissionMedal Medal { get; }
        public int Percent { get; }
        public bool Gold => Medal == MissionMedal.Gold;
        public IReadOnlyList<MissionObjective> Objectives { get; }
        public string FailReasonLabel { get; }
        public string FailReasonText { get; }
        public TimeSpan Duration { get; }
    }

    public sealed class ReplayFinishedEventArgs : EventArgs
    {
        internal ReplayFinishedEventArgs(ReplayMission mission, int attempts, ReplayOutcome? lastOutcome)
        {
            Mission = mission;
            Attempts = attempts;
            LastOutcome = lastOutcome;
        }

        public ReplayMission Mission { get; }
        public int Attempts { get; }
        public ReplayOutcome? LastOutcome { get; }
    }
}
