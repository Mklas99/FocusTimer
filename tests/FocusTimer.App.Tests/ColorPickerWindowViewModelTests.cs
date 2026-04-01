namespace FocusTimer.App.Tests;

using FocusTimer.App.ViewModels;

public class ColorPickerWindowViewModelTests
{
    [Fact]
    public void Constructor_GivenInvalidColor_UsesDefaultWhite()
    {
        var vm = new ColorPickerWindowViewModel("not-a-color");

        Assert.Equal("#FFFFFF", vm.SelectedColorHex);
        Assert.Equal(255, vm.Alpha);
    }

    [Theory]
    [InlineData(300, 255)]
    [InlineData(-10, 0)]
    [InlineData(127.4, 127)]
    [InlineData(127.5, 128)]
    public void Red_GivenOutOfRangeOrFractionalInput_ClampsAndRounds(double input, int expected)
    {
        var vm = new ColorPickerWindowViewModel("#000000");

        vm.Red = input;

        Assert.Equal(expected, vm.Red);
    }

    [Fact]
    public void RgbChannels_GivenUpdatedValues_ReflectInSelectedHex()
    {
        var vm = new ColorPickerWindowViewModel("#000000");

        vm.Red = 400;
        vm.Green = -50;
        vm.Blue = 32.2;

        Assert.Equal(255, vm.Red);
        Assert.Equal(0, vm.Green);
        Assert.Equal(32, vm.Blue);
        Assert.Equal("#FF0020", vm.SelectedColorHex);
    }

    [Theory]
    [InlineData(-30, 330)]
    [InlineData(390, 30)]
    [InlineData(720, 0)]
    public void Hue_GivenAnyInput_NormalizesToExpectedRange(double input, double expected)
    {
        var vm = new ColorPickerWindowViewModel("#112233");

        vm.Hue = input;

        Assert.InRange(vm.Hue, expected - 1, expected + 1);
    }

    [Fact]
    public void PercentChannels_GivenOutOfRangeInput_ClampBetweenZeroAndHundred()
    {
        var vm = new ColorPickerWindowViewModel("#112233");

        vm.Hue = -30;
        vm.SaturationPercent = 130;
        vm.ValuePercent = -5;

        Assert.InRange(vm.Hue, 329, 331);
        Assert.InRange(vm.SaturationPercent, 0, 100);
        Assert.Equal(0, vm.ValuePercent);
    }

    [Fact]
    public void ColorHex_GivenInvalidValue_DoesNotChangeSelectedColor()
    {
        var vm = new ColorPickerWindowViewModel("#123456");
        var before = vm.SelectedColorHex;

        vm.ColorHex = "#GGGGGG";

        Assert.Equal(before, vm.SelectedColorHex);
    }

    [Fact]
    public void ColorHex_GivenValidArgbValue_UpdatesAlphaAndSelectedHex()
    {
        var vm = new ColorPickerWindowViewModel("#FFFFFF");

        vm.ColorHex = "#80112233";

        Assert.Equal(128, vm.Alpha);
        Assert.Equal("#80112233", vm.SelectedColorHex);
    }
}
