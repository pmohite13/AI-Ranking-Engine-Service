using System.Buffers;
using System.Security.Cryptography;

namespace AI.Ranking.Engine.Infrastructure.IO;

/// <summary>
/// Reads a bounded number of bytes from a stream (aligned with declared upload size), materializing once for hashing and format parsing.
/// </summary>
internal static class BoundedDocumentMaterializer
{
    /// <summary>
    /// Reads at most <paramref name="maxBytesInclusive" /> bytes, returns the buffer and its SHA-256 hash.
    /// </summary>
    public static async Task<(byte[] Bytes, byte[] Sha256)> ReadAndHashAsync(
        Stream stream,
        long maxBytesInclusive,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (maxBytesInclusive <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxBytesInclusive));
        }

        var cap = (int)Math.Min(maxBytesInclusive, int.MaxValue);
        using var ms = new MemoryStream(cap);
        var buffer = ArrayPool<byte>.Shared.Rent(8192);
        try
        {
            var remaining = maxBytesInclusive;
            while (remaining > 0)
            {
                var toRead = (int)Math.Min(remaining, buffer.Length);
                var read = await stream.ReadAsync(buffer.AsMemory(0, toRead), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }

                await ms.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                remaining -= read;
            }

            var bytes = ms.ToArray();
            var sha = SHA256.HashData(bytes);
            return (bytes, sha);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
