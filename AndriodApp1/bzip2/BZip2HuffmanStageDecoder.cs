// Bzip2 library for .net
// By Jaime Olivares
// Location: http://github.com/jaime-olivares/bzip2
// Ported from the Java implementation by Matthew Francis: https://github.com/MateuszBartosiewicz/bzip2

using System;

namespace Bzip2
{
    /// <summary>
    /// A decoder for the BZip2 Huffman coding stage
    /// </summary>
    internal class BZip2HuffmanStageDecoder
    {
        #region Private fields
        // The BZip2BitInputStream from which Huffman codes are read
		private readonly BZip2BitInputStream bitInputStream;

        // The longest Huffman code length accepted by the decoder
        private const int HUFFMAN_DECODE_MAXIMUM_CODE_LENGTH = 23;

        // The Huffman table number to use for each group of 50 symbols
        private readonly byte[] selectors;

		// The minimum code length for each Huffman table
		private readonly int[] minimumLengths = new int[BZip2BlockDecompressor.HUFFMAN_MAXIMUM_TABLES];

		/**
	     * An array of values for each Huffman table that must be subtracted from the numerical value of
	     * a Huffman code of a given bit length to give its canonical code index
	     */
		private readonly int[,] codeBases = new int[BZip2BlockDecompressor.HUFFMAN_MAXIMUM_TABLES, HUFFMAN_DECODE_MAXIMUM_CODE_LENGTH + 2];

		/**
	     * An array of values for each Huffman table that gives the highest numerical value of a Huffman
	     * code of a given bit length
	     */
		private readonly int[,] codeLimits = new int[BZip2BlockDecompressor.HUFFMAN_MAXIMUM_TABLES, HUFFMAN_DECODE_MAXIMUM_CODE_LENGTH + 1];

		// A mapping for each Huffman table from canonical code index to output symbol
		private readonly int[,] codeSymbols = new int[BZip2BlockDecompressor.HUFFMAN_MAXIMUM_TABLES, BZip2MTFAndRLE2StageEncoder.HUFFMAN_MAXIMUM_ALPHABET_SIZE];

		// The Huffman table for the current group
		private int currentTable;

		// The index of the current group within the selectors array
		private int groupIndex = -1;

		// The byte position within the current group. A new group is selected every 50 decoded bytes
		private int groupPosition = -1;
        #endregion

        #region Public methods
        /**
         * Public constructor
		 * @param bitInputStream The BZip2BitInputStream from which Huffman codes are read
		 * @param alphabetSize The total number of codes (uniform for each table)
		 * @param tableCodeLengths The Canonical Huffman code lengths for each table
		 * @param selectors The Huffman table number to use for each group of 50 symbols
		 */
        public BZip2HuffmanStageDecoder(BZip2BitInputStream bitInputStream, int alphabetSize, byte[,] tableCodeLengths, byte[] selectors)
        {
            this.bitInputStream = bitInputStream;
            this.selectors = selectors;
            this.currentTable = this.selectors[0];
            this.CreateHuffmanDecodingTables(alphabetSize, tableCodeLengths);
        }

        /**
         * Decodes and returns the next symbol
         * @return The decoded symbol
         * Exception if the end of the input stream is reached while decoding
         */
        public int NextSymbol()
        {
            // Move to next group selector if required
            if (((++this.groupPosition % 50) == 0))
            {
                this.groupIndex++;
                if (this.groupIndex == this.selectors.Length)
                    throw new Exception("Error decoding BZip2 block");

                this.currentTable = this.selectors[this.groupIndex] & 0xff;
            }

            var codeLength = this.minimumLengths[currentTable];

            // Starting with the minimum bit length for the table, read additional bits one at a time
            // until a complete code is recognised
            for (uint codeBits = bitInputStream.ReadBits(codeLength); codeLength <= HUFFMAN_DECODE_MAXIMUM_CODE_LENGTH; codeLength++)
            {
                if (codeBits <= this.codeLimits[currentTable, codeLength])
                {
                    // Convert the code to a symbol index and return
                    return this.codeSymbols[currentTable, codeBits - this.codeBases[currentTable, codeLength]];
                }
                codeBits = (codeBits << 1) | bitInputStream.ReadBits(1);
            }

            // A valid code was not recognised
            throw new Exception("Error decoding BZip2 block");
        }
        #endregion

        #region Private methods
        /**
	     * Constructs Huffman decoding tables from lists of Canonical Huffman code lengths
	     * @param alphabetSize The total number of codes (uniform for each table)
	     * @param tableCodeLengths The Canonical Huffman code lengths for each table
	     */
		private void CreateHuffmanDecodingTables (int alphabetSize,  byte[,] tableCodeLengths) {

			for (int table = 0; table < tableCodeLengths.GetLength(0); table++)
            {
				int minimumLength = HUFFMAN_DECODE_MAXIMUM_CODE_LENGTH;
				int maximumLength = 0;

				// Find the minimum and maximum code length for the table
				for (int i = 0; i < alphabetSize; i++)
                {
					maximumLength = Math.Max (tableCodeLengths[table,i], maximumLength);
					minimumLength = Math.Min (tableCodeLengths[table,i], minimumLength);
				}
				this.minimumLengths[table] = minimumLength;

				// Calculate the first output symbol for each code length
				for (int i = 0; i < alphabetSize; i++)
                {
					this.codeBases[table,tableCodeLengths[table,i] + 1]++;
				}
				for (int i = 1; i < HUFFMAN_DECODE_MAXIMUM_CODE_LENGTH + 2; i++)
                {
					this.codeBases[table,i] += this.codeBases[table,i - 1];
				}

				// Calculate the first and last Huffman code for each code length (codes at a given length are sequential in value)
				for (int i = minimumLength, code=0; i <= maximumLength; i++)
                {
					int base1 = code;
					code += this.codeBases[table,i + 1] - this.codeBases[table,i];
					this.codeBases[table,i] = base1 - this.codeBases[table,i];
					this.codeLimits[table,i] = code - 1;
					code <<= 1;
				}

				// Populate the mapping from canonical code index to output symbol
				for (int bitLength = minimumLength, codeIndex = 0; bitLength <= maximumLength; bitLength++) {
					for (int symbol = 0; symbol < alphabetSize; symbol++) {
						if (tableCodeLengths[table,symbol] == bitLength) 
							this.codeSymbols[table,codeIndex++] = symbol;
					}
				}
			}
		}
        #endregion
    }
}
