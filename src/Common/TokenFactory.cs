// <copyright file="TokenFactory.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek
{
    /// <summary>
    ///     Generates unique tokens for network operations.
    /// </summary>
    internal sealed class TokenFactory : ITokenFactory
    {
        private readonly object syncRoot = new object();
        private int current;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TokenFactory"/> class.
        /// </summary>
        /// <param name="start">The optional starting value.</param>
        public TokenFactory(int start = 0)
        {
            current = start;
        }

        /// <summary>
        ///     Gets the next token.
        /// </summary>
        /// <remarks>
        ///     <para>Tokens are returned sequentially and the token value rolls over to 0 when it has reached <see cref="int.MaxValue"/>.</para>
        ///     <para>This operation is thread safe.</para>
        /// </remarks>
        /// <returns>The next token.</returns>
        /// <threadsafety instance="true"/>
        public int NextToken()
        {
            lock (syncRoot)
            {
                var retVal = current;
                current = current == int.MaxValue ? 0 : current + 1;
                return retVal;
            }
        }
    }
}