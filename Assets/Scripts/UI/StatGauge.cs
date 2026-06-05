using UnityEngine;
using UnityEngine.UIElements;

namespace MuseGameJam.UI
{
    // Indicatore circolare (outline bianco) che si riempie dal basso in base a un valore 0..1
    // (0 = vuoto, 1 = pieno). Il colore del riempimento cambia a soglie:
    //   v >= 0.7 verde | 0.5–0.7 giallo | 0.3–0.5 arancione | < 0.3 rosso
    // Usalo in UXML come <MuseGameJam.UI.StatGauge value="1"> con il Button dentro (ci sta sopra).
    [UxmlElement]
    public partial class StatGauge : VisualElement
    {
        static readonly Color Green = new Color(0.30f, 0.78f, 0.33f);
        static readonly Color Yellow = new Color(0.95f, 0.85f, 0.20f);
        static readonly Color Orange = new Color(0.95f, 0.55f, 0.15f);
        static readonly Color Red = new Color(0.86f, 0.25f, 0.22f);

        readonly VisualElement fill;
        float current = 1f;

        // Valore 0..1 mostrato dal gauge. Impostalo da codice o da UXML (attributo "value").
        [UxmlAttribute]
        public float value
        {
            get => current;
            set
            {
                current = Mathf.Clamp01(value);
                Refresh();
            }
        }

        public StatGauge()
        {
            AddToClassList("stat-gauge");

            // Il riempimento sta dietro al contenuto (es. il Button), in absolute ancorato al fondo.
            fill = new VisualElement();
            fill.AddToClassList("stat-gauge__fill");
            fill.pickingMode = PickingMode.Ignore;
            Add(fill);

            Refresh();
        }

        void Refresh()
        {
            fill.style.height = Length.Percent(current * 100f);
            fill.style.backgroundColor = ColorFor(current);
        }

        static Color ColorFor(float v)
        {
            if (v >= 0.7f) return Green;
            if (v >= 0.5f) return Yellow;
            if (v >= 0.3f) return Orange;
            return Red;
        }
    }
}
