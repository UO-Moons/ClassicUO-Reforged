namespace ClassicUO.Network
{
    internal static class AssistantFeatureRestrictionState
    {
        public static bool DisableWeatherFilter { get; set; }
        public static bool DisableLightFilter { get; set; }
        public static bool DisableSeasonFilter { get; set; }
        public static bool DisableAutoOpenDoors { get; set; }
        public static bool DisableOverheadHealth { get; set; }

        public static void Reset()
        {
            DisableWeatherFilter = false;
            DisableLightFilter = false;
            DisableSeasonFilter = false;
            DisableAutoOpenDoors = false;
            DisableOverheadHealth = false;
        }
    }
}
