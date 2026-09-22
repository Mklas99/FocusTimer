namespace FocusTimer.App.Tests;

using System.Globalization;
using Avalonia;
using Avalonia.Media;
using FocusTimer.App.Converters;

public class BooleanNegationConverterTests
{
    private readonly BooleanNegationConverter _converter = new();

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Convert_GivenBoolean_ReturnsNegatedValue(bool input, bool expected)
    {
        var result = _converter.Convert(input, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-bool")]
    [InlineData(42)]
    public void Convert_GivenNonBoolean_ReturnsUnsetValue(object? input)
    {
        var result = _converter.Convert(input, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.Equal(AvaloniaProperty.UnsetValue, result);
    }

    [Fact]
    public void ConvertBack_IsItsOwnInverse()
    {
        var result = _converter.ConvertBack(true, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.Equal(false, result);
    }
}

public class BooleanToAngleConverterTests
{
    private readonly BooleanToAngleConverter _converter = new();

    [Fact]
    public void Convert_GivenTrue_Returns90Degrees()
    {
        var result = _converter.Convert(true, typeof(double), null, CultureInfo.InvariantCulture);

        Assert.Equal(90d, result);
    }

    [Fact]
    public void Convert_GivenFalse_ReturnsZero()
    {
        var result = _converter.Convert(false, typeof(double), null, CultureInfo.InvariantCulture);

        Assert.Equal(0d, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("true")]
    [InlineData(1)]
    public void Convert_GivenNonBoolean_ReturnsZero(object? input)
    {
        var result = _converter.Convert(input, typeof(double), null, CultureInfo.InvariantCulture);

        Assert.Equal(0d, result);
    }

    [Fact]
    public void ConvertBack_Given90Degrees_ReturnsTrue()
    {
        var result = _converter.ConvertBack(90d, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.Equal(true, result);
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(45d)]
    [InlineData(-90d)]
    public void ConvertBack_GivenNon90Degrees_ReturnsFalse(double input)
    {
        var result = _converter.ConvertBack(input, typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.Equal(false, result);
    }

    [Fact]
    public void ConvertBack_GivenNonDouble_ReturnsFalse()
    {
        var result = _converter.ConvertBack("not-a-double", typeof(bool), null, CultureInfo.InvariantCulture);

        Assert.Equal(false, result);
    }
}

public class PlayPauseConverterTests
{
    private readonly PlayPauseConverter _converter = new();

    [Fact]
    public void Convert_GivenTrue_ReturnsPause()
    {
        var result = _converter.Convert(true, typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal("Pause", result);
    }

    [Fact]
    public void Convert_GivenFalse_ReturnsPlay()
    {
        var result = _converter.Convert(false, typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal("Play", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("running")]
    [InlineData(1)]
    public void Convert_GivenNonBoolean_ReturnsPlay(object? input)
    {
        var result = _converter.Convert(input, typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal("Play", result);
    }

    [Fact]
    public void ConvertBack_ThrowsNotImplementedException()
    {
        Assert.Throws<NotImplementedException>(
            () => _converter.ConvertBack("Play", typeof(bool), null, CultureInfo.InvariantCulture));
    }
}

public class ColorOpacityToBrushConverterTests
{
    private readonly ColorOpacityToBrushConverter _converter = new();

    [Fact]
    public void Convert_GivenColorAndOpacities_ReturnsBrushWithMultipliedOpacity()
    {
        var result = _converter.Convert(
            new List<object?> { Colors.Red, 0.5, 0.5 },
            typeof(SolidColorBrush),
            null,
            CultureInfo.InvariantCulture);

        var brush = Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(Colors.Red, brush.Color);
        Assert.Equal(0.25, brush.Opacity, 3);
    }

    [Fact]
    public void Convert_GivenHexStringColor_ParsesColor()
    {
        var result = _converter.Convert(
            new List<object?> { "#FF0000", 1.0, 1.0 },
            typeof(SolidColorBrush),
            null,
            CultureInfo.InvariantCulture);

        var brush = Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(Colors.Red, brush.Color);
    }

    [Theory]
    [InlineData(-1.0, 2.0, 0.0)] // local clamps to 0, overall clamps to 1 -> product 0
    [InlineData(2.0, 1.0, 1.0)] // local clamps to 1, overall stays 1 -> product 1
    public void Convert_GivenOutOfRangeOpacities_ClampsToUnitRange(double local, double overall, double expected)
    {
        var result = _converter.Convert(
            new List<object?> { Colors.Blue, local, overall },
            typeof(SolidColorBrush),
            null,
            CultureInfo.InvariantCulture);

        var brush = Assert.IsType<SolidColorBrush>(result);
        Assert.Equal(expected, brush.Opacity, 3);
    }

    [Fact]
    public void Convert_GivenFewerThanThreeValues_ReturnsUnsetValue()
    {
        var result = _converter.Convert(
            new List<object?> { Colors.Red, 1.0 },
            typeof(SolidColorBrush),
            null,
            CultureInfo.InvariantCulture);

        Assert.Equal(Avalonia.Data.BindingNotification.UnsetValue, result);
    }

    [Fact]
    public void Convert_GivenUnparsableColorString_ReturnsUnsetValue()
    {
        var result = _converter.Convert(
            new List<object?> { "not-a-color", 1.0, 1.0 },
            typeof(SolidColorBrush),
            null,
            CultureInfo.InvariantCulture);

        Assert.Equal(Avalonia.Data.BindingNotification.UnsetValue, result);
    }

    [Fact]
    public void Convert_GivenNonNumericOpacity_ReturnsUnsetValue()
    {
        var result = _converter.Convert(
            new List<object?> { Colors.Red, "not-a-number", 1.0 },
            typeof(SolidColorBrush),
            null,
            CultureInfo.InvariantCulture);

        Assert.Equal(Avalonia.Data.BindingNotification.UnsetValue, result);
    }

    [Fact]
    public void Convert_GivenColorAsWrongType_ReturnsUnsetValue()
    {
        var result = _converter.Convert(
            new List<object?> { 12345, 1.0, 1.0 },
            typeof(SolidColorBrush),
            null,
            CultureInfo.InvariantCulture);

        Assert.Equal(Avalonia.Data.BindingNotification.UnsetValue, result);
    }
}
