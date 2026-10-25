using NUnit.Framework;
using Seeker.Transfers;
using Soulseek;
using System;

namespace UnitTestCommon
{
    [TestFixture]
    public class TransferPresentationTests
    {
        // --- ProgressPercent ---

        [Test]
        public void ProgressPercent_InProgress_IsByteDerived()
        {
            Assert.AreEqual(40, TransferPresentation.ProgressPercent(400, 1000, terminal: false));
        }

        [Test]
        public void ProgressPercent_Terminal_ReadsComplete()
        {
            Assert.AreEqual(100, TransferPresentation.ProgressPercent(10, 1000, terminal: true));
            Assert.AreEqual(100, TransferPresentation.ProgressPercent(0, 1000, terminal: true));
        }

        [Test]
        public void ProgressPercent_ClampsStaleCounts()
        {
            Assert.AreEqual(100, TransferPresentation.ProgressPercent(2000, 1000, terminal: false));
            Assert.AreEqual(0, TransferPresentation.ProgressPercent(-5, 1000, terminal: false));
        }

        [Test]
        public void ProgressPercent_UnknownSize_IsZero()
        {
            Assert.AreEqual(0, TransferPresentation.ProgressPercent(0, 0, terminal: false));
            Assert.AreEqual(0, TransferPresentation.ProgressPercent(123, -1, terminal: false));
        }

        [Test]
        public void ProgressPercent_NonTerminalOverloadMatches()
        {
            Assert.AreEqual(
                TransferPresentation.ProgressPercent(333, 1000, terminal: false),
                TransferPresentation.ProgressPercent(333, 1000));
        }

        // --- MeaningfulRemainingTime / ShouldShowEta ---

        [Test]
        public void MeaningfulRemainingTime_InProgressPositive_IsShown()
        {
            var eta = TimeSpan.FromSeconds(90);
            Assert.AreEqual(eta, TransferPresentation.MeaningfulRemainingTime(eta, inProgress: true));
            Assert.IsTrue(TransferPresentation.ShouldShowEta(eta, inProgress: true));
        }

        [Test]
        public void MeaningfulRemainingTime_NotInProgress_IsHidden()
        {
            var eta = TimeSpan.FromSeconds(90);
            Assert.IsNull(TransferPresentation.MeaningfulRemainingTime(eta, inProgress: false));
            Assert.IsFalse(TransferPresentation.ShouldShowEta(eta, inProgress: false));
            Assert.IsNull(TransferPresentation.MeaningfulRemainingTime(null, inProgress: false));
        }

        [Test]
        public void MeaningfulRemainingTime_ZeroOrNegative_IsHidden()
        {
            Assert.IsNull(TransferPresentation.MeaningfulRemainingTime(TimeSpan.Zero, inProgress: true));
            Assert.IsNull(TransferPresentation.MeaningfulRemainingTime(TimeSpan.FromSeconds(-1), inProgress: true));
            Assert.IsFalse(TransferPresentation.ShouldShowEta(TimeSpan.Zero, inProgress: true));
        }

        [Test]
        public void MeaningfulRemainingTime_Null_IsNull()
        {
            Assert.IsNull(TransferPresentation.MeaningfulRemainingTime((TimeSpan?)null));
            Assert.IsNull(TransferPresentation.MeaningfulRemainingTime((TimeSpan?)null, inProgress: true));
        }

        [Test]
        public void MeaningfulRemainingTime_DefaultOverloadAssumesInProgress()
        {
            var eta = TimeSpan.FromMinutes(2);
            Assert.AreEqual(
                TransferPresentation.MeaningfulRemainingTime(eta, inProgress: true),
                TransferPresentation.MeaningfulRemainingTime(eta));
            Assert.IsNull(TransferPresentation.MeaningfulRemainingTime(TimeSpan.Zero));
        }

        // --- Preference AND real transfer state (the row, not just the setting) ---

        private static readonly TransferStates[] InactiveEtaStates = new[]
        {
            TransferStates.Cancelled,           // paused
            TransferStates.Cancelled | TransferStates.Completed,
            TransferStates.Queued,
            TransferStates.Queued | TransferStates.Remotely,
            TransferStates.Requested,
            TransferStates.Succeeded | TransferStates.Completed,
            TransferStates.Errored | TransferStates.Completed,
            TransferStates.TimedOut | TransferStates.Completed,
            TransferStates.Rejected | TransferStates.Completed,
            TransferStates.None,
        };

        [Test]
        public void ShouldQueryEta_RequiresPreferenceAndInProgressState()
        {
            Assert.IsTrue(TransferPresentation.ShouldQueryEta(showTimeRemaining: true, TransferStates.InProgress));
            Assert.IsFalse(TransferPresentation.ShouldQueryEta(showTimeRemaining: false, TransferStates.InProgress));
        }

