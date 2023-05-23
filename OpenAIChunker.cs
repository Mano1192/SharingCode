using TiktokenSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IMEG_BIM_Chatbot.ParsingEngine.Parsers;

public class OpenAIChunker
{
    // Global variables
    private static readonly TikToken
        tokenizer = TikToken.GetEncoding("cl100k_base"); // The encoding scheme to use for tokenization

    // Constants
    private static int CHUNK_SIZE = 75; // The target size of each text chunk in tokens
    private static int MIN_CHUNK_SIZE_CHARS = 350; // The minimum size of each text chunk in characters
    private static int MIN_CHUNK_LENGTH_TO_EMBED = 5; // Discard chunks shorter than this

    private static readonly int
        EMBEDDINGS_BATCH_SIZE = int.Parse("128"); // The number of embeddings to request at a time

    private static int MAX_NUM_CHUNKS = 10000; // The maximum number of chunks to generate from a text

    /// <summary>
    ///     Split a text into chunks of ~CHUNK_SIZE tokens, based on punctuation and newline boundaries.
    ///     Args:
    ///         text: The text to split into chunks.
    ///         chunk_token_size: The target size of each chunk in tokens, or None to use the default CHUNK_SIZE.
    ///     Returns:
    ///         A list of text chunks, each of which is a string of ~CHUNK_SIZE tokens.
    /// </summary>
    /// <param name="text">input text to chunk</param>
    /// <param name="chunkTokenSize">set the token chunk size, default is 128</param>
    /// <returns></returns>
    public async static Task<string[]> GetTextChunks(string text, int? chunkTokenSize = null)
    {
        // CHUNK_SIZE = 128;
        // MIN_CHUNK_SIZE_CHARS = 32;
        // MIN_CHUNK_LENGTH_TO_EMBED = 64;
        // MAX_NUM_CHUNKS = 100;

        // Return an empty list if the text is empty or whitespace
        if (string.IsNullOrWhiteSpace(text))
        {
            return new string[] { };
        }

        // Tokenize the text
        var tokens = tokenizer.Encode(text, disallowedSpecial: Enumerable.Empty<string>());

        // Initialize an empty list of chunks
        var chunks = new List<string>();

        // Use the provided chunk token size or the default one
        var chunkSize = chunkTokenSize ?? CHUNK_SIZE;

        // Initialize a counter for the number of chunks
        var numChunks = 0;

        // Loop until all tokens are consumed
        while (tokens.Count > 0 && numChunks < MAX_NUM_CHUNKS)
        {
            // Take the first chunkSize tokens as a chunk
            var chunk = tokens.Take(chunkSize).ToList();

            // Decode the chunk into text
            var chunkText = tokenizer.Decode(chunk);

            // Skip the chunk if it is empty or whitespace
            if (string.IsNullOrWhiteSpace(chunkText))
            {
                // Remove the tokens corresponding to the chunk text from the remaining tokens
                tokens.RemoveRange(0, chunk.Count);
                // Continue to the next iteration of the loop
                continue;
            }

            // Find the last period or punctuation mark in the chunk
            var lastPunctuation = Math.Max(
                chunkText.LastIndexOf('.'),
                Math.Max(chunkText.LastIndexOf('?'),
                    Math.Max(chunkText.LastIndexOf('!'), chunkText.LastIndexOf('\n'))));

            // If there is a punctuation mark, and the last punctuation index is before MIN_CHUNK_SIZE_CHARS
            if (lastPunctuation != -1 && lastPunctuation > MIN_CHUNK_SIZE_CHARS)
            {
                // Truncate the chunk text at the punctuation mark
                chunkText = chunkText.Substring(0, lastPunctuation + 1);
            }

            // Remove any newline characters and strip any leading or trailing whitespace
            var chunkTextToAppend = chunkText.Replace("\n", " ").Trim();

            if (chunkTextToAppend.Length > MIN_CHUNK_LENGTH_TO_EMBED)
            {
                // Append the chunk text to the list of chunks
                chunks.Add(chunkTextToAppend);
            }

            // Remove the tokens corresponding to the chunk text from the remaining tokens
            tokens.RemoveRange(0, tokenizer.Encode(chunkText, disallowedSpecial: Enumerable.Empty<string>()).Count());

            // Increment the number of chunks
            numChunks++;
        }

        // Handle the remaining tokens
        if (tokens.Count > 0)
        {
            var remainingText = tokenizer.Decode(tokens).Replace("\n", " ").Trim();
            if (remainingText.Length > MIN_CHUNK_LENGTH_TO_EMBED)
            {
                chunks.Add(remainingText);
            }
        }

        return chunks.ToArray();
    }
}