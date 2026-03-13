using NUnit.Framework;
using Seeker;
using System.IO;

namespace UnitTestCommon
{
    public class MicroTagReaderTests
    {
        private MicroTagReader _reader;

        private static string SamplePath(string filename) =>
            Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "MicroTagReader", filename);

        [SetUp]
        public void SetUp()
        {
            _reader = new MicroTagReader(null); // null logger: catch blocks are no-ops
        }

        // ── FLAC ─────────────────────────────────────────────────────────────

        [Test]
        public void GetFlacMetadata_Mono_44100_16bit()
        {
            // flac1sMono.flac — tinytag: samplerate=44100, bitdepth=16
            using var stream = File.OpenRead(SamplePath("flac1sMono.flac"));
            _reader.GetFlacMetadata(stream, out int sampleRate, out int bitDepth);
            Assert.That(sampleRate, Is.EqualTo(44100));
            Assert.That(bitDepth, Is.EqualTo(16));
        }

        [Test]
        public void GetFlacMetadata_Stereo_44100_16bit()
        {
            // flac1.5sStereo.flac — tinytag: samplerate=44100, bitdepth=16
            using var stream = File.OpenRead(SamplePath("flac1.5sStereo.flac"));
            _reader.GetFlacMetadata(stream, out int sampleRate, out int bitDepth);
            Assert.That(sampleRate, Is.EqualTo(44100));
            Assert.That(bitDepth, Is.EqualTo(16));
        }

        [Test]
        public void GetFlacMetadata_NoTags()
        {
            // no-tags.flac — tinytag: samplerate=44100, bitdepth=16
            using var stream = File.OpenRead(SamplePath("no-tags.flac"));
            _reader.GetFlacMetadata(stream, out int sampleRate, out int bitDepth);
            Assert.That(sampleRate, Is.EqualTo(44100));
            Assert.That(bitDepth, Is.EqualTo(16));
        }

        [Test]
        public void GetFlacMetadata_WithPaddedId3Header()
        {
            // with_padded_id3_header.flac — exercises the ID3-prefix skip path
            // tinytag: samplerate=44100, bitdepth=16
            using var stream = File.OpenRead(SamplePath("with_padded_id3_header.flac"));
            _reader.GetFlacMetadata(stream, out int sampleRate, out int bitDepth);
            Assert.That(sampleRate, Is.EqualTo(44100));
            Assert.That(bitDepth, Is.EqualTo(16));
        }

        // ── AIFF ─────────────────────────────────────────────────────────────

        [Test]
        public void GetAiffMetadata_Tagged_44100_16bit_1second()
        {
            // test-tagged.aiff — tinytag: samplerate=44100, bitdepth=16, duration=1.0
            // duration truncates via (int)(totalFrames / sample_rate)
            using var stream = File.OpenRead(SamplePath("test-tagged.aiff"));
            _reader.GetAiffMetadata(stream, out int sampleRate, out int bitDepth, out int durationSeconds);
            Assert.That(sampleRate, Is.EqualTo(44100));
            Assert.That(bitDepth, Is.EqualTo(16));
            Assert.That(durationSeconds, Is.EqualTo(1));
        }

        [Test]
        public void GetAiffMetadata_Minimal_44100_16bit_ZeroDuration()
        {
            // test.aiff — tinytag: samplerate=44100, bitdepth=16, duration=0.0
            using var stream = File.OpenRead(SamplePath("test.aiff"));
            _reader.GetAiffMetadata(stream, out int sampleRate, out int bitDepth, out int durationSeconds);
            Assert.That(sampleRate, Is.EqualTo(44100));
            Assert.That(bitDepth, Is.EqualTo(16));
            Assert.That(durationSeconds, Is.EqualTo(0));
        }

        [Test]
        public void GetAiffMetadata_8bit_11025hz()
        {
            // pluck-pcm8.aiff — tinytag: samplerate=11025, bitdepth=8, duration=0.299...
            // 0.299 truncates to 0
            using var stream = File.OpenRead(SamplePath("pluck-pcm8.aiff"));
            _reader.GetAiffMetadata(stream, out int sampleRate, out int bitDepth, out int durationSeconds);
            Assert.That(sampleRate, Is.EqualTo(11025));
            Assert.That(bitDepth, Is.EqualTo(8));
            Assert.That(durationSeconds, Is.EqualTo(0));
        }

        [Test]
        public void GetAiffMetadata_8bit_8000hz_ExtraTags()
        {
            // aiff_extra_tags.aiff — tinytag: samplerate=8000, bitdepth=8, duration=2.176
            // 2.176 truncates to 2
            using var stream = File.OpenRead(SamplePath("aiff_extra_tags.aiff"));
            _reader.GetAiffMetadata(stream, out int sampleRate, out int bitDepth, out int durationSeconds);
            Assert.That(sampleRate, Is.EqualTo(8000));
            Assert.That(bitDepth, Is.EqualTo(8));
            Assert.That(durationSeconds, Is.EqualTo(2));
        }

        // ── MP3 ──────────────────────────────────────────────────────────────

        [Test]
        public void GetMp3Metadata_Cbr_56kbps()
        {
            // silence-44khz-56k-mono-1s.mp3 — tinytag: bitrate=56.0 kbps, CBR
            // CBR path: bitrate = frame_bitrate * 1000 = 56 * 1000 = 56000
            using var stream = File.OpenRead(SamplePath("silence-44khz-56k-mono-1s.mp3"));
            _reader.GetMp3Metadata(stream, true_duration: -1, true_size: 0, out int bitrate);
            Assert.That(bitrate, Is.EqualTo(56000));
        }

        [Test]
        public void GetMp3Metadata_Cbr_32kbps()
        {
            // silence-22khz-mono-1s.mp3 — tinytag: bitrate=32.0 kbps, CBR
            // CBR path: bitrate = frame_bitrate * 1000 = 32 * 1000 = 32000
            using var stream = File.OpenRead(SamplePath("silence-22khz-mono-1s.mp3"));
            _reader.GetMp3Metadata(stream, true_duration: -1, true_size: 0, out int bitrate);
            Assert.That(bitrate, Is.EqualTo(32000));
        }

        [Test]
        public void GetMp3Metadata_VbrXingHeader()
        {
            // vbr_xing_header.mp3 — tinytag: bitrate=186.04 kbps
            // Xing path: bitrate = (int)(byte_count*8/duration/1000)*1000 → 186000
            using var stream = File.OpenRead(SamplePath("vbr_xing_header.mp3"));
            _reader.GetMp3Metadata(stream, true_duration: -1, true_size: 0, out int bitrate);
            Assert.That(bitrate, Is.EqualTo(186000));
        }
    }
}
