using System.Collections.Generic;
using Moderation.Models;
using SDG.Unturned;
using UnityEngine;

namespace Moderation.Services
{
    public class GlitchService
    {
        private readonly List<GlitchObject> suspectedGlitches = new List<GlitchObject>();
        private readonly Dictionary<ulong, int> playerIndices = new Dictionary<ulong, int>();
        private readonly object syncLock = new object();

        public int Rescan()
        {
            lock (syncLock)
            {
                suspectedGlitches.Clear();
                playerIndices.Clear();

                // Scan Barricades
                if (BarricadeManager.regions != null)
                {
                    for (byte x = 0; x < Regions.WORLD_SIZE; x++)
                    {
                        for (byte y = 0; y < Regions.WORLD_SIZE; y++)
                        {
                            var region = BarricadeManager.regions[x, y];
                            if (region?.drops == null) continue;

                            foreach (var drop in region.drops)
                            {
                                if (drop == null) continue;
                                var serverside = drop.GetServersideData();
                                Vector3 pos = drop.model != null ? drop.model.position : (serverside != null ? serverside.point : Vector3.zero);
                                float groundY = LevelGround.getHeight(pos);

                                if (pos.y < groundY - 1.5f || pos.y < -5f)
                                {
                                    suspectedGlitches.Add(new GlitchObject
                                    {
                                        Position = pos,
                                        RootTransform = drop.model,
                                        ObjectType = "barricade",
                                        AssetId = drop.asset != null ? drop.asset.id : (ushort)0,
                                        Owner = serverside != null ? serverside.owner : 0
                                    });
                                }
                            }
                        }
                    }
                }

                // Scan Structures
                if (StructureManager.regions != null)
                {
                    for (byte x = 0; x < Regions.WORLD_SIZE; x++)
                    {
                        for (byte y = 0; y < Regions.WORLD_SIZE; y++)
                        {
                            var region = StructureManager.regions[x, y];
                            if (region?.drops == null) continue;

                            foreach (var drop in region.drops)
                            {
                                if (drop == null) continue;
                                var serverside = drop.GetServersideData();
                                Vector3 pos = drop.model != null ? drop.model.position : (serverside != null ? serverside.point : Vector3.zero);
                                float groundY = LevelGround.getHeight(pos);

                                if (pos.y < groundY - 1.5f || pos.y < -5f)
                                {
                                    suspectedGlitches.Add(new GlitchObject
                                    {
                                        Position = pos,
                                        RootTransform = drop.model,
                                        ObjectType = "structure",
                                        AssetId = drop.asset != null ? drop.asset.id : (ushort)0,
                                        Owner = serverside != null ? serverside.owner : 0
                                    });
                                }
                            }
                        }
                    }
                }

                return suspectedGlitches.Count;
            }
        }

        public int Count
        {
            get
            {
                lock (syncLock)
                {
                    return suspectedGlitches.Count;
                }
            }
        }

        public int GetNextIndex(ulong steamId)
        {
            lock (syncLock)
            {
                if (suspectedGlitches.Count == 0) return 0;
                if (!playerIndices.TryGetValue(steamId, out int idx))
                    idx = 0;

                idx++;
                if (idx > suspectedGlitches.Count)
                    idx = 1;

                playerIndices[steamId] = idx;
                return idx;
            }
        }

        public GlitchObject GetGlitch(int oneBasedIndex)
        {
            lock (syncLock)
            {
                if (oneBasedIndex < 1 || oneBasedIndex > suspectedGlitches.Count)
                    return null;

                return suspectedGlitches[oneBasedIndex - 1];
            }
        }
    }
}
