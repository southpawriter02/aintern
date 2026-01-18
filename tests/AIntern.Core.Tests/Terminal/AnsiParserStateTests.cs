using AIntern.Core.Terminal;
using Xunit;

namespace AIntern.Core.Tests.Terminal;

/// <summary>
/// Unit tests for <see cref="AnsiParserState"/>.
/// </summary>
public class AnsiParserStateTests
{
    [Fact]
    public void Ground_IsDefaultValue()
    {
        // Arrange & Act
        var state = default(AnsiParserState);

        // Assert
        Assert.Equal(AnsiParserState.Ground, state);
    }

    [Fact]
    public void AllStates_HaveExpectedCount()
    {
        // Arrange
        var values = Enum.GetValues<AnsiParserState>();

        // Assert - Should have 12 states per design spec
        Assert.Equal(12, values.Length);
    }

    [Fact]
    public void AllStates_AreUnique()
    {
        // Arrange
        var values = Enum.GetValues<AnsiParserState>();

        // Assert
        Assert.Equal(values.Length, values.Distinct().Count());
    }

    [Theory]
    [InlineData(AnsiParserState.Ground, 0)]
    [InlineData(AnsiParserState.Escape, 1)]
    [InlineData(AnsiParserState.EscapeIntermediate, 2)]
    [InlineData(AnsiParserState.CsiEntry, 3)]
    [InlineData(AnsiParserState.CsiParam, 4)]
    [InlineData(AnsiParserState.CsiIntermediate, 5)]
    [InlineData(AnsiParserState.OscString, 6)]
    [InlineData(AnsiParserState.DcsEntry, 7)]
    [InlineData(AnsiParserState.DcsParam, 8)]
    [InlineData(AnsiParserState.DcsIntermediate, 9)]
    [InlineData(AnsiParserState.DcsPassthrough, 10)]
    [InlineData(AnsiParserState.SosPmApcString, 11)]
    public void States_HaveExpectedValues(AnsiParserState state, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)state);
    }
}
