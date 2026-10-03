#if UNITY_EDITOR
using System.IO;
using CatMe.Session;
using UnityEditor;
using UnityEngine;

namespace CatMe.Editor
{
    public static class HouseRoomPersonalization
    {
        const string RoomPath = "Assets/CatMe/Resources/Rooms/HouseRoom_r3.prefab";
        const string ArtPath = "Assets/CatMe/Art/Rooms/";

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
            foreach (string name in new[] { "Cat bed frame", "Cat bed cushion", "Food bowl", "Cat house", "Dog bowl", "House photo frame" })
            {
                var old = root.Find(name); if (old != null) Object.DestroyImmediate(old.gameObject);
            }
            Prop(root, "cat_house.glb", "Cat house", centre + new Vector3(1.8f, 0f, -.2f), 180f, 1.1f, true, true);
            Vector3 bowlPosition = centre + new Vector3(1f, 0f, .3f);
            Prop(root, "dog_bowl.glb", "Dog bowl", bowlPosition, 0f, .55f, false);
            var bowlAnchor = root.Find("Cat_Bowl"); if (bowlAnchor != null) bowlAnchor.position = bowlPosition;
            var frame = new GameObject("House photo frame"); frame.transform.SetParent(root, false);
            frame.transform.position = centre + new Vector3(.5f, 1.75f, -5.55f);
            var wood = Material("Frame wood", new Color(.27f, .16f, .09f), false);
            var paper = Material("Frame paper", new Color(.94f, .90f, .83f), true);
            Piece(frame.transform, "Frame backing", Vector3.zero, new Vector3(2.5f, 1.75f, .08f), wood);
            Piece(frame.transform, "Photo mat", new Vector3(0, 0, .05f), new Vector3(2.3f, 1.55f, .015f), paper);
            var surface = GameObject.CreatePrimitive(PrimitiveType.Quad); surface.name = "Photograph"; surface.transform.SetParent(frame.transform, false);
            surface.transform.localPosition = new Vector3(0, 0, .065f); surface.transform.localRotation = Quaternion.Euler(0, 180, 0);
            surface.transform.localScale = new Vector3(2.16f, 1.41f, 1);
            Object.DestroyImmediate(surface.GetComponent<Collider>()); surface.GetComponent<Renderer>().sharedMaterial = paper;
            var placeholder = new GameObject("Add a photo here"); placeholder.transform.SetParent(frame.transform, false);
            placeholder.transform.localPosition = new Vector3(0, 0, .075f); placeholder.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var text = placeholder.AddComponent<TextMesh>(); text.text = "Add a photo here"; text.fontSize = 64; text.characterSize = .025f;
            text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center; text.color = new Color(.27f, .23f, .19f);
            var point = frame.AddComponent<InteractionPoint>(); point.kind = InteractionKind.HousePhoto; point.label = "Add a photo"; point.radius = 2.6f;
            frame.AddComponent<BoxCollider>().size = new Vector3(2.5f, 1.75f, .14f);
            var component = frame.AddComponent<HousePhotoFrame>(); component.photoSurface = surface.GetComponent<Renderer>(); component.placeholder = placeholder;
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
