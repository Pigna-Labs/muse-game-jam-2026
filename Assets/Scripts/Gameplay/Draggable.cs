using UnityEngine;

namespace MuseGameJam.Gameplay
{
    // Oggetto la cui interazione È il trascinamento stesso (es. spugna per CLEAN, mano per PET):
    // conta quanto/dove si muove sopra una DropArea. La reazione lato area sta nella DropArea
    // (OnTriggerStay2D), qui si mette ciò che riguarda l'oggetto durante il movimento.
    public class Draggable : Item
    {
        protected override void OnDragMove(Vector3 worldPosition)
        {
            // es. scia/particelle mentre strofini. La logica dell'area sta nella DropArea.
        }
    }
}
