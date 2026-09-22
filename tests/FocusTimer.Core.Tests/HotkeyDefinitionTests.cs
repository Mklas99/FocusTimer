namespace FocusTimer.Core.Tests;

using FocusTimer.Core.Models;

public class HotkeyDefinitionTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_GivenNullOrWhitespace_ReturnsNull(string? input)
    {
        Assert.Null(HotkeyDefinition.Parse(input));
    }

    [Theory]
    [InlineData("S")]
    [InlineData("Ctrl")]
    public void Parse_GivenSinglePart_ReturnsNull(string input)
    {
        Assert.Null(HotkeyDefinition.Parse(input));
    }

    [Fact]
    public void Parse_GivenNoKeyPart_ReturnsNull()
    {
        Assert.Null(HotkeyDefinition.Parse("Ctrl+Alt"));
    }

    [Fact]
    public void Parse_GivenMultiCharacterKey_ReturnsNull()
    {
        Assert.Null(HotkeyDefinition.Parse("Ctrl+Esc"));
    }

    [Fact]
    public void Parse_GivenValidCtrlAltKey_ReturnsExpectedModifiersAndKeyCode()
    {
        var result = HotkeyDefinition.Parse("Ctrl+Alt+T");

        Assert.NotNull(result);
        Assert.Equal(HotkeyModifiers.Control | HotkeyModifiers.Alt, result!.Value.Modifiers);
        Assert.Equal('T', (char)result.Value.KeyCode);
    }

    [Theory]
    [InlineData("ctrl+alt+t")]
    [InlineData("CTRL+ALT+T")]
    [InlineData("Control+Alt+t")]
    public void Parse_GivenDifferentCasing_IsCaseInsensitive(string input)
    {
        var result = HotkeyDefinition.Parse(input);

        Assert.NotNull(result);
        Assert.Equal(HotkeyModifiers.Control | HotkeyModifiers.Alt, result!.Value.Modifiers);
        Assert.Equal('T', (char)result.Value.KeyCode);
    }

    [Theory]
    [InlineData("Win+P", HotkeyModifiers.Win)]
    [InlineData("Windows+P", HotkeyModifiers.Win)]
    [InlineData("Shift+P", HotkeyModifiers.Shift)]
    public void Parse_GivenModifierAliases_ResolvesToExpectedFlag(string input, HotkeyModifiers expected)
    {
        var result = HotkeyDefinition.Parse(input);

        Assert.NotNull(result);
        Assert.Equal(expected, result!.Value.Modifiers);
    }

    [Fact]
    public void Parse_GivenAllModifiers_CombinesFlags()
    {
        var result = HotkeyDefinition.Parse("Ctrl+Alt+Shift+Win+X");

        Assert.NotNull(result);
        Assert.Equal(
            HotkeyModifiers.Control | HotkeyModifiers.Alt | HotkeyModifiers.Shift | HotkeyModifiers.Win,
            result!.Value.Modifiers);
        Assert.Equal('X', (char)result.Value.KeyCode);
    }

    [Fact]
    public void Parse_GivenExtraWhitespaceAroundParts_TrimsAndParsesCorrectly()
    {
        var result = HotkeyDefinition.Parse(" Ctrl + Alt + T ");

        Assert.NotNull(result);
        Assert.Equal(HotkeyModifiers.Control | HotkeyModifiers.Alt, result!.Value.Modifiers);
        Assert.Equal('T', (char)result.Value.KeyCode);
    }

    [Fact]
    public void Parse_GivenTwoNonModifierParts_KeepsLastAsKey()
    {
        // Unrecognized tokens fall through to keyPart; only the last non-modifier token wins.
        var result = HotkeyDefinition.Parse("Ctrl+A+B");

        Assert.NotNull(result);
        Assert.Equal('B', (char)result!.Value.KeyCode);
    }

    [Fact]
    public void Parse_GivenLowercaseKey_NormalizesToUppercase()
    {
        var result = HotkeyDefinition.Parse("Ctrl+Alt+t");

        Assert.NotNull(result);
        Assert.Equal('T', (char)result!.Value.KeyCode);
    }

    [Fact]
    public void ToString_GivenModifiersAndKey_ProducesParsableRoundTrip()
    {
        var original = new HotkeyDefinition(HotkeyModifiers.Control | HotkeyModifiers.Shift, 'K');

        string text = original.ToString();
        var reparsed = HotkeyDefinition.Parse(text);

        Assert.NotNull(reparsed);
        Assert.Equal(original.Modifiers, reparsed!.Value.Modifiers);
        Assert.Equal(original.KeyCode, reparsed.Value.KeyCode);
    }

    [Fact]
    public void ToString_GivenNoModifiers_ReturnsBareKey()
    {
        var definition = new HotkeyDefinition(HotkeyModifiers.None, 'Q');

        Assert.Equal("Q", definition.ToString());
    }
}
