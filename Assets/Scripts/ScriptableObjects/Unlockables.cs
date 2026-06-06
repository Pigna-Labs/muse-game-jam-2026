using System.Collections.Generic;
using UnityEngine;

namespace MuseGameJam.Gameplay
{
        [CreateAssetMenu(fileName = "Unlockable Manager", menuName = "Scriptable Objects/Unlockable Manager")]
        public class Unlockables : ScriptableObject
        {
                [SerializeField] private List<ItemSO> foods = new();
                [SerializeField] private List<ItemSO> soaps = new();
                [SerializeField] private List<ItemSO> toys = new();
                [SerializeField] private List<CompanionSO> companions = new();
                [SerializeField] private List<InfoSO> infos = new();

                public IReadOnlyList<ItemSO> Foods => foods;
                public  IReadOnlyList<ItemSO> Soaps => soaps;
                public IReadOnlyList<ItemSO> Toys => toys;
                public IReadOnlyList<CompanionSO> Companions => companions;
                public IReadOnlyList<InfoSO> Infos => infos;

                // Returns the Info asset whose QrValue matches the scanned text, or null if none does.
                // The comparison is NORMALIZED (see NormalizeUrl): a QR printed as
                // "http://muse.it/.../funghi-tropicali" still matches an asset configured as
                // "https://www.muse.it/.../funghi-tropicali/". This avoids no-match failures caused
                // by trivial differences (scheme, www, trailing slash, case, spaces).
                public InfoSO FindInfoByQrValue(string qrValue)
                {
                        if (string.IsNullOrEmpty(qrValue)) return null;

                        string scanned = NormalizeUrl(qrValue);

                        foreach (InfoSO info in infos)
                        {
                                if (info != null && NormalizeUrl(info.QrValue) == scanned)
                                {
                                        return info;
                                }
                        }

                        return null;
                }

                // Canonical form of a URL for matching: lowercase, no scheme, no leading "www.",
                // no trailing slash, trimmed. So all these collapse to the same string:
                //   "https://www.muse.it/x/" , "http://muse.it/x" , "  MUSE.IT/X/ "  ->  "muse.it/x"
                public static string NormalizeUrl(string url)
                {
                        if (string.IsNullOrEmpty(url)) return string.Empty;

                        string s = url.Trim().ToLowerInvariant();

                        if (s.StartsWith("https://")) s = s.Substring(8);
                        else if (s.StartsWith("http://")) s = s.Substring(7);

                        if (s.StartsWith("www.")) s = s.Substring(4);

                        s = s.TrimEnd('/');

                        return s;
                }
        }
}
