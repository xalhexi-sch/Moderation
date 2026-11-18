using UnityEngine;

namespace Moderation.Models
{
    public class GlitchObject
    {
        public Vector3 Position { get; set; }
        public Transform RootTransform { get; set; }
        public string ObjectType { get; set; } // "barricade" or "structure"
        public ushort AssetId { get; set; }
        public ulong Owner { get; set; }

        public bool IsDestroyed => RootTransform == null;
    }
}
