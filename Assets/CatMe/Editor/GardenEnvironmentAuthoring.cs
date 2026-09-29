#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatMe.Garden;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CatMe.Editor
{
    // Deterministic, editable first garden. Only the generated prefab/material folder is rebuilt.
    public static class GardenEnvironmentAuthoring
    {
        const string Output = "Assets/CatMe/Art/Garden";
        const string PrefabPath = "Assets/CatMe/Resources/CompanionGarden/GardenEnvironment.prefab";
        const string Japanese = "Assets/Waldemarst/JapaneseGardenPackage/Prefabs/";
        const string Free = "Assets/Waldemarst/FreeJapaneseGarden/Prefabs/";
        const string Fristy = "Assets/Fristy Mobile CG/Prefabs/";
        static Transform root;
        static readonly Dictionary<Material, Material> converted = new Dictionary<Material, Material>();
        static System.Random random;

        [MenuItem("CatMe/Garden/Build First Garden %&g")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring the garden.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Output + "/Materials");
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            AssetDatabase.Refresh();
            converted.Clear(); random = new System.Random(2909);
            root = new GameObject("GardenEnvironment").transform;
            try
            {
                Material grass = Solid("Meadow", new Color(.34f,.43f,.18f));
                Material earth = Solid("Path sand", new Color(.58f,.49f,.32f));
                Material stone = Solid("Warm limestone", new Color(.54f,.51f,.42f));
                Material plaster = Solid("Boundary plaster", new Color(.72f,.65f,.49f));
                Material cap = Solid("Boundary coping", new Color(.25f,.29f,.28f));
                Material water = Solid("Pond jade", new Color(.18f,.40f,.37f), .62f);
                Box("Ground", new Vector3(0,-.17f,3), new Vector3(26,.3f,32), grass);
                // Continuous flat walk surface, with shallow visual stones so small paws never snag.
                for (int i=0;i<26;i++)
                {
                    float z=-8.8f+i*.7f;
                    float x=Mathf.Sin((z+3)*.33f)*.72f;
                    var soil=Disc("Path bed", new Vector3(x,-.008f,z),new Vector3(2.6f,.028f,1.2f),earth);
                    for(int side=0;side<2;side++)
                    {
                        var step=Place(Free+"Misc/Rocks/JG_TobiIshi_A_01.prefab", "Stepping stone",new Vector3(x+(side==0?-.46f:.46f),0,z+(side==0?-.13f:.15f)),1,Next(-22,22),false);
                        Fit(step,new Vector3(Next(.82f,1.04f),.045f,Next(.58f,.74f)));
                        Ground(step, new Vector3(x+(side==0?-.46f:.46f),-.006f,z+(side==0?-.13f:.15f)));
                    }
                }
                Disc("Reunion clearing",new Vector3(0,-.009f,2.7f),new Vector3(6.2f,.025f,5.2f),earth);
                // Low enclosure is scenery beyond the playable lawn, never a ceiling or room.
                Box("Back garden wall",new Vector3(0,.55f,11),new Vector3(22,1.1f,.24f),plaster);
                Box("Back wall coping",new Vector3(0,1.13f,11),new Vector3(22,.12f,.36f),cap);
                foreach(float side in new[]{-1f,1f})
                {
                    Box("Side garden wall",new Vector3(side*10,.55f,2.3f),new Vector3(.24f,1.1f,18),plaster);
                    Box("Side coping",new Vector3(side*10,1.13f,2.3f),new Vector3(.36f,.12f,18),cap);
                }
                var gate=Place(Japanese+"torii.obj","Garden gate landmark",new Vector3(.6f,0,9.3f),3.7f,0,false);
                var gateMat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Waldemarst/JapaneseGardenPackage/Materials/brocoo_jg_torii.mat");
                foreach(var r in gate.GetComponentsInChildren<Renderer>())r.sharedMaterial=Convert(gateMat);
                Place("Assets/WoodenParkBench/Prefabs/WoodenParkBench.prefab","Quiet bench",new Vector3(-4.8f,0,1.6f),1.05f,22,false);
                Place(Japanese+"JapaneseMapleTree_A.prefab","Entry maple",new Vector3(-5.6f,0,-.2f),4.8f,20,true);
                Place(Japanese+"SakuraTree_B.prefab","Reunion blossom",new Vector3(5.5f,0,3.5f),5.3f,210,true);
                Place(Japanese+"BlackPineTree_A.prefab","Bench pine",new Vector3(-6.5f,0,6),4.8f,30,true);
                Place(Japanese+"SakuraTree_B.prefab","Gate blossom",new Vector3(-4.5f,0,10),5.2f,80,true);
                Place(Japanese+"BlackPineTree_A.prefab","Far pine",new Vector3(6.8f,0,11.5f),5.6f,150,true);
                Place(Japanese+"JapaneseMapleTree_A.prefab","Far maple",new Vector3(-8.8f,0,12),5.8f,130,true);
                Place(Japanese+"BlackPineTree_A.prefab","Right canopy",new Vector3(9,0,-1),5.3f,280,true);
                // Pond stays outside the flat movement envelope; no water physics in phase one.
                Disc("Pond bank",new Vector3(6,.0f,3),new Vector3(4.6f,.08f,6.2f),stone);
                Disc("Pond water",new Vector3(6,.055f,3),new Vector3(3.85f,.035f,5.45f),water);
                for(int i=0;i<14;i++)
                {
                    float a=i*Mathf.PI*2/14;
                    Place(Fristy+"3_Rock_1.prefab","Pond edge rock",new Vector3(6+Mathf.Cos(a)*2.05f,0,3+Mathf.Sin(a)*2.75f),Next(.34f,.64f),Next(0,360),false);
                }
                for(int i=0;i<22;i++)
                {
                    float z=-5.5f+(i/2)*1.4f;
                    float side=i%2==0?-1:1;
                    float x=side*Next(4.25f,5.1f);
                    if(side>0 && z>.1f && z<6) x=8.6f;
                    Place(Free+"Plants/Boxwood/Plant_Boxwood_Spring_01.prefab","Garden hedge",new Vector3(x,0,z),Next(.65f,1.05f),Next(0,360),true);
                }
                // Nearby hide anchor is reserved for the later safe hide-and-find opening.
                Place(Free+"Plants/Boxwood/Plant_Boxwood_Spring_01.prefab","Hide-and-find shrub",new Vector3(4.4f,0,-.25f),.83f,50,true);
                for(int i=0;i<48;i++)
                {
                    float z=Next(-7,10),side=i%2==0?-1:1;
                    float x=side*Next(3.9f,4.8f);
                    if(side>0 && z>.2f && z<6)x=Next(8.1f,9.1f);
                    Place(i%3==0?Fristy+"White Plant.prefab":Fristy+"Purple Plant.prefab","Flower drift",new Vector3(x,0,z),Next(.22f,.40f),Next(0,360),false);
                    if(i%3==0)Place(Free+"Plants/PaintedFern/Plant_PaintedFern_Spring_01.prefab","Fern",new Vector3(x+side*.3f,0,z+.35f),Next(.3f,.52f),Next(0,360),true);
                    if(i%2==0)Place(Fristy+"1_Grass_2.prefab","Grass tuft",new Vector3(x-side*.35f,0,z-.2f),Next(.22f,.34f),Next(0,360),false);
                }
                for(int i=0;i<10;i++)
                {
                    float side=i%2==0?-1:1;
                    Place(Fristy+"3_Rock_1.prefab","Planting rock",new Vector3(side*Next(4.5f,7.5f),-.08f,Next(-6,10)),Next(.35f,.85f),Next(0,360),false);
                }
                Anchor("OwnerSpawn",new Vector3(0,0,-2.15f));
                Anchor("CatSpawn",new Vector3(1,0,-1.7f));
                Anchor("HideSpot",new Vector3(3.7f,0,-.1f));
                Anchor("ReunionSpot",new Vector3(0,0,2.7f));
                Anchor("BallPlaySpot",new Vector3(0,0,5.4f));
                PrefabUtility.SaveAsPrefabAsset(root.gameObject,PrefabPath);
                AssetDatabase.SaveAssets();
                EditorSceneManager.OpenScene("Assets/CatMe/Scenes/GardenCompanion.unity");
                var old=GameObject.Find("GardenEnvironment");if(old!=null)Object.DestroyImmediate(old);
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                Debug.Log("[CatMe][GardenAuthoring] Built first garden from imported Japanese Garden, Fristy and Wooden Park Bench assets.");
                File.WriteAllText("/tmp/catme-garden-build-result.txt","SUCCESS " + DateTime.Now.ToString("O"));
            }
            finally { if(root!=null)Object.DestroyImmediate(root.gameObject); }
        }

        static float Next(float a,float b)=>(float)(a+(b-a)*random.NextDouble());
        static void Anchor(string name,Vector3 position){var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=position;}
        static GameObject Place(string path,string name,Vector3 position,float height,float yaw,bool foliage)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(source==null)throw new FileNotFoundException("Garden source asset not imported: "+path);
            var go=Object.Instantiate(source,root);go.name=name;
            foreach(var t in go.GetComponentsInChildren<Transform>(true))GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
            foreach(var script in go.GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(script);
            foreach(var collider in go.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
            foreach(var animator in go.GetComponentsInChildren<Animator>(true))Object.DestroyImmediate(animator);
            // Keep the authored first mesh LOD, remove billboard shader dependencies.
            foreach(var group in go.GetComponentsInChildren<LODGroup>(true))
            {
                var lods=group.GetLODs();
                if(lods.Length>0)
                {
                    int choice=foliage && lods.Length>2?1:0;
                    var keep=new HashSet<Renderer>(lods[choice].renderers.Where(r=>r!=null && !(r is BillboardRenderer)));
                    if(keep.Count==0)keep=new HashSet<Renderer>(lods[0].renderers.Where(r=>r!=null && !(r is BillboardRenderer)));
                    foreach(var r in lods.SelectMany(l=>l.renderers).Where(r=>r!=null).Distinct())if(!keep.Contains(r))r.gameObject.SetActive(false);
                }
                Object.DestroyImmediate(group);
            }
            foreach(var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if(r is BillboardRenderer){r.gameObject.SetActive(false);continue;}
                r.sharedMaterials=r.sharedMaterials.Select(Convert).ToArray();
                r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;
            }
            go.transform.localRotation=Quaternion.Euler(0,yaw,0);
            var bounds=BoundsOf(go);
            if(bounds.size.y>.001f)go.transform.localScale*=height/bounds.size.y;
            Ground(go,position);
            return go;
        }
        static Bounds BoundsOf(GameObject go)
        {
            var rs=go.GetComponentsInChildren<Renderer>().Where(r=>r.enabled && !(r is BillboardRenderer)).ToArray();
            if(rs.Length==0)throw new InvalidOperationException("No visible mesh: "+go.name);
            Bounds b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;
        }
        static void Ground(GameObject go,Vector3 p){Bounds b=BoundsOf(go);go.transform.position+=new Vector3(p.x-b.center.x,p.y-b.min.y,p.z-b.center.z);}
        static void Fit(GameObject go,Vector3 size){var b=BoundsOf(go);go.transform.localScale=Vector3.Scale(go.transform.localScale,new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z));}
        static GameObject Box(string name,Vector3 position,Vector3 scale,Material mat)=>Shape(PrimitiveType.Cube,name,position,scale,mat);
        static GameObject Disc(string name,Vector3 position,Vector3 scale,Material mat)=>Shape(PrimitiveType.Cylinder,name,position,new Vector3(scale.x,scale.y*.5f,scale.z),mat);
        static GameObject Shape(PrimitiveType shape,string name,Vector3 position,Vector3 scale,Material mat)
        {var go=GameObject.CreatePrimitive(shape);go.name=name;go.transform.SetParent(root,false);go.transform.position=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=mat;Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
        static Material Solid(string name,Color color,float smoothness=0)
        {
            string path=Output+"/Materials/"+name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",smoothness);mat.enableInstancing=true;EditorUtility.SetDirty(mat);return mat;
        }
        static Texture SavedTexture(Material source,params string[] keys)
        {
            var array=new SerializedObject(source).FindProperty("m_SavedProperties.m_TexEnvs");
            foreach(string key in keys)for(int i=0;i<array.arraySize;i++)
            {var entry=array.GetArrayElementAtIndex(i);if(entry.FindPropertyRelative("first").stringValue==key){var tex=entry.FindPropertyRelative("second.m_Texture").objectReferenceValue as Texture;if(tex!=null)return tex;}}
            return null;
        }
        static Material Convert(Material source)
        {
            if(source==null)return Solid("Fallback stone",new Color(.5f,.52f,.44f));
            if(converted.TryGetValue(source,out var result))return result;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string guid,out long id);
            string filename=guid+"_"+id;
            result=Solid(filename,Color.white);
            var main=SavedTexture(source,"_BaseMap","_MainTex","_BaseColorMap");
            result.SetTexture("_BaseMap",main);
            string label=(source.name+" "+(main!=null?AssetDatabase.GetAssetPath(main):"")).ToLowerInvariant();
            bool cutout=label.Contains("atlas") || label.Contains("plant") || label.Contains("grass") || label.Contains("leave") || label.Contains("sprout");
            result.SetFloat("_AlphaClip",cutout?1:0);result.SetFloat("_Cutoff",.42f);result.SetFloat("_Cull",cutout?0:2);
            result.SetFloat("_Smoothness",0);result.SetFloat("_Metallic",0);
            if(cutout){result.EnableKeyword("_ALPHATEST_ON");result.renderQueue=2450;result.SetOverrideTag("RenderType","TransparentCutout");}
            else{result.DisableKeyword("_ALPHATEST_ON");result.renderQueue=-1;}
            result.name=source.name+" Garden URP";converted[source]=result;EditorUtility.SetDirty(result);
            if(main!=null && AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(main)) is TextureImporter importer && importer.maxTextureSize>1024)
            {importer.maxTextureSize=1024;importer.mipmapEnabled=true;importer.SaveAndReimport();}
            return result;
        }
    }
}
#endif
