using System;
using OneRoof.Application.Dialogue;
using OneRoof.Application.Modes;
using OneRoof.Application.Tower;
using OneRoof.Domain.Dialogue;
using OneRoof.Domain.Commands;
using EntityId = OneRoof.Domain.Identity.EntityId;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace OneRoof.UI.Dialogue
{
    /// <summary>Shared modal for leader exchanges, Resident learning and the persistent repertoire.</summary>
    [DisallowMultipleComponent]
    public sealed class WordBattlePanelView : MonoBehaviour
    {
        private TowerSimulationSession _simulation;
        private ModeShellSession _modes;
        private Action<bool> _pause;
        private Func<bool> _paused;
        private Action _refreshLock;
        private bool _previousPause, _notebook;
        private string _message = "", _search = "";
        private Vector2 _scroll, _entryScroll;
        private GUIStyle _replyStyle;
        public bool IsOpen { get; private set; }
        public static Rect EntryRect(int screenWidth, int screenHeight) =>
            new Rect(Mathf.Max(10, screenWidth - 306), Mathf.Max(112, screenHeight - 255), 290, 235);
        public void Bind(TowerSimulationSession simulation, ModeShellSession modes, Action<bool> pause, Func<bool> paused, Action refreshLock)
        {
            Release(); _simulation = simulation; _modes = modes; _pause = pause; _paused = paused; _refreshLock = refreshLock;
            var phase = simulation.WordBattleProjection().Phase;
            if (phase == WordBattlePhase.Active || phase == WordBattlePhase.ResidentChat || phase == WordBattlePhase.Result) Acquire();
        }
        private void Acquire()
        { if (IsOpen) return; _previousPause = _paused != null && _paused(); IsOpen = true; _pause?.Invoke(true); _refreshLock?.Invoke(); }
        private void Release()
        { if (!IsOpen) return; IsOpen = false; _pause?.Invoke(_previousPause); _refreshLock?.Invoke(); }
        private void OnDisable()
        {
            if (IsOpen && !_notebook && _simulation != null)
            {
                var p = _simulation.WordBattleProjection();
                if (p.Phase == WordBattlePhase.Active || p.Phase == WordBattlePhase.ResidentChat || p.Phase == WordBattlePhase.Result)
                    _simulation.ExecuteWordBattle(new WordBattleCommand(p.Phase == WordBattlePhase.Active ? WordBattleAction.Suspend : WordBattleAction.Close, expectedRevision: p.Revision));
            }
            Release();
        }
        public bool StartBattle(bool practice = false, int tier = 0)
        {
            if (_simulation == null || _simulation.CaretakerOwnsInput || (!IsOpen && _modes.IsInputBlocked)) return false;
            var result = _simulation.ExecuteWordBattle(new WordBattleCommand(WordBattleAction.Start, factionId: "merchant_guild", expectedRevision: _simulation.WordBattleProjection().Revision, practice: practice, tier: tier));
            _message = result.Reason;
            if (result.Accepted) { _notebook = false; Acquire(); }
            return result.Accepted;
        }
        public bool TeachResident(int residentId)
        {
            if (_simulation == null || _simulation.CaretakerOwnsInput || (!IsOpen && _modes.IsInputBlocked)) return false;
            var result = _simulation.ExecuteWordBattle(new WordBattleCommand(WordBattleAction.Teach, expectedRevision: _simulation.WordBattleProjection().Revision, speakerId: residentId));
            _message = result.Reason;
            if (result.Accepted) { _notebook = false; Acquire(); }
            return result.Accepted;
        }
        public bool Choose(string replyId)
        {
            if (!IsOpen || _notebook) return false;
            var result = _simulation.ExecuteWordBattle(new WordBattleCommand(WordBattleAction.Choose, replyId: replyId, expectedRevision: _simulation.WordBattleProjection().Revision));
            _message = result.Reason; return result.Accepted;
        }
        public void SuspendOrClose()
        {
            if (!IsOpen) return;
            if (!_notebook)
            {
                var p = _simulation.WordBattleProjection();
                var action = p.Phase == WordBattlePhase.Active ? WordBattleAction.Suspend : WordBattleAction.Close;
                var result = _simulation.ExecuteWordBattle(new WordBattleCommand(action, expectedRevision: p.Revision));
                if (!result.Accepted) { _message = result.Reason; return; }
            }
            Release();
        }
        public bool Resume()
        {
            if (_simulation == null || _modes.IsInputBlocked) return false;
            var result = _simulation.ExecuteWordBattle(new WordBattleCommand(WordBattleAction.Resume, expectedRevision: _simulation.WordBattleProjection().Revision));
            _message = result.Reason;
            if (result.Accepted) { _notebook = false; Acquire(); }
            return result.Accepted;
        }
        private void Update()
        {
            if (!IsOpen) return;
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame) { SuspendOrClose(); return; }
            var index = keyboard.digit1Key.wasPressedThisFrame ? 0 : keyboard.digit2Key.wasPressedThisFrame ? 1 : keyboard.digit3Key.wasPressedThisFrame ? 2 : keyboard.digit4Key.wasPressedThisFrame ? 3 : -1;
            if (!_notebook && (_simulation.WordBattleProjection().Phase == WordBattlePhase.Active || _simulation.WordBattleProjection().Phase == WordBattlePhase.ResidentChat))
            {
                var choices = _simulation.WordBattleProjection().Choices;
                if (index >= 0 && index < choices.Count) Choose(choices[index].Id);
                else if (keyboard.digit0Key.wasPressedThisFrame) Choose(null);
            }
#else
            if (Input.GetKeyDown(KeyCode.Escape)) { SuspendOrClose(); return; }
            if (!_notebook && (_simulation.WordBattleProjection().Phase == WordBattlePhase.Active || _simulation.WordBattleProjection().Phase == WordBattlePhase.ResidentChat))
            {
                var choices = _simulation.WordBattleProjection().Choices;
                for (var i = 0; i < choices.Count && i < 4; i++) if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i))) { Choose(choices[i].Id); break; }
                if (Input.GetKeyDown(KeyCode.Alpha0)) Choose(null);
            }
