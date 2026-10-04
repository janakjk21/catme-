#if UNITY_EDITOR
using System.IO;
using System.Linq;
using CatMe.Session;
using UnityEditor;
using UnityEngine;

namespace CatMe.Editor
{
    public static class HouseRoomPersonalization
    {
        const string RoomPath = "Assets/CatMe/Resources/Rooms/HouseRoom_r3.prefab";
        const string ArtPath = "Assets/CatMe/Art/Rooms/";

        [MenuItem("CatMe/Rooms/Refurnish House Room")]
        public static void Install()
        {
            AssetDatabase.Refresh();
            var root = PrefabUtility.LoadPrefabContents(RoomPath);
            try
            {
                Add(root.transform, root.transform.Find("HomeKitShell").position);
                PrefabUtility.SaveAsPrefabAsset(root, RoomPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
        }

        // Shared with the room builder, so rebuilding the existing room preserves the furnishings.
        public static void Add(Transform root, Vector3 centre)
        {
            foreach (string name in new[] { "Cat bed frame", "Cat bed cushion", "Food bowl", "Cat house", "Dog bowl", "House photo frame", "Room life", "Wall frames", "Window view" })
            {
                var old = root.Find(name); if (old != null) Object.DestroyImmediate(old.gameObject);
            }
            var shell = root.Find("HomeKitShell");
            Bounds room = shell != null ? Bounds(shell.gameObject) : new Bounds(centre + Vector3.up * 1.3f, new Vector3(7f, 2.6f, 12f));
            // The cat house is sized by its footprint so the 0.65 m pet fits through the opening, and it is walk-in (not solid):
            // Bed time sends the pet inside to sleep. Sizing by height made a tall 1.15 m-wide tower the pet could not enter,
            // and a solid blocker around it trapped the pet when the bed spot fell inside it.
            // Yaw 270 turns the arched entrance (on one face of the model only) to face into the room, toward centre.
            Prop(root, "cat_house.glb", "Cat house", centre + new Vector3(1.9f, 0f, -.3f), 270f, HouseWidth, false);
            var house = root.Find("Cat house");
            if (house != null && Bounds(house.gameObject).size.y > room.size.y - .35f)
            {   // never through the ceiling
                float k = (room.size.y - .35f) / Bounds(house.gameObject).size.y; house.localScale *= k;
                var hb = Bounds(house.gameObject); house.position += new Vector3(centre.x + 1.9f - hb.center.x, centre.y - hb.min.y, centre.z - .3f - hb.center.z);
            }
            Bed(root, centre, house);
            Vector3 bowlPosition = centre + new Vector3(-1.5f, 0f, .6f);   // opposite side from the cat house and bed
            Prop(root, "dog_bowl.glb", "Dog bowl", bowlPosition, 0f, .8f, false);
            var bowlAnchor = root.Find("Cat_Bowl"); if (bowlAnchor != null) bowlAnchor.position = bowlPosition;
            var frame = new GameObject("House photo frame"); frame.transform.SetParent(root, false);
            frame.transform.position = centre + new Vector3(.5f, 1.75f, -5.55f);
            var wood = Material("Frame wood", new Color(.27f, .16f, .09f), false);
            var paper = Material("Frame paper", new Color(.94f, .90f, .83f), true);
            Piece(frame.transform, "Frame backing", Vector3.zero, new Vector3(1.3f, .95f, .06f), wood);
            Piece(frame.transform, "Photo mat", new Vector3(0, 0, .04f), new Vector3(1.18f, .83f, .015f), paper);
            var surface = GameObject.CreatePrimitive(PrimitiveType.Quad); surface.name = "Photograph"; surface.transform.SetParent(frame.transform, false);
            surface.transform.localPosition = new Vector3(0, 0, .055f); surface.transform.localRotation = Quaternion.Euler(0, 180, 0);
            surface.transform.localScale = new Vector3(1.08f, .72f, 1);
            Object.DestroyImmediate(surface.GetComponent<Collider>()); surface.GetComponent<Renderer>().sharedMaterial = paper;
            var placeholder = new GameObject("Add a photo here"); placeholder.transform.SetParent(frame.transform, false);
            placeholder.transform.localPosition = new Vector3(0, 0, .065f); placeholder.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var text = placeholder.AddComponent<TextMesh>(); text.text = "Add a photo here"; text.fontSize = 64; text.characterSize = .025f;
            text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center; text.color = new Color(.27f, .23f, .19f);
            var point = frame.AddComponent<InteractionPoint>(); point.kind = InteractionKind.HousePhoto; point.label = "Add a photo"; point.radius = 2.6f;
            frame.AddComponent<BoxCollider>().size = new Vector3(1.3f, .95f, .12f);
            var component = frame.AddComponent<HousePhotoFrame>(); component.photoSurface = surface.GetComponent<Renderer>(); component.placeholder = placeholder;
            StandPlantsUpright(root);
            RoomLife(root, centre, room);
            WallFrames(root, centre, room);
            WindowView(root, centre);
        }

        /// <summary>F8: the pet's bed and house are shown this much bigger than before.</summary>
        public const float BedScale = 1.6f;
        /// <summary>Cat house footprint width in metres; wide enough that the pet (CompanionScaleStandards.CatHeightMeters) fits inside.</summary>
        public const float HouseWidth = 1.7f;

        // The bed is a cushion inside the cat house; the Cat_Bed anchor the sleep routine walks to sits at the house's centre on
        // the floor, facing out into the room, so the pet walks in and curls up there.
        static void Bed(Transform root, Vector3 centre, Transform house)
        {
            var anchor = root.Find("Cat_Bed");
            if (anchor == null) { anchor = new GameObject("Cat_Bed").transform; anchor.SetParent(root, false); }
            Vector3 at; float inner;
            if (house != null)
            {
                // Centre on the floor box only: the posts and the top platform sit off to one side and pull the whole model's
                // centre away from the box the pet sleeps in.
                var all = Bounds(house.gameObject); Bounds hb = default; bool found = false;
                foreach (var r in house.GetComponentsInChildren<Renderer>(true))
                    if (r.bounds.min.y < all.min.y + .05f) { if (!found) { hb = r.bounds; found = true; } else hb.Encapsulate(r.bounds); }
                if (!found) hb = all;
                at = new Vector3(hb.center.x, centre.y, hb.center.z); inner = Mathf.Min(hb.size.x, hb.size.z) * .6f;
            }
            else { at = centre + new Vector3(1.9f, 0f, -.3f); inner = .9f; }
            Vector3 outward = centre - at; outward.y = 0f;
            float yaw = outward.sqrMagnitude > .01f ? Quaternion.LookRotation(outward).eulerAngles.y : 180f;
            anchor.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            var cushionMat = Material("Cat bed cushion", new Color(.82f, .62f, .5f), false);
            var cushion = GameObject.CreatePrimitive(PrimitiveType.Cylinder); cushion.name = "Cat bed cushion"; cushion.transform.SetParent(root, false);
            cushion.transform.SetPositionAndRotation(at + Vector3.up * .05f, Quaternion.Euler(0, yaw, 0)); cushion.transform.localScale = new Vector3(inner, .05f, inner);
            cushion.GetComponent<Renderer>().sharedMaterial = cushionMat; Object.DestroyImmediate(cushion.GetComponent<Collider>());
        }

        // F8: imported plants (monstera, bonsai and others) came in lying on their side. Try the axis turns and keep the one that
        // stands tallest, and set it back on the floor (or table) where it stood.
        public static void StandPlantsUpright(Transform root)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLowerInvariant();
                if (t.parent != root || !(n.Contains("monstera") || n.Contains("bonsai") || n.Contains("plant"))) continue;
                if (t.GetComponentsInChildren<Renderer>(true).Length == 0) continue;
                Bounds before = Bounds(t.gameObject);
                float yaw = t.eulerAngles.y; Quaternion best = t.rotation; float bestHeight = before.size.y;
                foreach (var turn in new[] { Quaternion.identity, Quaternion.Euler(-90f, 0f, 0f), Quaternion.Euler(90f, 0f, 0f), Quaternion.Euler(0f, 0f, 90f), Quaternion.Euler(0f, 0f, -90f) })
                {
                    t.rotation = Quaternion.Euler(0f, yaw, 0f) * turn;
                    float h = Bounds(t.gameObject).size.y;
                    if (h > bestHeight * 1.15f) { bestHeight = h; best = t.rotation; }
                }
                t.rotation = best;
                // Pot at the bottom: the leaves spread wider than the pot, so if the bottom quarter is wider than the top, flip it.
                if (Spread(t, .0f, .25f) > Spread(t, .75f, 1f) * 1.1f) t.rotation = Quaternion.AngleAxis(180f, t.right) * t.rotation;
                Bounds after;   // a turn keeps the plant's real size; only its footing moves
                after = Bounds(t.gameObject);
                t.position += new Vector3(before.center.x - after.center.x, before.min.y - after.min.y, before.center.z - after.center.z);
            }
        }

