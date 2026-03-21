using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace vMixController.Classes.Scripting
{
    internal sealed class ScriptExecutionChainToken
    {
        public Guid Id { get; set; }
        public string RootLink { get; set; }
        public long StartedUtcTicks { get; set; }
        public int HopCount { get; set; }
        internal Dictionary<string, int> ActiveLinks { get; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        internal object ActiveLinksSync { get; } = new object();
    }

    internal sealed class ScriptExecutionDispatchPayload
    {
        public ScriptExecutionChainToken ChainToken { get; set; }
        public object Payload { get; set; }
    }

    internal static class ScriptExecutionDispatchRuntime
    {
        private static readonly AsyncLocal<ScriptExecutionChainToken> _current = new AsyncLocal<ScriptExecutionChainToken>();

        public static ScriptExecutionChainToken CurrentChain => _current.Value;

        public static ScriptExecutionChainToken Push(ScriptExecutionChainToken token)
        {
            var previous = _current.Value;
            _current.Value = token;
            return previous;
        }

        public static void Pop(ScriptExecutionChainToken previous)
        {
            _current.Value = previous;
        }

        public static object CreateOutgoingParameter(object payload)
        {
            var chain = _current.Value;
            if (chain == null)
                return payload;

            return new ScriptExecutionDispatchPayload
            {
                ChainToken = chain,
                Payload = payload
            };
        }
    }

    internal sealed class ScriptExecutionLoopGuard
    {
        private const string GlobalScopeKey = "__all_exec_links__";

        private sealed class GuardState
        {
            public readonly Queue<long> HitsUtcTicks = new Queue<long>();
            public long BlockedUntilUtcTicks;
        }

        private readonly object _sync = new object();
        private readonly Dictionary<string, GuardState> _states = new Dictionary<string, GuardState>(StringComparer.Ordinal);
        private readonly int _maxHitsInWindow;
        private readonly long _windowTicks;
        private readonly long _cooldownTicks;

        public ScriptExecutionLoopGuard(int maxHitsInWindow, TimeSpan window, TimeSpan cooldown)
        {
            _maxHitsInWindow = Math.Max(1, maxHitsInWindow);
            _windowTicks = Math.Max(TimeSpan.FromMilliseconds(1).Ticks, window.Ticks);
            _cooldownTicks = Math.Max(TimeSpan.FromMilliseconds(1).Ticks, cooldown.Ticks);
        }

        public ScriptExecutionLoopGuardResult CheckAndRegister(ScriptExecutionChainToken chain, string currentLink, DateTime utcNow)
        {
            var nowTicks = utcNow.Ticks;
            var current = Normalize(currentLink);
            var chainKey = chain != null ? chain.Id.ToString("N") : string.Empty;
            if (string.IsNullOrWhiteSpace(chainKey))
                chainKey = "__unknown_chain__";

            lock (_sync)
            {
                var chainState = GetOrCreate(chainKey);
                var globalState = GetOrCreate(GlobalScopeKey);

                var blocked = CheckBlocked(chainState, nowTicks, chain, current);
                if (blocked != null)
                    return blocked;

                blocked = CheckBlocked(globalState, nowTicks, chain, current);
                if (blocked != null)
                    return blocked;

                var chainTriggered = RegisterHit(chainState, nowTicks);
                if (chainTriggered)
                    return Block(chainState, nowTicks, chain, current);

                var globalTriggered = RegisterHit(globalState, nowTicks);
                if (globalTriggered)
                    return Block(globalState, nowTicks, chain, current);

                Cleanup(nowTicks);

                return ScriptExecutionLoopGuardResult.Allowed(chain, current, Math.Max(chainState.HitsUtcTicks.Count, globalState.HitsUtcTicks.Count));
            }
        }

        public ScriptExecutionLoopGuardResult TryEnterLink(ScriptExecutionChainToken chain, string currentLink)
        {
            if (chain == null)
                return null;

            var normalized = Normalize(currentLink);
            lock (chain.ActiveLinksSync)
            {
                if (chain.ActiveLinks.TryGetValue(normalized, out var count) && count > 0)
                {
                    return ScriptExecutionLoopGuardResult.Blocked(
                        chain,
                        normalized,
                        count + 1,
                        TimeSpan.Zero,
                        true);
                }

                chain.ActiveLinks[normalized] = count + 1;
            }

            return null;
        }

        public void ExitLink(ScriptExecutionChainToken chain, string currentLink)
        {
            if (chain == null)
                return;

            var normalized = Normalize(currentLink);
            lock (chain.ActiveLinksSync)
            {
                if (!chain.ActiveLinks.TryGetValue(normalized, out var count))
                    return;

                if (count <= 1)
                    chain.ActiveLinks.Remove(normalized);
                else
                    chain.ActiveLinks[normalized] = count - 1;
            }
        }

        public ScriptExecutionChainToken CreateOrContinueChain(ScriptExecutionChainToken incoming, string currentLink, DateTime utcNow)
        {
            var nowTicks = utcNow.Ticks;
            if (incoming == null || incoming.Id == Guid.Empty)
            {
                return new ScriptExecutionChainToken
                {
                    Id = Guid.NewGuid(),
                    RootLink = Normalize(currentLink),
                    StartedUtcTicks = nowTicks,
                    HopCount = 1
                };
            }

            incoming.HopCount++;
            return incoming;
        }

        private ScriptExecutionLoopGuardResult Block(GuardState state, long nowTicks, ScriptExecutionChainToken chain, string currentLink)
        {
            state.BlockedUntilUtcTicks = nowTicks + _cooldownTicks;
            var hits = state.HitsUtcTicks.Count;
            state.HitsUtcTicks.Clear();

            return ScriptExecutionLoopGuardResult.Blocked(chain, currentLink, hits, TimeSpan.FromTicks(_cooldownTicks), true);
        }

        private ScriptExecutionLoopGuardResult CheckBlocked(GuardState state, long nowTicks, ScriptExecutionChainToken chain, string currentLink)
        {
            TrimWindow(state, nowTicks);
            if (state.BlockedUntilUtcTicks > nowTicks)
            {
                return ScriptExecutionLoopGuardResult.Blocked(
                    chain,
                    currentLink,
                    state.HitsUtcTicks.Count,
                    TimeSpan.FromTicks(state.BlockedUntilUtcTicks - nowTicks),
                    false);
            }

            return null;
        }

        private bool RegisterHit(GuardState state, long nowTicks)
        {
            state.HitsUtcTicks.Enqueue(nowTicks);
            TrimWindow(state, nowTicks);
            return state.HitsUtcTicks.Count > _maxHitsInWindow;
        }

        private GuardState GetOrCreate(string key)
        {
            if (!_states.TryGetValue(key, out var state))
            {
                state = new GuardState();
                _states[key] = state;
            }

            return state;
        }

        private void TrimWindow(GuardState state, long nowTicks)
        {
            while (state.HitsUtcTicks.Count > 0 && (nowTicks - state.HitsUtcTicks.Peek()) > _windowTicks)
                state.HitsUtcTicks.Dequeue();
        }

        private void Cleanup(long nowTicks)
        {
            if (_states.Count < 1024)
                return;

            var toRemove = new List<string>();
            foreach (var pair in _states)
            {
                if (pair.Key == GlobalScopeKey)
                    continue;

                var state = pair.Value;
                if (state.HitsUtcTicks.Count == 0 && state.BlockedUntilUtcTicks <= nowTicks)
                    toRemove.Add(pair.Key);
            }

            foreach (var key in toRemove)
                _states.Remove(key);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }

    internal sealed class ScriptExecutionLoopGuardResult
    {
        public bool IsAllowed { get; private set; }
        public bool JustTriggered { get; private set; }
        public ScriptExecutionChainToken Chain { get; private set; }
        public string CurrentLink { get; private set; }
        public int HitsInWindow { get; private set; }
        public TimeSpan RetryAfter { get; private set; }

        public static ScriptExecutionLoopGuardResult Allowed(ScriptExecutionChainToken chain, string currentLink, int hitsInWindow)
        {
            return new ScriptExecutionLoopGuardResult
            {
                IsAllowed = true,
                JustTriggered = false,
                Chain = chain,
                CurrentLink = currentLink,
                HitsInWindow = hitsInWindow,
                RetryAfter = TimeSpan.Zero
            };
        }

        public static ScriptExecutionLoopGuardResult Blocked(ScriptExecutionChainToken chain, string currentLink, int hitsInWindow, TimeSpan retryAfter, bool justTriggered)
        {
            return new ScriptExecutionLoopGuardResult
            {
                IsAllowed = false,
                JustTriggered = justTriggered,
                Chain = chain,
                CurrentLink = currentLink,
                HitsInWindow = hitsInWindow,
                RetryAfter = retryAfter
            };
        }
    }
}
