using ClassicUO.Configuration;
using ClassicUO.Game;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Game;

public class WeatherAccessibilityTests
{
    [Fact]
    public void GetAccessibilityWeatherIntensityScale_WhenAccessibilityDisabled_ReturnsDefaultScale()
    {
        var profile = new Profile
        {
            AccessibilityEnabled = false,
            AnimationIntensityPercent = 25
        };

        var scale = Weather.GetAccessibilityWeatherIntensityScale(profile);

        scale.Should().Be(1.0f);
    }

    [Fact]
    public void GetAccessibilityWeatherIntensityScale_WhenAccessibilityEnabled_ReturnsClampedNormalizedScale()
    {
        var profile = new Profile
        {
            AccessibilityEnabled = true,
            AnimationIntensityPercent = 150
        };

        var scale = Weather.GetAccessibilityWeatherIntensityScale(profile);

        scale.Should().Be(1.0f);
    }
}