        [Test]
        public void ShouldQueryEta_StaleEtaOnNonActiveState_IsNotQueried()
        {
            foreach (var state in InactiveEtaStates)
            {
                Assert.IsFalse(
                    TransferPresentation.ShouldQueryEta(showTimeRemaining: true, state),
                    $"{state} must not query the engine ETA");
            }
        }

        [Test]
        public void MeaningfulRemainingTime_StaleEtaOnNonActiveState_IsHidden()
        {
            var stale = TimeSpan.FromSeconds(90);
            foreach (var state in InactiveEtaStates)
            {
                Assert.IsNull(
                    TransferPresentation.MeaningfulRemainingTime(stale, showTimeRemaining: true, state),
                    $"{state} must hide a stale ETA");
            }
        }

        [Test]
        public void MeaningfulRemainingTime_PreferenceOff_IsHidden()
        {
            var eta = TimeSpan.FromSeconds(90);
            Assert.IsNull(TransferPresentation.MeaningfulRemainingTime(eta, showTimeRemaining: false, TransferStates.InProgress));
            foreach (var state in InactiveEtaStates)
            {
                Assert.IsNull(TransferPresentation.MeaningfulRemainingTime(eta, showTimeRemaining: false, state));
            }
        }

        [Test]
        public void MeaningfulRemainingTime_PreferenceOnAndInProgress_IsVisible()
        {
            var eta = TimeSpan.FromSeconds(90);
            Assert.AreEqual(eta, TransferPresentation.MeaningfulRemainingTime(eta, showTimeRemaining: true, TransferStates.InProgress));
            Assert.IsNull(TransferPresentation.MeaningfulRemainingTime(TimeSpan.Zero, showTimeRemaining: true, TransferStates.InProgress));
            Assert.IsNull(TransferPresentation.MeaningfulRemainingTime(null, showTimeRemaining: true, TransferStates.InProgress));
        }

        /// <summary>
        /// Mirrors what the transfer row does: only ask the item for its estimate when the pure
        /// policy allows it, so a paused/queued/finished row never even calls GetRemainingTime.
        /// </summary>
        private sealed class CountingTransferItem : Seeker.ITransferItem
        {
            private readonly TimeSpan? _staleEta;
            public int RemainingTimeCalls { get; private set; }

            public CountingTransferItem(TimeSpan? staleEta)
            {
                _staleEta = staleEta;
            }

            public string GetFolderName() => "folder";
            public string GetUsername() => "user";
            public double GetAvgSpeed() => 0;
            public int GetQueueLength() => int.MaxValue;
            public bool IsUpload() => false;

            public TimeSpan? GetRemainingTime()
            {
                RemainingTimeCalls++;
                return _staleEta;
            }
        }

        [Test]
        public void RowGate_DoesNotQueryEstimateWhenPreferenceOffOrStateInactive()
        {
            var eta = TimeSpan.FromSeconds(90);
            foreach (bool showPreference in new[] { false, true })
            {
                foreach (var state in InactiveEtaStates)
                {
                    var item = new CountingTransferItem(eta);
                    TimeSpan? remaining = null;
                    if (TransferPresentation.ShouldQueryEta(showPreference, state))
                    {
                        remaining = TransferPresentation.MeaningfulRemainingTime(item.GetRemainingTime(), showPreference, state);
                    }

                    Assert.IsNull(remaining, $"{state} (preference {showPreference}) must stay hidden");
                    Assert.AreEqual(0, item.RemainingTimeCalls, $"{state} (preference {showPreference}) must not call GetRemainingTime");
                }
            }
        }

        [Test]
        public void RowGate_QueriesAndShowsEstimateOnlyForInProgressWithPreferenceOn()
        {
            var eta = TimeSpan.FromSeconds(90);
            var shown = new CountingTransferItem(eta);
            Assert.IsTrue(TransferPresentation.ShouldQueryEta(showTimeRemaining: true, TransferStates.InProgress));
            Assert.AreEqual(eta, TransferPresentation.MeaningfulRemainingTime(shown.GetRemainingTime(), true, TransferStates.InProgress));
            Assert.AreEqual(1, shown.RemainingTimeCalls);

            var hidden = new CountingTransferItem(eta);
            Assert.IsFalse(TransferPresentation.ShouldQueryEta(showTimeRemaining: false, TransferStates.InProgress));
            Assert.AreEqual(0, hidden.RemainingTimeCalls);
        }

        [Test]
        public void IsEtaStateActive_MatchesOnlyInProgress()
        {
            Assert.IsTrue(TransferPresentation.IsEtaStateActive(TransferStates.InProgress));
            Assert.IsTrue(TransferPresentation.IsEtaStateActive(TransferStates.InProgress | TransferStates.Initializing));
            foreach (var state in InactiveEtaStates)
            {
                Assert.IsFalse(TransferPresentation.IsEtaStateActive(state), $"{state} is not an active ETA state");
            }
        }
    }
}
