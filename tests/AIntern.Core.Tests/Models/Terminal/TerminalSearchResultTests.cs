using Xunit;
using AIntern.Core.Models.Terminal;

namespace AIntern.Core.Tests.Models.Terminal;

/// <summary>
/// Unit tests for <see cref="TerminalSearchResult"/>.
/// </summary>
/// <remarks>Added in v0.5.5a.</remarks>
public sealed class TerminalSearchResultTests
{
    // ═══════════════════════════════════════════════════════════════════════
    // Position Properties Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void EndColumn_CalculatesCorrectly()
    {
        // Arrange
        var result = new TerminalSearchResult
        {
            StartColumn = 10,
            Length = 5
        };

        // Act & Assert
        Assert.Equal(15, result.EndColumn);
    }

    [Fact]
    public void EndColumn_IsStartColumnWhenLengthIsZero()
    {
        // Arrange
        var result = new TerminalSearchResult
        {
            StartColumn = 10,
            Length = 0
        };

        // Act & Assert
        Assert.Equal(10, result.EndColumn);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // IsValid Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void IsValid_ReturnsTrueForValidResult()
    {
        // Arrange
        var result = new TerminalSearchResult
        {
            Length = 5,
            MatchedText = "error"
        };

        // Act & Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void IsValid_ReturnsFalseWhenLengthIsZero()
    {
        // Arrange
        var result = new TerminalSearchResult
        {
            Length = 0,
            MatchedText = "error"
        };

        // Act & Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void IsValid_ReturnsFalseWhenMatchedTextIsEmpty()
    {
        // Arrange
        var result = new TerminalSearchResult
        {
            Length = 5,
            MatchedText = string.Empty
        };

        // Act & Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void IsValid_ReturnsFalseWhenNegativeLength()
    {
        // Arrange
        var result = new TerminalSearchResult
        {
            Length = -1,
            MatchedText = "error"
        };

        // Act & Assert
        Assert.False(result.IsValid);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Computed Position Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void IsAtLineStart_ReturnsTrueWhenStartColumnIsZero()
    {
        // Arrange
        var result = new TerminalSearchResult { StartColumn = 0 };

        // Act & Assert
        Assert.True(result.IsAtLineStart);
    }

    [Fact]
    public void IsAtLineStart_ReturnsFalseWhenStartColumnIsNotZero()
    {
        // Arrange
        var result = new TerminalSearchResult { StartColumn = 5 };

        // Act & Assert
        Assert.False(result.IsAtLineStart);
    }

    [Fact]
    public void IsAtLineEnd_ReturnsTrueWhenEndColumnEqualsLineLength()
    {
        // Arrange
        var result = new TerminalSearchResult
        {
            StartColumn = 10,
            Length = 5,
            LineContent = "Hello World!" // Length 12
        };

        // Act & Assert - EndColumn = 15, LineContent.Length = 12, so 15 >= 12
        Assert.True(result.IsAtLineEnd);
    }

    [Fact]
    public void AvailableContextBefore_ReturnsStartColumn()
    {
        // Arrange
        var result = new TerminalSearchResult { StartColumn = 15 };

        // Act & Assert
        Assert.Equal(15, result.AvailableContextBefore);
    }

    [Fact]
    public void AvailableContextAfter_ReturnsCorrectValue()
    {
        // Arrange
        var result = new TerminalSearchResult
        {
            StartColumn = 5,
            Length = 5,
            LineContent = "0123456789ABCDEF" // Length 16
        };

        // Act & Assert - EndColumn = 10, LineContent.Length = 16, so 16 - 10 = 6
        Assert.Equal(6, result.AvailableContextAfter);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetContextBefore Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetContextBefore_ReturnsCorrectContext()
    {
        // Arrange
        var result = new TerminalSearchResult
        {
            StartColumn = 10,
            LineContent = "Hello, World! How are you?"
        };

        // Act
        var context = result.GetContextBefore(10);

        // Assert - StartColumn 10 gives us characters 0-9 which is "Hello, Wor"
        Assert.Equal("Hello, Wor", context);
    }

    [Fact]
    public void GetContextBefore_AddsEllipsisWhenTruncated()
    {
        // Arrange
        var result = new TerminalSearchResult
        {
            StartColumn = 30,
            LineContent = "This is a long line with lots of text here"
        };

        // Act
        var context = result.GetContextBefore(10);

        // Assert
        Assert.StartsWith("...", context);
    }

    [Fact]
    public void GetContextBefore_ReturnsEmptyWhenAtLineStart()
    {
        // Arrange
        var result = new TerminalSearchResult
        {
            StartColumn = 0,
            LineContent = "Hello World"
        };

        // Act
        var context = result.GetContextBefore();

        // Assert
        Assert.Equal(string.Empty, context);
    }

    [Fact]
    public void GetContextBefore_ReturnsEmptyWhenLineContentIsEmpty()
    {
        // Arrange
        var result = new TerminalSearchResult
        {
            StartColumn = 5,
            LineContent = string.Empty
        };

        // Act
        var context = result.GetContextBefore();

        // Assert
        Assert.Equal(string.Empty, context);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GetContextAfter Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void GetContextAfter_ReturnsCorrectContext()
    {
        // Arrange
        var result = new TerminalSearchResult
        {
            StartColumn = 7,
            Length = 5,
            LineContent = "Hello, World! How are you today?"
        };

        // Act - EndColumn = 12, so context starts at position 12
        var context = result.GetContextAfter(10);

        // Assert - Characters 12-21 are "! How are " + ...
        Assert.Equal("! How are ...", context);
    }

    [Fact]
    public void GetContextAfter_ReturnsEmptyWhenAtLineEnd()
    {
        // Arrange
        var result = new TerminalSearchResult
        {
            StartColumn = 6,
            Length = 5,
            LineContent = "Hello World" // EndColumn = 11, Length = 11
        };

        // Act
        var context = result.GetContextAfter();

        // Assert
        Assert.Equal(string.Empty, context);
    }

    [Fact]
    public void GetContextAfter_AddsEllipsisWhenTruncated()
    {
        // Arrange
        var result = new TerminalSearchResult
        {
            StartColumn = 0,
            Length = 5,
            LineContent = "Hello World and lots more text here"
        };

        // Act
        var context = result.GetContextAfter(10);

        // Assert
        Assert.EndsWith("...", context);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ToPreviewString Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ToPreviewString_FormatsCorrectly()
    {
        // Arrange
        var result = new TerminalSearchResult
        {
            StartColumn = 10,
            Length = 5,
            MatchedText = "match",
            LineContent = "Before the match after the match"
        };

        // Act
        var preview = result.ToPreviewString(5, 5);

        // Assert
        Assert.Contains("[match]", preview);
    }

    [Fact]
    public void ToPreviewString_IncludesContextBeforeAndAfter()
    {
        // Arrange
        var result = new TerminalSearchResult
        {
            StartColumn = 7,
            Length = 5,
            MatchedText = "World",
            LineContent = "Hello, World! How are you?"
        };

        // Act
        var preview = result.ToPreviewString(7, 10);

        // Assert
        Assert.Equal("Hello, [World]! How are ...", preview);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Comparison Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void OverlapsWith_ReturnsTrueForOverlappingResults()
    {
        // Arrange
        var result1 = new TerminalSearchResult { LineIndex = 5, StartColumn = 10, Length = 10 };
        var result2 = new TerminalSearchResult { LineIndex = 5, StartColumn = 15, Length = 10 };

        // Act & Assert
        Assert.True(result1.OverlapsWith(result2));
    }

    [Fact]
    public void OverlapsWith_ReturnsFalseForDifferentLines()
    {
        // Arrange
        var result1 = new TerminalSearchResult { LineIndex = 5, StartColumn = 10, Length = 10 };
        var result2 = new TerminalSearchResult { LineIndex = 6, StartColumn = 10, Length = 10 };

        // Act & Assert
        Assert.False(result1.OverlapsWith(result2));
    }

    [Fact]
    public void OverlapsWith_ReturnsFalseForNonOverlapping()
    {
        // Arrange
        var result1 = new TerminalSearchResult { LineIndex = 5, StartColumn = 0, Length = 5 };
        var result2 = new TerminalSearchResult { LineIndex = 5, StartColumn = 10, Length = 5 };

        // Act & Assert
        Assert.False(result1.OverlapsWith(result2));
    }

    [Fact]
    public void IsBefore_ReturnsTrueForEarlierLine()
    {
        // Arrange
        var result1 = new TerminalSearchResult { LineIndex = 5, StartColumn = 10 };
        var result2 = new TerminalSearchResult { LineIndex = 10, StartColumn = 5 };

        // Act & Assert
        Assert.True(result1.IsBefore(result2));
    }

    [Fact]
    public void IsBefore_ReturnsTrueForSameLineEarlierColumn()
    {
        // Arrange
        var result1 = new TerminalSearchResult { LineIndex = 5, StartColumn = 5 };
        var result2 = new TerminalSearchResult { LineIndex = 5, StartColumn = 10 };

        // Act & Assert
        Assert.True(result1.IsBefore(result2));
    }

    [Fact]
    public void IsAfter_ReturnsTrueForLaterLine()
    {
        // Arrange
        var result1 = new TerminalSearchResult { LineIndex = 10, StartColumn = 5 };
        var result2 = new TerminalSearchResult { LineIndex = 5, StartColumn = 10 };

        // Act & Assert
        Assert.True(result1.IsAfter(result2));
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Factory Methods Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void Create_ReturnsResultWithCorrectProperties()
    {
        // Act
        var result = TerminalSearchResult.Create(
            lineIndex: 10,
            startColumn: 5,
            length: 7,
            matchedText: "testing",
            lineContent: "This testing is great"
        );

        // Assert
        Assert.Equal(10, result.LineIndex);
        Assert.Equal(5, result.StartColumn);
        Assert.Equal(7, result.Length);
        Assert.Equal("testing", result.MatchedText);
        Assert.Equal("This testing is great", result.LineContent);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Object Override Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var result = new TerminalSearchResult
        {
            LineIndex = 42,
            StartColumn = 10,
            Length = 5,
            MatchedText = "hello"
        };

        // Act
        var str = result.ToString();

        // Assert
        Assert.Equal("Line 42, Col 10-15: \"hello\"", str);
    }

    [Fact]
    public void Equals_ReturnsTrueForEqualResults()
    {
        // Arrange
        var result1 = new TerminalSearchResult
        {
            LineIndex = 10,
            StartColumn = 5,
            Length = 7,
            MatchedText = "testing"
        };
        var result2 = new TerminalSearchResult
        {
            LineIndex = 10,
            StartColumn = 5,
            Length = 7,
            MatchedText = "testing"
        };

        // Act & Assert
        Assert.Equal(result1, result2);
        Assert.True(result1.Equals(result2));
    }

    [Fact]
    public void Equals_ReturnsFalseForDifferentResults()
    {
        // Arrange
        var result1 = new TerminalSearchResult { LineIndex = 10, StartColumn = 5 };
        var result2 = new TerminalSearchResult { LineIndex = 10, StartColumn = 6 };

        // Act & Assert
        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public void GetHashCode_ReturnsSameValueForEqualResults()
    {
        // Arrange
        var result1 = new TerminalSearchResult
        {
            LineIndex = 10,
            StartColumn = 5,
            Length = 7,
            MatchedText = "testing"
        };
        var result2 = new TerminalSearchResult
        {
            LineIndex = 10,
            StartColumn = 5,
            Length = 7,
            MatchedText = "testing"
        };

        // Act & Assert
        Assert.Equal(result1.GetHashCode(), result2.GetHashCode());
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Default Values Tests
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var result = new TerminalSearchResult();

        // Assert
        Assert.Equal(0, result.LineIndex);
        Assert.Equal(0, result.StartColumn);
        Assert.Equal(0, result.Length);
        Assert.Equal(string.Empty, result.MatchedText);
        Assert.Equal(string.Empty, result.LineContent);
        Assert.False(result.IsCurrent);
    }

    [Fact]
    public void IsCurrent_CanBeModified()
    {
        // Arrange
        var result = new TerminalSearchResult();

        // Act
        result.IsCurrent = true;

        // Assert
        Assert.True(result.IsCurrent);
    }
}