        // How wide (x/z extent) the model's vertices are between two heights, as fractions of its height.
        static float Spread(Transform t, float from, float to)
        {
            Bounds all = Bounds(t.gameObject); float y0 = all.min.y + all.size.y * from, y1 = all.min.y + all.size.y * to;
            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (var mf in t.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                var vertices = mf.sharedMesh.vertices; int step = Mathf.Max(1, vertices.Length / 4000);
                for (int i = 0; i < vertices.Length; i += step)
                {
                    Vector3 w = mf.transform.TransformPoint(vertices[i]);
                    if (w.y < y0 || w.y > y1) continue;
                    minX = Mathf.Min(minX, w.x); maxX = Mathf.Max(maxX, w.x); minZ = Mathf.Min(minZ, w.z); maxZ = Mathf.Max(maxZ, w.z);
                }
            }
            return maxX < minX ? 0f : Mathf.Max(maxX - minX, maxZ - minZ);
        }

        // F8: a livelier room: warm walls, a reading lamp with a warm light, a shelf with small things, cushions on the couch.
        static void RoomLife(Transform root, Vector3 centre, Bounds room)
        {
            var warm = Material("Warm plaster", new Color(.91f, .84f, .74f), false);
            var shell = root.Find("HomeKitShell");
            if (shell != null) foreach (var r in shell.GetComponentsInChildren<Renderer>(true)) if (r.name.ToLowerInvariant().Contains("wall")) r.sharedMaterial = warm;
            var life = new GameObject("Room life").transform; life.SetParent(root, false);
            const string LR = "Assets/LowPolyLivingRoomPack/Prefabs/";
            float back = room.min.z + .45f, left = room.min.x + .45f, right = room.max.x - .45f;
            Kit(life, LR + "Lamp_Tall.prefab", new Vector3(left + .1f, centre.y, back + .2f), 0f, 1.55f);
            var lampLight = new GameObject("Reading lamp light"); lampLight.transform.SetParent(life, false); lampLight.transform.position = new Vector3(left + .2f, centre.y + 1.45f, back + .35f);
            var l = lampLight.AddComponent<Light>(); l.type = LightType.Point; l.range = 4.5f; l.intensity = .9f; l.color = new Color(1f, .74f, .45f); l.shadows = LightShadows.None;
            Kit(life, LR + "Bookshelf_Tall.prefab", new Vector3(right - .05f, centre.y, centre.z - 1.6f), 270f, 1.9f);
            Kit(life, LR + "Night_stand_1.prefab", new Vector3(right - .1f, centre.y, centre.z + .6f), 270f, .55f);
            Kit(life, LR + "DeskClock_1.prefab", new Vector3(right - .1f, centre.y + .56f, centre.z + .5f), 250f, .16f);
            Kit(life, LR + "Mug.prefab", new Vector3(right - .15f, centre.y + .56f, centre.z + .75f), 0f, .1f);
            Kit(life, LR + "Book_3.prefab", new Vector3(right - .1f, centre.y + .56f, centre.z + .9f), 80f, .05f);
            // Cushions on the kit couch (P(-0.9, -4.5) in the room builder).
            var cloth = new[] { Material("Cushion sage", new Color(.53f, .58f, .53f), false), Material("Cushion terracotta", new Color(.74f, .45f, .33f), false) };
            for (int i = 0; i < 2; i++)
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cube); c.name = "Cushion"; c.transform.SetParent(life, false);
                c.transform.position = centre + new Vector3(-1.55f + i * 1.3f, .62f, -4.45f); c.transform.rotation = Quaternion.Euler(-12f, i == 0 ? 15f : -12f, 0f);
                c.transform.localScale = new Vector3(.42f, .38f, .14f); c.GetComponent<Renderer>().sharedMaterial = cloth[i]; Object.DestroyImmediate(c.GetComponent<Collider>());
            }
        }

        // F8: 3 to 5 pictures at eye height on the side walls, clear of the main photo frame on the back wall. Uses the owner's
        // frames from Art/Rooms/Frames when there are any; otherwise simple wooden frames with soft neutral art.
        static void WallFrames(Transform root, Vector3 centre, Bounds room)
        {
            var group = new GameObject("Wall frames").transform; group.SetParent(root, false);
            var owned = Directory.Exists(ArtPath + "Frames") ? Directory.GetFiles(ArtPath + "Frames").Where(f => f.EndsWith(".prefab") || f.EndsWith(".glb") || f.EndsWith(".fbx")).OrderBy(f => f).ToArray() : new string[0];
            float eye = centre.y + 1.6f;
            // A gallery on each side wall: mixed sizes in two rows, clear of the lamp (left, back corner),
            // the bookshelf (right, z - 1.6) and the night stand (right, z + .6, low).
            float hi = eye + .32f, lo = eye - .32f;
            // The same gallery rhythm on both side walls: a large frame, a stacked pair of small ones, a wide one, another pair,
            // a large one. The right wall skips the bookshelf's stretch (z - 2.3 .. - .9) and keeps clear of its top.
            var pattern = new (float dz, int row, Vector2 size)[]
            {
                (-2.4f, 0, new Vector2(.62f, .78f)), (-1.3f, 1, new Vector2(.42f, .52f)), (-1.3f, -1, new Vector2(.42f, .42f)),
                (-.1f, 0, new Vector2(.80f, .60f)), (1.1f, 1, new Vector2(.48f, .60f)), (1.1f, -1, new Vector2(.48f, .38f)),
                (2.3f, 0, new Vector2(.62f, .78f)), (3.4f, 1, new Vector2(.44f, .52f)), (3.4f, -1, new Vector2(.44f, .42f)),
                (4.5f, 0, new Vector2(.70f, .55f)),
            };
            var list = new System.Collections.Generic.List<(Vector3 pos, float yaw, Vector2 size)>();
            foreach (var side in new[] { -1, 1 })
                foreach (var (dz, row, size) in pattern)
                {
                    float z = centre.z + dz;
                    if (z + size.x * .5f > room.max.z - .3f || z - size.x * .5f < room.min.z + .3f) continue;
                    if (side == 1 && z > centre.z - 2.3f && z < centre.z - .9f) continue;   // bookshelf
                    // Hang on the real inner wall: the shell's bounds include the roof overhang outside, so a frame placed at
                    // room.min/max ended up behind the wall. Cast from the room's centre to the wall and use the hit surface.
                    float y = row > 0 ? hi : row < 0 ? lo : eye;
                    if (!WallHit(root, new Vector3(centre.x, y, z), new Vector3(side, 0f, 0f), out var hit)) continue;
                    list.Add((hit.point + hit.normal * .035f, Quaternion.LookRotation(hit.normal).eulerAngles.y, size));
                }
            var spots = list.ToArray();
            var art = new[] { new Color(.62f, .72f, .66f), new Color(.86f, .74f, .55f), new Color(.70f, .63f, .74f), new Color(.80f, .60f, .52f), new Color(.55f, .66f, .74f), new Color(.88f, .82f, .70f) };
            var wood = Material("Frame wood", new Color(.27f, .16f, .09f), false);
            for (int i = 0; i < spots.Length; i++)
            {
                var (pos, yaw, size) = spots[i];
                if (i < owned.Length)
                {
                    var src = AssetDatabase.LoadAssetAtPath<GameObject>(owned[i].Replace('\\', '/'));
                    if (src != null)
                    {
                        var go = Object.Instantiate(src, group); go.name = "Wall frame " + (i + 1);
                        foreach (var col in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
                        go.transform.rotation = Quaternion.Euler(0, yaw, 0);
                        var b = Bounds(go); float s = size.y / Mathf.Max(b.size.y, .01f); go.transform.localScale *= s;
                        b = Bounds(go); go.transform.position += pos - b.center;
                        continue;
                    }
                }
                var frame = new GameObject("Wall frame " + (i + 1)).transform; frame.SetParent(group, false); frame.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
                Piece(frame, "Frame", Vector3.zero, new Vector3(size.x, size.y, .04f), wood);
                Piece(frame, "Art", new Vector3(0, 0, .025f), new Vector3(size.x - .12f, size.y - .12f, .01f), Material("Wall art " + (i % art.Length + 1), art[i % art.Length], true));
            }
        }

        // What the windows look out on: open water to the horizon under the day sky (the room sits away from the garden,
        // so without this the windows show the void). The room's own floor and walls cover the water inside.
        static void WindowView(Transform root, Vector3 centre)
        {
            var view = new GameObject("Window view").transform; view.SetParent(root, false);
            var water = GameObject.CreatePrimitive(PrimitiveType.Plane); water.name = "Window water"; water.transform.SetParent(view, false);
            water.transform.position = centre + Vector3.down * .35f; water.transform.localScale = new Vector3(16f, 1f, 16f);
            Object.DestroyImmediate(water.GetComponent<Collider>());
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/CatMe/Art/Places/Materials/River CatMe.mat");
            water.GetComponent<Renderer>().sharedMaterial = mat != null ? mat : Material("Window water", new Color(.17f, .42f, .50f), false);
            water.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static bool WallHit(Transform root, Vector3 origin, Vector3 direction, out RaycastHit best)
        {
            best = default; float nearest = float.MaxValue; bool found = false; Physics.SyncTransforms();
            foreach (var col in root.GetComponentsInChildren<Collider>(true))
            {
                if (!col.name.ToLowerInvariant().Contains("wall")) continue;
                if (col.Raycast(new Ray(origin, direction), out var h, 15f) && h.distance < nearest) { nearest = h.distance; best = h; found = true; }
            }
            return found;
        }

        // A LowPolyLivingRoomPack prop at a height, grounded where it stands; nothing solid (the room keeps its own blockers).
        static void Kit(Transform parent, string path, Vector3 position, float yaw, float height)
        {
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (src == null) { Debug.LogWarning("[CatMe][Rooms] missing " + path); return; }
            var go = Object.Instantiate(src, parent); go.name = Path.GetFileNameWithoutExtension(path);
            foreach (var col in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
            go.transform.rotation = Quaternion.Euler(0, yaw, 0);
            var b = Bounds(go); if (b.size.y > .001f) go.transform.localScale *= height / b.size.y;
            b = Bounds(go); go.transform.position += new Vector3(position.x - b.center.x, position.y - b.min.y, position.z - b.center.z);
        }

        static void Prop(Transform root, string file, string name, Vector3 position, float yaw, float width, bool solid, bool sizeByHeight = false)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(ArtPath + file);
            if (source == null) throw new FileNotFoundException("Room model has not imported: " + file);
            var go = Object.Instantiate(source, root); go.name = name; go.transform.rotation = Quaternion.Euler(-90, yaw, 0); // supplied props use Z as their vertical axis
            foreach (var collider in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
            Bounds b = Bounds(go); go.transform.localScale *= width / (sizeByHeight ? b.size.y : Mathf.Max(b.size.x, b.size.z));
            b = Bounds(go); go.transform.position += new Vector3(position.x - b.center.x, position.y - b.min.y, position.z - b.center.z);
            if (solid)
            {
                b = Bounds(go); var blocker = new GameObject("Cat house blocker"); blocker.transform.SetParent(go.transform, true); blocker.transform.position = b.center;
                var col = blocker.AddComponent<BoxCollider>(); col.size = new Vector3(b.size.x * .8f, b.size.y, b.size.z * .8f); blocker.transform.localScale = new Vector3(1f / go.transform.lossyScale.x, 1f / go.transform.lossyScale.y, 1f / go.transform.lossyScale.z);
            }
        }
        static Bounds Bounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true); var b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }
        static Material Material(string name, Color color, bool unlit)
        {
            string path = ArtPath + name.Replace(" ", "") + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            mat.color = color; return mat;
        }
        static void Piece(Transform parent, string name, Vector3 pos, Vector3 size, Material material)
        {
            var piece = GameObject.CreatePrimitive(PrimitiveType.Cube); piece.name = name; piece.transform.SetParent(parent, false);
            piece.transform.localPosition = pos; piece.transform.localScale = size; piece.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(piece.GetComponent<Collider>());
        }
    }
}
#endif
