using System.Collections.Generic;
using UnityEngine;

namespace MuseGameJam.Gameplay
{
    /// <summary>
    /// Spawns companion (unlockable) visuals into the scene.
    ///
    /// Holds a reference to the shared <see cref="Unlockables"/> asset (the list of all
    /// unlockables) and instantiates a companion prefab at a spawn point, applying the
    /// selected <see cref="CompanionSO"/>'s sprite.
    /// </summary>
    public class CompanionManager : MonoBehaviour
    {
        [Tooltip("Shared asset that holds the lists of all unlockables (companions, foods, soaps, toys).")]
        [SerializeField] private Unlockables unlockables;

        [Tooltip("Prefab instantiated for each spawned companion. Needs a SpriteRenderer to show the companion image.")]
        [SerializeField] private GameObject companionPrefab;

        [Tooltip("Anchors where unlockables can spawn. One is picked at random per spawn. " +
                 "Falls back to this transform if the list is empty.")]
        [SerializeField] private List<Transform> spawnAnchors = new();

        // Tracks spawned instances so the same companion is not duplicated.
        private readonly Dictionary<CompanionSO, GameObject> spawned = new();

        public Unlockables Unlockables => unlockables;

        /// <summary>
        /// Spawns the given unlockable's companion prefab on a random spawn anchor, applying the
        /// unlockable's sprite, and returns its instance. If it is already spawned, returns the
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

            Transform anchor = PickRandomAnchor();
            GameObject instance = Instantiate(companionPrefab, anchor.position, anchor.rotation, anchor);
            instance.name = $"Companion_{companion.DisplayName}";

            ApplySprite(instance, companion.Image);

            spawned[companion] = instance;
            return instance;
        }

        // Picks a random non-null anchor from the list; falls back to this transform if none are set.
        private Transform PickRandomAnchor()
        {
            int validCount = 0;
            foreach (Transform anchor in spawnAnchors)
            {
                if (anchor != null) validCount++;
            }

            if (validCount == 0)
            {
                return transform;
            }

            int pick = Random.Range(0, validCount);
            foreach (Transform anchor in spawnAnchors)
            {
                if (anchor == null) continue;
                if (pick == 0) return anchor;
                pick--;
            }

            return transform; // unreachable: validCount > 0 guarantees a hit above
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
