using UnityEngine;
using UnityEngine.UI;

namespace MuseGameJam.Visuals
{
    // Mostra sull'Image dello stesso GameObject la versione "giorno" o "notte" dello sprite
    // a seconda dell'ora reale del dispositivo: di giorno (tra alba e tramonto) usa daySprite,
    // altrimenti nightSprite. Ricontrolla periodicamente così la transizione avviene anche a gioco aperto.
    [RequireComponent(typeof(Image))]
    public class DayNightImage : MonoBehaviour
    {
        [Header("Sprite")]
        [SerializeField] Sprite daySprite;
        [SerializeField] Sprite nightSprite;

        [Header("Ore di sole (24h)")]
        // È giorno quando l'ora locale è >= alba e < tramonto.
        [SerializeField, Range(0, 23)] int sunriseHour = 7;
        [SerializeField, Range(1, 24)] int sunsetHour = 19;

        // Ogni quanti secondi ricontrollare l'orario (0 = solo all'avvio/abilitazione).
        [SerializeField] float refreshIntervalSeconds = 60f;

        Image image;
        float timer;

        void Awake()
        {
            image = GetComponent<Image>();
        }

        void OnEnable()
        {
            Apply();
            timer = 0f;
        }

        void Update()
        {
            if (refreshIntervalSeconds <= 0f) return;

            timer += Time.unscaledDeltaTime;
            if (timer < refreshIntervalSeconds) return;

            timer = 0f;
            Apply();
        }

        void Apply()
        {
            var sprite = IsDaytime() ? daySprite : nightSprite;
            if (sprite != null) image.sprite = sprite;
        }

        bool IsDaytime()
        {
            int hour = System.DateTime.Now.Hour;
            return hour >= sunriseHour && hour < sunsetHour;
        }
    }
}
