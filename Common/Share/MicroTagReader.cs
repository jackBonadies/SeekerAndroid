using Seeker.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Seeker
{
    public class MicroTagReader
    {
        public static MicroTagReader Instance { get; set; }

        private readonly ILoggerBackend _logger;

        public MicroTagReader(ILoggerBackend logger)
        {
            _logger = logger;
        }

        static readonly int[][] samplerates;
        static readonly int[] channels_per_mode;
        static readonly int[][][] bitrate_by_version_by_layer;
        static MicroTagReader()
        {
            samplerates = new int[][]
            {
                new[] { 11025, 12000, 8000 },
                Array.Empty<int>(),
                new[] { 22050, 24000, 16000 },
                new[] { 44100, 48000, 32000 },
            };

            int[] v1l1 = { 0, 32, 64, 96, 128, 160, 192, 224, 256, 288, 320, 352, 384, 416, 448, 0 };
            int[] v1l2 = { 0, 32, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 384, 0 };
            int[] v1l3 = { 0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 0 };
            int[] v2l1 = { 0, 32, 48, 56, 64, 80, 96, 112, 128, 144, 160, 176, 192, 224, 256, 0 };
            int[] v2l2 = { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0 };
            int[] v2l3 = v2l2; // v2.5 layer3 shares values with v2 layer3

            bitrate_by_version_by_layer = new int[][][]
            {
                new int[][] { null, v2l3, v2l2, v2l1 },  // MPEG 2.5
                null,                                       // reserved
                new int[][] { null, v2l3, v2l2, v2l1 },  // MPEG 2
                new int[][] { null, v1l3, v1l2, v1l1 },  // MPEG 1
            };
            //samples_per_frame = 1152  # the default frame size for mp3

            channels_per_mode = new[] { 2, 2, 2, 1 };
        }

        /// <summary>
        /// used since android messes up very badly when it comes to vbr mp3s
        /// </summary>
        public void GetMp3Metadata(System.IO.Stream stream, int true_duration, long true_size, out int bitrate)
        {
            bitrate = -1;
            try
            {
                //int max_estimation_frames = 30 * 44100 / 1152;

                double bitrate_accumulator = 0;
                int frame_size_accu = 0;
                List<double> last_bitrates = new List<double>();
                //int audio_offset = -1;
                byte[] header = new byte[4];
                stream.Read(header, 0, 4);
                bool startsWithID3 = header.Take(3).SequenceEqual(System.Text.Encoding.ASCII.GetBytes("ID3"));
                //{
                //its technically incorrect, but flac files can have ID3 tags.
                //I found the sample file to test in tinytag repo.  otherwise I think this is rare.
                byte[] id3Header = new byte[10];
                if ((stream.Read(id3Header, 0, 10) == 10))
                {
                    if (startsWithID3)
                    {
                        int size = id3Header[2] * 128 * 128 * 128 + id3Header[3] * 128 * 128 + id3Header[4] * 128 + id3Header[5];
                        stream.Seek(size, System.IO.SeekOrigin.Begin);
                    }
                    else
                    {
                        stream.Seek(0, System.IO.SeekOrigin.Begin);
                    }
                }
                int frames = 0;
                while (true)
                {
                    byte[] nextFour = new byte[4];
                    int read = stream.Read(nextFour, 0, 4);
                    if (read < 4)
                    {
                        return;
                    }
                    int br_id = (byte)(((nextFour[2] >> 4))) & 0x0F;
                    int sr_id = (byte)((nextFour[2] / 4)) & 0x03;
                    int padding = (nextFour[2] & 0x02) > 0 ? 1 : 0;
                    int mpeg_id = (byte)((nextFour[1] / 8)) & 0x03;
                    int layer_id = (byte)((nextFour[1] / 2)) & 0x03;
                    int channel_mode = (byte)((nextFour[3] / 64)) & 0x03;
                    int val = nextFour[0] * 256 + nextFour[1];
                    if (val <= (65504) || br_id > 14 || br_id == 0 || sr_id == 3 || layer_id == 0 || mpeg_id == 1)
                    {
                        int index = Array.IndexOf(nextFour, (byte)(0xFF));
                        if (index == -1)
                        {
                            index = nextFour.Length;
                        }
                        if (index == 0)
                        {
                            index = 1;
                        }
                        int amountToMove = index - 4;
                        stream.Seek(amountToMove, System.IO.SeekOrigin.Current); //we go backwards if need be.
                        continue;
                    }

                    int frame_bitrate = bitrate_by_version_by_layer[mpeg_id][layer_id][br_id];
                    int samplerate = samplerates[mpeg_id][sr_id];
                    int channels = channels_per_mode[channel_mode];


                    if (frames == 0)
                    {
                        byte[] lookForXing = new byte[1024];
                        stream.Read(lookForXing, 0, 1024);
                        stream.Seek(-1028, System.IO.SeekOrigin.Current);
                        byte[] toLookForXing = nextFour.Concat(lookForXing).ToArray();
                        int index = -1;
                        for (int i = 0; i < toLookForXing.Length - 4; i++)
                        {
                            if (toLookForXing[i] == (byte)(88) &&
                                toLookForXing[i + 1] == (byte)(105) &&
                                toLookForXing[i + 2] == (byte)(110) &&
                                toLookForXing[i + 3] == (byte)(103))
                            {
                                index = i;
                                break;
                            }
                        }
                        if (index != -1)
                        {
                            stream.Seek(index + 4, System.IO.SeekOrigin.Current);


                            stream.Read(nextFour, 0, 4);
                            var id3header = nextFour.ToArray();
                            int id3frames = -1;
                            int byte_count = -1;
                            if ((id3header[3] & 0x01) != 0)
                            {
                                stream.Read(nextFour, 0, 4);
                                id3frames = nextFour[0] * 256 * 256 * 256 + nextFour[1] * 256 * 256 + nextFour[2] * 256 + nextFour[3];
                            }
                            if ((id3header[3] & 0x02) != 0)
                            {
                                stream.Read(nextFour, 0, 4);
                                byte_count = nextFour[0] * 256 * 256 * 256 + nextFour[1] * 256 * 256 + nextFour[2] * 256 + nextFour[3];
                            }
                            if ((id3header[3] & 0x04) != 0)
                            {
                                byte[] next400 = new byte[400];
                                stream.Read(next400, 0, 400);
                            }
                            if ((id3header[3] & 0x08) != 0)
                            {
                                stream.Read(nextFour, 0, 4);
                            }
                            if (id3frames != -1 && byte_count != -1 && id3frames != 0)
                            {
                                double duration = id3frames * 1152 / (double)(samplerate);
                                bitrate = (int)(byte_count * 8 / duration / 1000) * 1000;
                            }
                            return;
                        }
                        else
                        {
                            stream.Seek(4, System.IO.SeekOrigin.Current);
                        }
                    }




                    frames += 1;
                    bitrate_accumulator += frame_bitrate;
                    if (frames <= 5)
                    {
                        last_bitrates.Add(frame_bitrate);
                    }


                    //if(frames==1)
                    //{
                    //    audio_offset = stream.Position;
                    //}

                    //stream.Seek(4, System.IO.SeekOrigin.Current)

                    int frame_length = (144000 * frame_bitrate) / samplerate + padding;
                    frame_size_accu += frame_length;
                    //if bitrate does not change over time its probably CBR
                    bool is_cbr = (frames == 5 && last_bitrates.Distinct().Count() == 1);
                    if (is_cbr)
                    {
                        //int audio_stream_size = stream.Position - audio_offset;
                        //int est_frame_count = audio_stream_size / (frame_size_accu / float(frames))
                        //int samples = est_frame_count * 1152;
                        //double duration = samples / (double)(samplerate);
                        bitrate = (int)(bitrate_accumulator / frames) * 1000; //works especially great for cbr
                        return;
                    }

                    if (frames > 5)
                    {
                        //dont use this estimation method for vbr... its no more accurate than size / duration... and takes way longer.
                        bitrate = (true_duration != -1) ? (int)((true_size * 8 * 1000.0 / 1024.0) / true_duration) : -1;//todo test
                        return;
                    }

                    if (frame_length > 1)
                    {
                        stream.Seek(frame_length - header.Length, System.IO.SeekOrigin.Current);
                    }
                }
                //}
                //else
                //{
                //    return;
                //}
            }
            catch (Exception e)
            {
                _logger?.Firebase("getMp3Metadata: " + e.Message + e.StackTrace);
            }
        }

        public void GetAiffMetadata(System.IO.Stream stream, out int sampleRate, out int bitDepth, out int durationSeconds)
        {
            sampleRate = -1;
            bitDepth = -1;
            durationSeconds = -1;
            try
            {
                byte[] buffer = new byte[4];
                stream.Read(buffer, 0, 4); //FORM

                if (System.Text.Encoding.ASCII.GetString(buffer) != "FORM")
                {
                    throw new Exception("malformed FORM");
                }


                stream.Read(buffer, 0, 4); //filesize UINT.
                Array.Reverse(buffer, 0, buffer.Length); //big endian
                uint fileSize = System.BitConverter.ToUInt32(buffer);

                stream.Read(buffer, 0, 4); //AIFF
                if (System.Text.Encoding.ASCII.GetString(buffer) != "AIFF")
                {
                    throw new Exception("malformed AIFF");
                }


                long comm_chunk = long.MinValue;
                long cur_position = stream.Position;
                while (cur_position < fileSize)
                {
                    // Read 4-byte chunk name
                    stream.Read(buffer, 0, 4);

                    if (System.Text.Encoding.ASCII.GetString(buffer) == "COMM")
                    {
                        comm_chunk = stream.Position - 4;
                        break;
                    }

                    // chunk size
                    stream.Read(buffer, 0, 4);
                    Array.Reverse(buffer, 0, buffer.Length); //big endian
                    uint chunkSize = System.BitConverter.ToUInt32(buffer);
                    if (chunkSize % 2 != 0)
                    {
                        chunkSize++;
                    }
                    stream.Seek(chunkSize, System.IO.SeekOrigin.Current);
                    cur_position = stream.Position;

                    //if (chunkHeader == chunkName)
                    //{
                    //	// We found a matching chunk, return the position
                    //	// of the header start
                    //	return Tell - 4;
                    //}
                    //else
                    //{
                    //	// This chunk is not the one we are looking for
                    //	// Continue the search, seeking over the chunk
                    //	uint chunkSize = ReadBlock(4).ToUInt();
                    //	// Seek forward "chunkSize" bytes
                    //	Seek(chunkSize, System.IO.SeekOrigin.Current);
                    //}
                }

                if (comm_chunk == long.MinValue)
                {
                    throw new Exception("couldnt find comm block");
                }

                stream.Seek(comm_chunk, System.IO.SeekOrigin.Begin);
                byte[] comm_buffer = new byte[26];
                stream.Read(comm_buffer, 0, 26);

                uint totalFrames = System.BitConverter.ToUInt32(comm_buffer.Skip(10).Take(4).Reverse().ToArray());
                ushort bits_per_sample = System.BitConverter.ToUInt16(comm_buffer.Skip(14).Take(2).Reverse().ToArray());
                double sample_rate = ToExtendedPrecision(comm_buffer.Skip(16).Take(10).ToArray());
                int seconds = (int)(totalFrames / sample_rate);
                sampleRate = (int)sample_rate;
                bitDepth = (int)bits_per_sample;
                durationSeconds = seconds;
            }
            catch (Exception e)
            {
                _logger?.Firebase("GetAiffMetadata: " + e.Message + e.StackTrace);
            }
        }

        /// <summary>
        /// From taglib-sharp:
        ///    Converts the first 10 bytes of the current instance to an IEEE
        ///    754 80-bit extended precision floating point number, expressed
        ///    as a <see cref="double"/>.
        /// </summary>
        /// <returns>
        ///    A <see cref="double"/> value containing the value read from the
        ///    current instance.
        /// </returns>
        private static double ToExtendedPrecision(byte[] bytesToConvert)
        {
            int exponent = ((bytesToConvert[0] & 0x7F) << 8) | bytesToConvert[1];
            ulong hiMantissa = ((ulong)bytesToConvert[2] << 24)
                               | ((ulong)bytesToConvert[3] << 16)
                               | ((ulong)bytesToConvert[4] << 8)
                               | bytesToConvert[5];
            ulong loMantissa = ((ulong)bytesToConvert[6] << 24)
                               | ((ulong)bytesToConvert[7] << 16)
                               | ((ulong)bytesToConvert[8] << 8)
                               | bytesToConvert[9];

            double f;
            if (exponent == 0 && hiMantissa == 0 && loMantissa == 0)
            {
                f = 0;
            }
            else
            {
                if (exponent == 0x7FFF)
                {
                    f = double.PositiveInfinity;
                }
                else
                {
                    exponent -= 16383;
                    f = hiMantissa * Math.Pow(2, exponent -= 31);
                    f += loMantissa * Math.Pow(2, exponent -= 32);
                }
            }

            return (bytesToConvert[0] & 0x80) != 0 ? -f : f;
        }

        public void GetApeMetadata(System.IO.Stream stream, out int sampleRate, out int bitDepth, out int durationSeconds)
        {
            sampleRate = -1;
            bitDepth = -1;
            durationSeconds = -1;
            try
            {
                byte[] a = new byte[76];
                stream.Read(a, 0, 76); //ape header
                if ((System.Text.Encoding.ASCII.GetString(a.Take(4).ToArray()) != "MAC "))
                {
                    throw new Exception("MAC  not present");
                }

                byte[] b = a.Skip(68).Take(2).ToArray();
                bitDepth = (int)System.BitConverter.ToUInt16(b);
                byte[] c = a.Skip(72).Take(4).ToArray();
                sampleRate = (int)System.BitConverter.ToUInt32(c);
                byte[] d = a.Skip(64).Take(4).ToArray();
                uint total_frames = System.BitConverter.ToUInt32(d);
                uint blocks_per_frame = System.BitConverter.ToUInt32(a.Skip(56).Take(4).ToArray());
                uint final_frame_blocks = System.BitConverter.ToUInt32(a.Skip(60).Take(4).ToArray());

                durationSeconds = (int)(((total_frames - 1) *
                     blocks_per_frame + final_frame_blocks) /
                    (double)sampleRate);
            }
            catch (Exception e)
            {
                _logger?.Firebase("GetApeMetadata: " + e.Message + e.StackTrace);
            }
        }

        public void GetFlacMetadata(System.IO.Stream stream, out int sampleRate, out int bitDepth)
        {
            sampleRate = -1;
            bitDepth = -1;
            try
            {
                byte[] header = new byte[4];
                stream.Read(header, 0, 4);
                if (header.Take(3).SequenceEqual(System.Text.Encoding.ASCII.GetBytes("ID3")))
                {
                    //its technically incorrect, but flac files can have ID3 tags.
                    //I found the sample file to test in tinytag repo.  otherwise I think this is rare.
                    //just skip over this
                    byte[] id3Header = new byte[10];
                    if ((stream.Read(id3Header, 0, 10) == 10))
                    {
                        int size = id3Header[2] * 128 * 128 * 128 + id3Header[3] * 128 * 128 + id3Header[4] * 128 + id3Header[5];
                        stream.Seek(size - 4, System.IO.SeekOrigin.Current);
                        stream.Read(header, 0, 4);
                    }
                    else
                    {
                        return;
                    }
                }
                if (!(header.SequenceEqual(System.Text.Encoding.ASCII.GetBytes("fLaC"))))
                {
                    throw new Exception("bad format");
                }
                //position is now after the fLaC

                while (stream.Read(header, 0, 4) == 4)
                {
                    int blockType = header[0] & (byte)(0x7f);
                    int isLastBlock = header[0] & (byte)(0x80);
                    int size = header[1] * 256 * 256 + header[2] * 256 + header[3];
                    if (blockType == 0)
                    {
                        byte[] stream_info_header = new byte[size];
                        if (stream.Read(stream_info_header, 0, size) != size)
                        {
                            return;
                        }
                        int offset_to_sample_rate = 10;
                        sampleRate = (stream_info_header[offset_to_sample_rate] * 256 * 256 + stream_info_header[offset_to_sample_rate + 1] * 256 + stream_info_header[offset_to_sample_rate + 2]) / 16;

                        bitDepth = ((stream_info_header[offset_to_sample_rate + 2] & (byte)(0x1)) * 16 + (stream_info_header[offset_to_sample_rate + 3] & (byte)(0xf0)) / 16) + 1;
                        return;
                    }
                    else if (isLastBlock != 0) //it will be 128
                    {
                        return;
                    }
                    else
                    {
                        //go to next block
                        stream.Seek(size, System.IO.SeekOrigin.Current);
                    }
                }
            }
            catch (Exception e)
            {
                _logger?.Firebase("getFlacMetadata: " + e.Message + e.StackTrace); //TODO: getFlacMetadata: FileDescriptor must not be null a
            }
        }
    }
}
