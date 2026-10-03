using UnityEngine;

namespace CatMe.Garden
{
    /// <summary>Single source of ground truth for owner, cat and ball. Uses the Walkable layer when a place defines it.</summary>
    public static class GroundProbe
    {
        public const string WalkableLayerName = "Walkable";
        // Used by the legacy single-route garden, which has one named ground collider.
        public static Collider LegacyGround;

        /// <summary>Layers that block capsule movement: everything except walkable ground, so slopes never act as walls.</summary>
        public static int ObstacleMask
        {
            get { int layer = LayerMask.NameToLayer(WalkableLayerName); return layer >= 0 ? ~(1 << layer) : ~0; }
        }

        public static float Height(Vector3 position, float fallback = 0f)
        {
            int layer = LayerMask.NameToLayer(WalkableLayerName);
            if (layer >= 0)
            {
                if (Physics.Raycast(new Vector3(position.x, position.y + 2f, position.z), Vector3.down, out RaycastHit walkable, 60f, 1 << layer, QueryTriggerInteraction.Ignore))
                    return walkable.point.y;
                // Fall through for places that have not tagged their ground yet.
            }
            if (LegacyGround != null && LegacyGround.Raycast(new Ray(new Vector3(position.x, 200f, position.z), Vector3.down), out RaycastHit legacy, 400f))
                return legacy.point.y;
            if (Physics.Raycast(new Vector3(position.x, position.y + 4f, position.z), Vector3.down, out RaycastHit any, 80f, ~0, QueryTriggerInteraction.Ignore))
                return any.point.y;
            return fallback;
        }

        public static Vector3 Snap(Vector3 position)
        {
            position.y = Height(position, position.y);
            return position;
        }
    }
}
