namespace MuseGameJam.Visuals
{
    /// <summary>
    /// Definizione condivisa di "giorno" vs "notte", basata sull'ora reale del
    /// dispositivo. La usano sia DayNightImage (sprite) sia la musica (layer
    /// giorno/notte), così restano sempre coerenti tra loro.
    /// </summary>
    public static class DayNight
    {
        /// <summary>Ora di default dell'alba (incluso) — è giorno da quest'ora.</summary>
        public const int DefaultSunriseHour = 7;
        /// <summary>Ora di default del tramonto (escluso) — è notte da quest'ora.</summary>
        public const int DefaultSunsetHour = 19;

        /// <summary>True se l'ora locale è tra alba (incl.) e tramonto (escl.).</summary>
        public static bool IsDaytime(int sunriseHour, int sunsetHour)
        {
            int hour = System.DateTime.Now.Hour;
            return hour >= sunriseHour && hour < sunsetHour;
        }

        /// <summary>Comodo: usa le ore di default.</summary>
        public static bool IsDaytime() => IsDaytime(DefaultSunriseHour, DefaultSunsetHour);
    }
}
