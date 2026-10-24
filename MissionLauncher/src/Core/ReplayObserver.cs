using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GTA;
using GTA.Native;

namespace MissionLauncher.Core
{
    internal sealed class ReplayObserver
    {
        private readonly GameGlobals _globals;
        private readonly StatReader _stats;
        private readonly ReplayCatalog _catalog;
        private readonly string _markerPath;

        private bool _session;
        private int _sessionStartedAt;
        private ReplayMission _mission;
        private int _attempts;
        private ReplayOutcome? _lastOutcome;

        private bool _attemptRunning;
        private int _attemptThread;
        private bool _reported;
        private bool _passFlagBefore;
        private int _endedAt = -1;
        private const int EndGrace = 6000;
        private const int SessionTimeout = 15000;
        private int _idleSince = -1;
        private const int MinimumPassTime = 15000;
        private bool _jingleAtStart;
        private int _attemptStartedAt;
        private int[] _scriptHashes = Array.Empty<int>();
        private bool _threadsLogged;
        private int[] _rampageAtStart;
        private int _shotsAtStart;
        private int _hitsAtStart;

        private int _createdAt = -1;
        private bool _markerChecked;

        public ReplayObserver(GameGlobals globals, StatReader stats, ReplayCatalog catalog, string directory)
        {
            _globals = globals;
            _stats = stats;
            _catalog = catalog;
            _markerPath = Path.Combine(directory, "replay_session.txt");
        }

        public bool SessionActive => _session;
        public ReplayMission CurrentMission => _session ? _mission : null;

        public IReadOnlyList<MissionObjective> LiveObjectives()
        {
            if (!_session || _mission == null || !_attemptRunning || _reported) return Array.Empty<MissionObjective>();
            StatResults stats = _stats.Read(_shotsAtStart, _hitsAtStart);
            foreach (MissionObjective o in stats.Objectives)
            {
                o.Id = _mission.ObjectiveId(o.Position);
                o.Code = _mission.ObjectiveCode(o.Position);
            }
            return stats.Objectives.AsReadOnly();
        }

        public void Tick()
        {
            bool controller = _globals.IsReplayControllerRunning();
            bool requested = _globals.IsReplayRequested();
            CheckMarker(controller || requested);

            if (!_session)
            {
                if (controller || requested) StartSession();
                return;
            }

            if (_mission != null)
            {
                int thread = FindThread(_scriptHashes);
                bool running = thread != 0;
                if (!running && _attempts == 0 && !_threadsLogged && Game.GameTime - _sessionStartedAt > 30000) LogThreads();

                if (_attemptRunning && running && thread != _attemptThread)
                {
                    if (!_reported) Report(ReplayOutcome.Abandoned, null, null);
                    _attemptRunning = false;
                }

                if (!_attemptRunning)
                {
                    if (running) StartAttempt(thread);
                }
                else
                {
                    if (!_reported) CheckOutcome();
                    if (!running)
                    {
                        if (_reported || FinishWithoutThread()) _attemptRunning = false;
                    }
                }
            }

            if (controller || requested) _idleSince = -1;
            else if (_idleSince < 0) _idleSince = Game.GameTime;
            if (!controller && !requested && Game.GameTime - _sessionStartedAt > 3000)
            {
                bool stuck = _idleSince >= 0 && Game.GameTime - _idleSince > SessionTimeout;
                if (_attemptRunning && !_reported && !FinishWithoutThread() && !stuck) return;
                if (stuck && _attemptRunning && !_reported) Report(ReplayOutcome.Abandoned, null, null);
                EndSession();
            }
        }

        private void StartSession()
        {
            _session = true;
            _sessionStartedAt = Game.GameTime;
            _idleSince = -1;
            _attempts = 0;
            _lastOutcome = null;
            _attemptRunning = false;

            _globals.ReadReplayRequest(out int type, out int index);
            ReplayCategory? category = type == 1 ? ReplayCategory.Story : type == 7 ? ReplayCategory.StrangersAndFreaks : (ReplayCategory?)null;
            _mission = category.HasValue ? _catalog.Find(category.Value, index) : null;
            _scriptHashes = ScriptHashesFor(_mission);
            _threadsLogged = false;

            if (_mission == null) Log.Warn($"[REPLAY] Replay started for an unknown mission (type {type}, index {index}): results are not tracked.");
            else Log.Result($"[REPLAY] Replay started: {_mission.Id} {_mission.Title} ({_mission.Code}).");

            SaveMarker();
            MissionApi.RaiseReplayStarted(_mission);
        }

