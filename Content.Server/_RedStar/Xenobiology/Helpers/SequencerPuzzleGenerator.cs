using System.Linq;
using Content.Shared._RedStar.Xenobiology.UI;
using Robust.Shared.Random;

namespace Content.Server._RedStar.Xenobiology.Helpers;

public static class SequencerPuzzleGenerator
{
    private const int MaximumAttempts = 4096;

    /// <summary>
    /// Fragments are stored in shuffled order. Order maps each fragment index to its position
    /// in the original sequence and must remain on the server.
    /// </summary>
    public sealed record Puzzle(string FamilyCode, string[] Fragments, int[] Order);

    /// <summary>
    /// Generates fragments with exactly the original sequence's adjacent overlaps.
    /// FamilyExists rejects codes already assigned to other genes. Throws if count is below two
    /// or no suitable sequence is found within the attempt limit.
    /// </summary>
    public static Puzzle Generate(IRobustRandom random, int count, Func<string, bool> familyExists)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 2);
        var length = checked(SequencerPuzzle.Overlap + SequencerPuzzle.Step * count);
        for (var attempt = 0; attempt < MaximumAttempts; attempt++)
        {
            var sequence = GenerateSequence(random, length);
            var family = sequence[..SequencerPuzzle.FamilyLength];
            if (familyExists(family))
                continue;

            var pieces = Enumerable.Range(0, count)
                .Select(i => sequence.Substring(i * SequencerPuzzle.Step, SequencerPuzzle.ReadLength))
                .ToArray();
            if (!Unambiguous(pieces))
                continue;

            var order = Enumerable.Range(0, count).ToArray();
            random.Shuffle(order);
            return new Puzzle(family, order.Select(index => pieces[index]).ToArray(), order);
        }

        throw new InvalidOperationException($"Could not generate an unambiguous sequencing puzzle after {MaximumAttempts} attempts.");
    }

    private static string GenerateSequence(IRobustRandom random, int length)
    {
        var result = new char[length];
        const string bases = "ACGT";
        for (var i = 0; i < length; i++)
        {
            result[i] = bases[random.Next(bases.Length)];
        }

        return new string(result);
    }

    private static bool Unambiguous(string[] pieces)
    {
        for (var i = 0; i < pieces.Length; i++)
        for (var j = 0; j < pieces.Length; j++)
        {
            if (pieces[i][SequencerPuzzle.Step..] == pieces[j][..SequencerPuzzle.Overlap] && j != i + 1)
                return false;
        }

        return true;
    }
}