#endif
        }
        private void OnGUI()
        {
            if (_simulation == null || _modes == null) return;
            var p = _simulation.WordBattleProjection();
            if (!IsOpen)
            {
                if (_modes.IsInputBlocked || (_modes.CurrentMode != InteractionMode.Manage && _modes.CurrentMode != InteractionMode.Inspect)) return;
                GUILayout.BeginArea(EntryRect(Screen.width, Screen.height), StewardTheme.Panel);
                GUILayout.Label("LEARNED WIT — MERCHANT GUILD", StewardTheme.Label(14, bold: true));
                _entryScroll = GUILayout.BeginScrollView(_entryScroll);
                if (p.Phase == WordBattlePhase.Suspended)
                {
                    if (GUILayout.Button("Resume word battle")) Resume();
                    if (p.SpeakerId > 0 && !_simulation.TryGetResidentInspection(new EntityId(p.SpeakerId), out var missingSpeaker) && GUILayout.Button("Close unavailable conversation"))
                    {
                        var close = _simulation.ExecuteWordBattle(new WordBattleCommand(WordBattleAction.Close, expectedRevision: p.Revision));
                        _message = close.Reason;
                    }
                }
                else if (p.Phase == WordBattlePhase.Result) { if (GUILayout.Button("View last result")) { _notebook = false; Acquire(); } }
                else if (GUILayout.Button("Challenge Guild leader")) StartBattle();
                if (p.Phase == WordBattlePhase.Idle && p.MasteredTiers > 0)
                    for (var tier = 1; tier <= p.MasteredTiers; tier++) if (GUILayout.Button("Practice tier " + tier)) StartBattle(true, tier);
                if (GUILayout.Button("Learned replies (" + p.LearnedReplies.Count + "/12)")) { _notebook = true; Acquire(); }
                var selected = _modes.Projection().SelectedEntityId;
                if (selected.HasValue && _simulation.TryGetResidentInspection(new EntityId(selected.Value), out var resident))
                { if (GUILayout.Button("Chat with Resident #" + selected.Value)) TeachResident(selected.Value); }
                else GUILayout.Label("Inspect a Resident to learn a reply.");
                if (!string.IsNullOrEmpty(_message)) GUILayout.Label(_message);
                GUILayout.EndScrollView();
                GUILayout.EndArea(); return;
            }
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
            var height = Mathf.Min(560, Mathf.Max(240, Screen.height - 48));
            GUILayout.BeginArea(new Rect(Mathf.Max(12, (Screen.width - 700) / 2), Mathf.Max(24, (Screen.height - height) / 2), Mathf.Min(700, Screen.width - 24), height), StewardTheme.Panel);
            GUILayout.Label(_notebook ? "LEARNED REPLIES" : p.SpeakerId > 0 && string.IsNullOrEmpty(p.FactionId) ? "CONVERSATION — RESIDENT #" + p.SpeakerId : "WORD BATTLE — MERCHANT GUILD", StewardTheme.Label(20, bold: true));
            if (!_notebook && !string.IsNullOrEmpty(p.FactionId)) GUILayout.Label("Guild spokesperson · Resident #" + p.SpeakerId, StewardTheme.Label(13, StewardTheme.Muted));
            if (_replyStyle == null) _replyStyle = new GUIStyle(StewardTheme.Button) { fontSize = 16, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(14, 14, 10, 10) };
            GUILayout.Space(8);
            _scroll = GUILayout.BeginScrollView(_scroll);
            if (_notebook)
            {
                _search = GUILayout.TextField(_search);
                foreach (var reply in p.LearnedReplies) if (string.IsNullOrEmpty(_search) || reply.Text.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0) GUILayout.Label("Tier " + reply.Tier + ": " + reply.Text, StewardTheme.Label(16));
                if (p.RepertoireComplete) GUILayout.Label("Repertoire complete.");
            }
            else
            {
                if (!string.IsNullOrEmpty(p.FactionId)) GUILayout.Label("Tier " + p.Tier + (p.Practice ? " — Practice (no rewards)" : "") + "    Steward " + p.StewardScore + " : Leader " + p.LeaderScore, StewardTheme.Label(16, bold: true));
                var historyCount = p.Transcript.Count;
                var responding = p.Phase == WordBattlePhase.Active || p.Phase == WordBattlePhase.ResidentChat;
                if (responding && historyCount > 0 && p.Transcript[historyCount - 1] == p.ChallengeText) historyCount--;
                for (var i = 0; i < historyCount; i++)
                    if (p.Phase != WordBattlePhase.Result || !p.Transcript[i].StartsWith("New reply learned:", StringComparison.Ordinal))
                        GUILayout.Label(p.Transcript[i], StewardTheme.Label(14, StewardTheme.Muted));
                if (p.Phase == WordBattlePhase.Active || p.Phase == WordBattlePhase.ResidentChat)
                {
                    GUILayout.Space(12);
                    GUILayout.Label(p.ChallengeText, StewardTheme.Label(20, StewardTheme.Mint, true));
                    GUILayout.Space(12);
                    for (var i = 0; i < p.Choices.Count; i++) if (GUILayout.Button((i + 1) + ". " + p.Choices[i].Text, _replyStyle)) { Choose(p.Choices[i].Id); break; }
                    if (p.Phase == WordBattlePhase.Active && GUILayout.Button("0. I have no answer.", _replyStyle)) Choose(null);
                }
                else if (p.Phase == WordBattlePhase.Result)
                {
                    if (string.IsNullOrEmpty(p.FactionId)) GUILayout.Label(p.LastFeedback, StewardTheme.Label(16));
                    GUILayout.Space(12);
                    if (!string.IsNullOrEmpty(p.LearnedReplyId)) foreach (var reply in p.LearnedReplies) if (reply.Id == p.LearnedReplyId)
                    {
                        GUILayout.BeginVertical(StewardTheme.Recess);
                        GUILayout.Label("NEW REPLY LEARNED", StewardTheme.Label(13, StewardTheme.Muted, true));
                        GUILayout.Label(reply.Text, StewardTheme.Label(20, StewardTheme.Mint, true));
                        GUILayout.EndVertical();
                    }
                    GUILayout.Space(8);
                    GUILayout.Label("Guild respect: " + (p.MasteredTiers * .10f).ToString("P0") + " permanent tension relief", StewardTheme.Label(16, StewardTheme.Mint, true));
                    GUILayout.Label("Mastered tiers: " + p.MasteredTiers + " · Current tension reduced by " + p.ActualTensionReduction.ToString("P0"), StewardTheme.Label(14, StewardTheme.Muted));
                    if (p.RepertoireComplete) GUILayout.Label("Repertoire complete.");
                }
            }
            if (!string.IsNullOrEmpty(_message)) GUILayout.Label(_message);
            GUILayout.EndScrollView();
            if (GUILayout.Button(!_notebook && p.Phase == WordBattlePhase.Active ? "Suspend (Escape)" : "Close (Escape)", StewardTheme.Button, GUILayout.Height(36))) SuspendOrClose();
            GUILayout.EndArea();
        }
    }
}
