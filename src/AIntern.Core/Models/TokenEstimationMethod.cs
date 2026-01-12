namespace AIntern.Core.Models;

/// <summary>
/// Methods for estimating token counts.
/// </summary>
public enum TokenEstimationMethod
{
    /// <summary>
    /// Simple character-based estimation (~3.5 chars per token).
    /// Fast but less accurate. Suitable for quick estimates.
    /// </summary>
    CharacterBased,

    /// <summary>
    /// Word and punctuation based estimation.
    /// Considers word count, punctuation, newlines, and whitespace.
    /// Good balance of speed and accuracy. Default method.
    /// </summary>
    WordBased,

    /// <summary>
    /// BPE-approximate estimation simulating subword tokenization.
    /// Most accurate for code but slower. Matches common programming tokens.
    /// </summary>
    BpeApproximate
}
