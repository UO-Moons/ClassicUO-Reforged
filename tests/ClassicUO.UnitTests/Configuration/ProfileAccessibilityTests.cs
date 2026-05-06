using ClassicUO.Configuration;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Configuration;

public class ProfileAccessibilityTests
{
    [Fact]
    public void ClampAccessibilityValues_WhenValuesOutOfRange_ShouldClampToSupportedBounds()
    {
        var profile = new Profile
        {
            UIFontScalePercent = 999,
            ChatLineSpacing = -10,
            AnimationIntensityPercent = -5
        };

        profile.ClampAccessibilityValues();

        profile.UIFontScalePercent.Should().Be(Profile.MAX_UI_FONT_SCALE_PERCENT);
        profile.ChatLineSpacing.Should().Be(Profile.MIN_CHAT_LINE_SPACING);
        profile.AnimationIntensityPercent.Should().Be(Profile.MIN_ANIMATION_INTENSITY_PERCENT);
    }

    [Fact]
    public void NormalizeAccessibilityEnums_WhenEnumValuesInvalid_ShouldResetToDefaults()
    {
        var profile = new Profile
        {
            AccessibilityPreset = (AccessibilityPreset) 999,
            AccessibilityColorMode = (AccessibilityColorMode) 999
        };

        profile.NormalizeAccessibilityEnums();

        profile.AccessibilityPreset.Should().Be(AccessibilityPreset.Default);
        profile.AccessibilityColorMode.Should().Be(AccessibilityColorMode.Normal);
    }
}