        private void StartAttempt(int thread)
        {
            _attempts++;
            _attemptThread = thread;
            _attemptRunning = true;
            _reported = false;
            _attemptStartedAt = Game.GameTime;
            _passFlagBefore = _globals.MissionPassedFlag();
            _jingleAtStart = Function.Call<bool>(Hash.IS_MISSION_COMPLETE_PLAYING);
            _rampageAtStart = null;
            if (IsRampage) StartRampage();
            _endedAt = -1;
            _stats.ReadShotBaseline(out _shotsAtStart, out _hitsAtStart);
            _globals.ClearFailReason();

            Log.Result($"[REPLAY] Attempt {_attempts} started: {_mission.Id} {_mission.Title}.");
            SaveMarker();
            MissionApi.RaiseAttemptStarted(new ReplayAttemptEventArgs(_mission, _attempts));
        }

        private bool PassSignal()
        {
            bool jingle = Function.Call<bool>(Hash.IS_MISSION_COMPLETE_PLAYING);
            if (!jingle) _jingleAtStart = false;
            if (Game.GameTime - _attemptStartedAt < MinimumPassTime)
            {
                if (_globals.MissionPassedFlag()) _passFlagBefore = true;
                return false;
            }
            if (_globals.MissionPassedFlag() && !_passFlagBefore) return true;
            if (RampagePassed()) return true;
            return jingle && !_jingleAtStart;
        }

        private bool IsRampage => _mission != null && (_mission.Code ?? string.Empty).StartsWith("RC_RAM", StringComparison.OrdinalIgnoreCase);

        private void StartRampage()
        {
            int[] before = _globals.RampageRecords();
            if (before == null)
            {
                Log.Warn($"[REPLAY] Rampage records unavailable (header {_globals.RampageHeader}), the result cannot be detected.");
                return;
            }
            _globals.ClearRampageRecords();
            _rampageAtStart = _globals.RampageRecords();
            Log.Info($"[REPLAY] Rampage records reset for detection (were {string.Join(",", before)}).");
        }

        private bool RampagePassed()
        {
            if (_rampageAtStart == null || !IsRampage) return false;
            int[] now = _globals.RampageRecords();
            if (now == null || now.Length != _rampageAtStart.Length) return false;
            for (int i = 0; i < now.Length; i += 2)
            {
                if (now[i] == _rampageAtStart[i] && now[i + 1] == _rampageAtStart[i + 1]) continue;
                Log.Info($"[REPLAY] Rampage record {i / 2} written (medal {now[i]}, score {now[i + 1]}).");
                if (now[i] < 1)
                {
                    _rampageAtStart = now;
                    return false;
                }
                _rampageAtStart = null;
                return true;
            }
            return false;
        }

        private bool FinishWithoutThread()
        {
            if (_reported) return true;
            if (PassSignal())
            {
                ReportPassed();
                return true;
            }
            string label = _globals.ReadFailReason();
            if (!string.IsNullOrEmpty(label))
            {
                Report(ReplayOutcome.Failed, label, GameText.Get(label));
                return true;
            }
            if (_endedAt < 0)
            {
                _endedAt = Game.GameTime;
                int[] records = IsRampage ? _globals.RampageRecords() : null;
                Log.Info($"[REPLAY] Mission script ended without a result yet (pass flag {_globals.MissionPassedFlag()}{(records != null ? ", rampage records " + string.Join(",", records) : string.Empty)}).");
            }
            if (Game.GameTime - _endedAt < EndGrace) return false;
            Report(ReplayOutcome.Abandoned, null, null);
            return true;
        }

