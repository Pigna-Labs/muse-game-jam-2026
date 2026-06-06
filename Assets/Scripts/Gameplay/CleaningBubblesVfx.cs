using UnityEngine;

namespace MuseGameJam.Gameplay
{
    // VFX di bolle di sapone per l'azione di pulizia.
    // Va messo sul child "Particle System" del Character. DropArea chiama Play()
    // quando inizi a pulire (spugna trascinata dentro l'area) e Stop() quando molli
    // o esci: le bolle ancora vive spariscono subito tutte insieme.
    //
    // Ogni bolla che nasce prende:
    //  - una rotazione iniziale casuale (0..360°);
    //  - uno sprite a caso tra i 4 della lista "Bubble Sprites".
    // Gli sprite vanno assegnati nell'Inspector: lo script configura da solo il modulo
    // Texture Sheet Animation, quindi non serve toccare il Material del renderer.
    [RequireComponent(typeof(ParticleSystem))]
    public class CleaningBubblesVfx : MonoBehaviour
    {
        [Header("Sprite bolle (scelto a caso per ogni particella)")]
        [SerializeField] Sprite[] bubbleSprites = new Sprite[4];

        ParticleSystem ps;

        void Awake()
        {
            ps = GetComponent<ParticleSystem>();
            ConfigureRandomRotation();
            ConfigureRandomSprite();
            ShrinkMaxSize();
            SlowDownSpeed();
            // A riposo non deve emettere nulla: parte solo quando si pulisce.
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // Rotazione iniziale casuale 0..360° (l'API vuole i radianti).
        void ConfigureRandomRotation()
        {
            var main = ps.main;
            main.startRotation3D = false;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        }

        // Texture Sheet Animation in modalità Sprites: ogni particella parte su un
        // frame casuale (= uno dei png) e ci resta per tutta la sua vita.
        void ConfigureRandomSprite()
        {
            var tsa = ps.textureSheetAnimation;
            tsa.enabled = true;
            tsa.mode = ParticleSystemAnimationMode.Sprites;

            for (int i = 0; i < bubbleSprites.Length; i++)
            {
                if (i < tsa.spriteCount) tsa.SetSprite(i, bubbleSprites[i]);
                else tsa.AddSprite(bubbleSprites[i]);
            }

            int count = bubbleSprites.Length;
            // Frame fermo (niente animazione) + frame di partenza casuale tra gli sprite.
            tsa.frameOverTime = new ParticleSystem.MinMaxCurve(0f);
            tsa.startFrame = new ParticleSystem.MinMaxCurve(0f, count);
        }

        // Riduce del 75% l'estremo massimo del range di Start Size (il minimo resta
        // invariato), così le bolle al massimo sono molto più piccole.
        void ShrinkMaxSize()
        {
            var main = ps.main;
            var size = main.startSize;
            size.constantMax *= 0.25f;
            main.startSize = size;
        }

        // Rallenta del 75% la velocità con cui le bolle si spostano: scala lo Start
        // Speed (min e max del range) al 25%.
        void SlowDownSpeed()
        {
            var main = ps.main;
            var speed = main.startSpeed;
            speed.constantMin *= 0.25f;
            speed.constantMax *= 0.25f;
            main.startSpeed = speed;
        }

        // Inizia a emettere bolle. Idempotente: se sta già emettendo non fa nulla.
        public void Play()
        {
            if (ps.isEmitting) return;
            ps.Play(true);
        }

        // Smette di emettere e cancella subito tutte le bolle ancora a schermo,
        // così spariscono tutte insieme quando molli il drag.
        public void Stop()
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
