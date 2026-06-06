using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace MuseGameJam.Gameplay
{
    /// <summary>
    /// Spawns companion (unlockable) visuals into the scene.
    ///
    /// Holds a reference to the shared <see cref="Unlockables"/> asset (the list of all
    /// unlockables) and instantiates a companion prefab at a placeholder slot, applying the
    /// selected <see cref="CompanionSO"/>'s sprite.
    ///
    /// Each companion is bound to a fixed placeholder: its index in the Unlockables asset's
    /// companion list selects the matching slot in <see cref="placeholders"/>, so a companion
    /// always lands in the same place. Scene singleton so the Unlockables menu can reach it
    /// to toggle companions in and out from a tile tap.
    /// </summary>
    public class CompanionManager : MonoBehaviour
    {
        [Tooltip("Shared asset that holds the lists of all unlockables (companions, foods, soaps, toys).")]
        [SerializeField] private Unlockables unlockables;

        [Tooltip("Prefab instantiated for each spawned companion. Needs a SpriteRenderer to show the companion image.")]
        [SerializeField] private GameObject companionPrefab;

        [Tooltip("Uniform world scale applied to each spawned companion. The companion art is large " +
                 "(2048px @ 100 PPU = ~20 world units), so a small value keeps it sized to its slot.")]
        [SerializeField] private float companionScale = 0.1f;

        [Tooltip("Placeholder slots, one per companion. A companion is placed at the slot whose index " +
                 "matches its position in the Unlockables companion list. Falls back to this transform " +
                 "if no matching slot is set.")]
        [FormerlySerializedAs("spawnAnchors")]
        [SerializeField] private List<Transform> placeholders = new();

        // Tracks spawned instances so the same companion is not duplicated, and so it can be removed.
        private readonly Dictionary<CompanionSO, GameObject> spawned = new();

        public static CompanionManager Instance { get; private set; }

        public Unlockables Unlockables => unlockables;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Toggles the given companion in the scene: places it at its fixed slot if it is not
        /// currently shown, or removes it if it is. Returns true if the companion is now placed,
        /// false if it was removed (or could not be placed).
        /// </summary>
        public bool ToggleUnlockable(CompanionSO companion)
        {
            if (IsPlaced(companion))
            {
                RemoveUnlockable(companion);
                return false;
            }

            return SpawnUnlockable(companion) != null;
        }

        /// <summary>True if the given companion currently has an instance in the scene.</summary>
        public bool IsPlaced(CompanionSO companion)
        {
            return companion != null
                && spawned.TryGetValue(companion, out GameObject existing)
                && existing != null;
        }

        /// <summary>
        /// Spawns the given companion's prefab at its statically assigned placeholder slot, applying
        /// the companion's sprite, and returns its instance. If it is already spawned, returns the
        /// existing instance instead of duplicating it.
        /// </summary>
        public GameObject SpawnUnlockable(CompanionSO companion)
        {
            if (companion == null)
            {
                Debug.LogWarning("[CompanionManager] SpawnUnlockable called with a null companion.", this);
                return null;
            }

            if (companionPrefab == null)
            {
                Debug.LogError("[CompanionManager] No companion prefab assigned.", this);
                return null;
            }

            if (spawned.TryGetValue(companion, out GameObject existing) && existing != null)
            {
                return existing;
            }

            Transform anchor = ResolveSlot(companion);
            GameObject instance = Instantiate(companionPrefab, anchor.position, anchor.rotation, anchor);
            instance.name = $"Companion_{companion.DisplayName}";
            instance.transform.localScale = Vector3.one * companionScale;

            ApplySprite(instance, companion.Image);

            spawned[companion] = instance;
            return instance;
        }

        /// <summary>Removes the given companion's instance from the scene, if it is placed.</summary>
        public void RemoveUnlockable(CompanionSO companion)
        {
            if (companion != null && spawned.TryGetValue(companion, out GameObject existing))
            {
                if (existing != null)
                {
                    Destroy(existing);
                }
                spawned.Remove(companion);
            }
        }

        // Resolves the placeholder a companion belongs to: the slot whose index matches the
        // companion's position in the Unlockables list. Falls back to this transform when the
        // companion is not found or no matching slot is assigned.
        private Transform ResolveSlot(CompanionSO companion)
        {
            int index = IndexOf(companion);
            if (index >= 0 && index < placeholders.Count && placeholders[index] != null)
            {
                return placeholders[index];
            }

            return transform;
        }

        // Position of the companion in the Unlockables companion list, or -1 if not present.
        private int IndexOf(CompanionSO companion)
        {
            if (unlockables == null)
            {
                return -1;
            }

            IReadOnlyList<CompanionSO> companions = unlockables.Companions;
            for (int i = 0; i < companions.Count; i++)
            {
                if (companions[i] == companion)
                {
                    return i;
                }
            }

            return -1;
        }

        // Copies the companion's sprite onto the instance's SpriteRenderer, if any.
        private static void ApplySprite(GameObject instance, Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            SpriteRenderer renderer = instance.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.sprite = sprite;
            }
        }
    }
}
