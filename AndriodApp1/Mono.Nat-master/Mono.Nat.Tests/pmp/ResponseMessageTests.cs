//
// Authors:
//   Alan McGovern <alan.mcgovern@gmail.com>
//
// Copyright (C) 2020 Alan McGovern
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Net;

using NUnit.Framework;

namespace Mono.Nat.Pmp.Tests
{
    [TestFixture]
    public class ResponseMessageTests
    {
        byte[] CreateResponseData (int privatePort, int publicPort, bool tcp, int lifetime, bool success, byte? opCode = null)
        {
            var response = new byte[16];
            //version 0
            response[0] = 0;

            // operation code (tcp or udp)
            response[1] = opCode.HasValue ? opCode.Value : (byte) (128 + (tcp ? 2 : 1));

            // successful error code
            response[2] = response[3] = (byte) (success ? 0 : 1);

            // unix epoch
            response[4] = 0;
            response[5] = 0;
            response[6] = 0;
            response[7] = 1;

            // private port
            response[8] = (byte) (IPAddress.HostToNetworkOrder (privatePort) >> 16);
            response[9] = (byte) (IPAddress.HostToNetworkOrder (privatePort) >> 24);

            // public port
            response[10] = (byte) (IPAddress.HostToNetworkOrder (publicPort) >> 16);
            response[11] = (byte) (IPAddress.HostToNetworkOrder (publicPort) >> 24);

            // lifetime
            response[12] = (byte) (IPAddress.HostToNetworkOrder (lifetime) >> 0);
            response[13] = (byte) (IPAddress.HostToNetworkOrder (lifetime) >> 8);
            response[14] = (byte) (IPAddress.HostToNetworkOrder (lifetime) >> 16);
            response[15] = (byte) (IPAddress.HostToNetworkOrder (lifetime) >> 24);

            return response;
        }

        [Test]
        public void InvalidOpcode ()
        {
            var data = CreateResponseData (65500, 65501, true, 66, true, opCode: 123);
            Assert.Throws<NotSupportedException> (() => ResponseMessage.Decode (data));
        }

        [Test]
        public void HighPortNumber_TCP ()
        {
            var data = CreateResponseData (65500, 65501, true, 66, true);
            var msg = ResponseMessage.Decode (data).Mapping;
            Assert.AreEqual (65500, msg.PrivatePort);
            Assert.AreEqual (65501, msg.PublicPort);
            Assert.AreEqual (msg.Protocol, Protocol.Tcp);
            Assert.AreEqual (66, msg.Lifetime);
        }

        [Test]
        public void HighPortNumber_UDP ()
        {
            var data = CreateResponseData (65500, 65501, false, 66, true);
            var msg = ResponseMessage.Decode (data).Mapping;
            Assert.AreEqual (65500, msg.PrivatePort);
            Assert.AreEqual (65501, msg.PublicPort);
            Assert.AreEqual (msg.Protocol, Protocol.Udp);
            Assert.AreEqual (66, msg.Lifetime);
        }

        [Test]
        public void LowPortNumber_UDP ()
        {
            var data = CreateResponseData (55, 56, false, 123, true);
            var msg = ResponseMessage.Decode (data).Mapping;
            Assert.AreEqual (55, msg.PrivatePort);
            Assert.AreEqual (56, msg.PublicPort);
            Assert.AreEqual (msg.Protocol, Protocol.Udp);
            Assert.AreEqual (123, msg.Lifetime);
        }

        [Test]
        public void LowPortNumber_TCP ()
        {
            var data = CreateResponseData (55, 56, true, int.MaxValue, true);
            var msg = ResponseMessage.Decode (data).Mapping;
            Assert.AreEqual (55, msg.PrivatePort);
            Assert.AreEqual (56, msg.PublicPort);
            Assert.AreEqual (msg.Protocol, Protocol.Tcp);
            Assert.AreEqual (int.MaxValue, msg.Lifetime);
        }

    }
}