        private void CheckOutcome()
        {
            bool pass = _globals.MissionPassedFlag();
            if (PassSignal())
            {
                ReportPassed();
                return;
            }
            if (!pass) _passFlagBefore = false;

            string label = _globals.ReadFailReason();
            if (!string.IsNullOrEmpty(label))
            {
                Report(ReplayOutcome.Failed, label, GameText.Get(label));
                return;
            }

            Ped player = Game.Player.Character;
            bool dead = player == null || !player.Exists() || player.IsDead;
            bool arrested = Function.Call<bool>(Hash.IS_PLAYER_BEING_ARRESTED, Game.Player.Handle, true);
            if (dead) Report(ReplayOutcome.Failed, null, "Wasted");
            else if (arrested) Report(ReplayOutcome.Failed, null, "Busted");
        }

        private void ReportPassed()
        {
            StatResults stats = _stats.Read(_shotsAtStart, _hitsAtStart);
            foreach (MissionObjective o in stats.Objectives)
            {
                o.Id = _mission.ObjectiveId(o.Position);
                o.Code = _mission.ObjectiveCode(o.Position);
            }
            Report(ReplayOutcome.Passed, null, null, stats);
        }

        private void Report(ReplayOutcome outcome, string failLabel, string failText, StatResults stats = null)
        {
            _reported = true;
            _lastOutcome = outcome;
            TimeSpan duration = TimeSpan.FromMilliseconds(Math.Max(0, Game.GameTime - _attemptStartedAt));
            bool passed = outcome == ReplayOutcome.Passed && stats != null;
            MissionMedal medal = passed ? stats.Medal : MissionMedal.None;
            int percent = passed ? stats.Percent : 0;
            IReadOnlyList<MissionObjective> objectives = stats != null
                ? stats.Objectives.AsReadOnly()
                : (IReadOnlyList<MissionObjective>)Array.Empty<MissionObjective>();

            var sb = new StringBuilder($"[RESULT] {_mission.Id} {_mission.Title} - attempt {_attempts} - ");
            switch (outcome)
            {
                case ReplayOutcome.Passed:
                    if (objectives.Count == 0)
                    {
                        sb.Append("PASSED (no medal objectives)");
                    }
                    else
                    {
                        sb.Append($"PASSED, {medal.ToString().ToUpperInvariant()} medal ({percent} %)");
                        if (medal == MissionMedal.Gold) sb.Append(" - GOLD");
                    }
                    break;
                case ReplayOutcome.Failed:
                    sb.Append("FAILED");
                    if (failText != null) sb.Append($": {failText}");
                    if (failLabel != null) sb.Append($" ({failLabel})");
                    break;
                default:
                    sb.Append("ABANDONED");
                    break;
            }
            sb.Append($" - {(int)duration.TotalMinutes}:{duration.Seconds:00}");
            foreach (MissionObjective o in objectives) sb.Append(Environment.NewLine).Append("    ").Append(o);
            Log.Result(sb.ToString());
            SaveMarker();

            MissionApi.RaiseAttemptEnded(new ReplayResultEventArgs(_mission, _attempts, outcome, medal, percent, objectives, failLabel, failText, duration));
        }

        private void EndSession()
        {
            Log.Result(_mission != null
                ? $"[REPLAY] Replay finished: {_mission.Id} {_mission.Title}, {_attempts} attempt(s), last outcome: {(_lastOutcome.HasValue ? _lastOutcome.Value.ToString() : "none")}."
                : "[REPLAY] Replay finished.");
            var args = new ReplayFinishedEventArgs(_mission, _attempts, _lastOutcome);
            DeleteMarker();
            _session = false;
            _mission = null;
            _attemptRunning = false;
            MissionApi.RaiseReplayFinished(args);
        }

        private static int FindThread(int[] scriptHashes)
        {
            if (scriptHashes.Length == 0) return 0;
            Function.Call(Hash.SCRIPT_THREAD_ITERATOR_RESET);
            for (int i = 0; i < 1000; i++)
            {
                int id = Function.Call<int>(Hash.SCRIPT_THREAD_ITERATOR_GET_NEXT_THREAD_ID);
                if (id == 0) return 0;
                string name = Function.Call<string>(Hash.GET_NAME_OF_SCRIPT_WITH_THIS_ID, id);
                if (string.IsNullOrEmpty(name)) continue;
                int hash = Function.Call<int>(Hash.GET_HASH_KEY, name);
                if (Array.IndexOf(scriptHashes, hash) >= 0)
                {
                    if (hash != scriptHashes[0] && _siblingLogged != id)
                    {
                        _siblingLogged = id;
                        Log.Info($"[REPLAY] The game runs {name} for {_currentCode}, counted for the requested mission.");
                    }
                    return id;
                }
            }
            return 0;
        }

