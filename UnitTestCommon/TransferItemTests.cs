using NUnit.Framework;
using Seeker;
using Soulseek;
using System;

namespace UnitTestCommon
{
    [TestFixture]
    public class TransferItemTests
    {
        // --- GetProgressForPresentation / GetBytesTransferred ---

        [Test]
        public void GetProgressForPresentation_Succeeded_Returns100_EvenWhenBytesRoundLow()
        {
            // Legacy item saved before BytesTransferred existed: Progress rounded to 99.
            var ti = new TransferItem
            {
                State = TransferStates.Completed | TransferStates.Succeeded,
                Progress = 99,
                BytesTransferred = 0,
                Size = 1000,
            };

            // Presentation is divorced from the truthful byte count.
            Assert.AreEqual(100, ti.GetProgressForPresentation());
            Assert.AreEqual(990, ti.GetBytesTransferred());
        }

        [Test]
        public void GetProgressForPresentation_Failed_Returns100_ButBytesStayTruthful()
        {
            var ti = new TransferItem
            {
                State = TransferStates.Completed | TransferStates.Errored,
                Failed = true,
                Progress = 40,
                BytesTransferred = 400,
                Size = 1000,
            };

            Assert.AreEqual(100, ti.GetProgressForPresentation());
            // GetBytesTransferred() is unchanged, so the size text stays partial.
            Assert.AreEqual(400, ti.GetBytesTransferred());
        }

        [Test]
        public void GetProgressForPresentation_InProgress_IsByteDerived()
        {
            var ti = new TransferItem
            {
                State = TransferStates.InProgress,
                BytesTransferred = 400,
                Size = 1000,
            };

            Assert.AreEqual(40, ti.GetProgressForPresentation());
            Assert.AreEqual(400, ti.GetBytesTransferred());
        }

        // --- GetRemainingTime ---

        [Test]
        public void GetRemainingTime_InProgress_ReturnsRemainingTime()
        {
            var ti = new TransferItem
            {
                State = TransferStates.InProgress,
                RemainingTime = TimeSpan.FromSeconds(42),
            };

            Assert.AreEqual(TimeSpan.FromSeconds(42), ti.GetRemainingTime());
        }

        [Test]
        public void GetRemainingTime_NotInProgress_ReturnsNull()
        {
            var ti = new TransferItem
            {
                State = TransferStates.Initializing,
                RemainingTime = TimeSpan.FromSeconds(42),
            };

            Assert.IsNull(ti.GetRemainingTime());
        }
    }
}