        private static string _currentCode = string.Empty;
        private static int _siblingLogged;

        private int[] ScriptHashesFor(ReplayMission mission)
        {
            _currentCode = mission?.Code ?? string.Empty;
            if (mission == null) return Array.Empty<int>();
            var hashes = new List<int> { mission.ScriptHash };
            string code = mission.Code ?? string.Empty;
            char last = code.Length > 3 ? char.ToUpperInvariant(code[code.Length - 1]) : ' ';
            if (last == 'A' || last == 'B')
            {
                string stem = code.Substring(0, code.Length - 1);
                foreach (ReplayMission other in _catalog.All)
                {
                    if (other == mission || other.Category != mission.Category || other.ScriptHash == 0) continue;
                    string otherCode = other.Code ?? string.Empty;
                    if (otherCode.Length != code.Length || !otherCode.StartsWith(stem, StringComparison.OrdinalIgnoreCase)) continue;
                    char otherLast = char.ToUpperInvariant(otherCode[otherCode.Length - 1]);
                    if ((otherLast == 'A' || otherLast == 'B') && !hashes.Contains(other.ScriptHash)) hashes.Add(other.ScriptHash);
                }
            }
            return hashes.ToArray();
        }

        private void LogThreads()
        {
            _threadsLogged = true;
            var names = new List<string>();
            Function.Call(Hash.SCRIPT_THREAD_ITERATOR_RESET);
            for (int i = 0; i < 1000; i++)
            {
                int id = Function.Call<int>(Hash.SCRIPT_THREAD_ITERATOR_GET_NEXT_THREAD_ID);
                if (id == 0) break;
                string name = Function.Call<string>(Hash.GET_NAME_OF_SCRIPT_WITH_THIS_ID, id);
                if (!string.IsNullOrEmpty(name)) names.Add(name);
            }
            Log.Warn($"[REPLAY] Mission script of {_mission?.Code} not found after 30 s (expected hash {(_scriptHashes.Length > 0 ? _scriptHashes[0] : 0)}). Running scripts: {string.Join(", ", names)}");
        }

        private void SaveMarker()
        {
            try
            {
                File.WriteAllText(_markerPath,
                    $"{(_mission != null ? _mission.Number : 0)};{_attempts};{(_lastOutcome.HasValue ? (int)_lastOutcome.Value : -1)}");
            }
            catch (Exception ex)
            {
                Log.Debug("replay_session.txt: " + ex.Message);
            }
        }

        private void DeleteMarker()
        {
            try
            {
                if (File.Exists(_markerPath)) File.Delete(_markerPath);
            }
            catch
            {
            }
        }

        private void CheckMarker(bool replayRunning)
        {
            if (_markerChecked) return;
            if (_createdAt < 0) _createdAt = Game.GameTime;
            if (Game.GameTime - _createdAt < 3000) return;
            _markerChecked = true;
            if (replayRunning || !File.Exists(_markerPath)) return;
            try
            {
                string[] parts = File.ReadAllText(_markerPath).Split(';');
                ReplayMission mission = _catalog.Find(int.Parse(parts[0]));
                int attempts = int.Parse(parts[1]);
                int outcome = int.Parse(parts[2]);
                ReplayOutcome? last = outcome >= 0 ? (ReplayOutcome)outcome : (ReplayOutcome?)null;
                Log.Result(mission != null
                    ? $"[REPLAY] Replay finished: {mission.Id} {mission.Title}, {attempts} attempt(s), last outcome: {(last.HasValue ? last.Value.ToString() : "none")}."
                    : "[REPLAY] Replay finished.");
                MissionApi.RaiseReplayFinished(new ReplayFinishedEventArgs(mission, attempts, last));
            }
            catch (Exception ex)
            {
                Log.Warn("replay_session.txt is unreadable: " + ex.Message);
            }
            DeleteMarker();
        }
    }
}
