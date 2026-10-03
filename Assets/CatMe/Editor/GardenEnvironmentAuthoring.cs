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

        static Transform root;
        static readonly Dictionary<Material, Material> converted = new Dictionary<Material, Material>();
        static System.Random random;

        [MenuItem("CatMe/Garden/Build First Garden %&g")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring the garden.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null &&
                !EditorUtility.DisplayDialog("Rebuild garden map?", "This replaces the authored GardenEnvironment prefab and its scene instance. Save a copy of manual map edits before continuing.", "Rebuild", "Cancel")) return;
            // Preserve pending scene work automatically, then replace only the authored garden prefab.
            if (!EditorSceneManager.SaveOpenScenes()) return;
            Directory.CreateDirectory(Output + "/Materials");
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            AssetDatabase.Refresh();
            converted.Clear(); random = new System.Random(2909);
            root = new GameObject("GardenEnvironment").transform;
            try
            {
                BuildGround();
                // The clearing stays walkable; scenery is grouped into asymmetric islands.
                string rock = Free + "Misc/Rocks/JG_Rock_A_01.prefab";
                string bush = Free + "Plants/Boxwood/Plant_Boxwood_Spring_01.prefab";
                string fern = Free + "Plants/PaintedFern/Plant_PaintedFern_Spring_01.prefab";
                var islands = Enumerable.Range(0,18).Select(i=>new Vector3((i%2==0?-1:1)*Next(3.5f,4.5f),0,-5+i*3.6f)).ToArray();
                for(int i=0;i<islands.Length;i++)
                {
                    Vector3 c=islands[i];
                    string tree=i%3==0?"SakuraTree_B.prefab":"BlackPineTree_A.prefab";
                    Place(Japanese+tree,"Grove canopy",c,i<5?Next(2.7f,3.5f):Next(4.2f,6.3f),Next(0,360),true);
                    var stone=Place(rock,"Weathered garden rock",c+new Vector3(i%2==0?1:-1,-.12f,.8f),Next(.75f,1.3f),Next(0,360),false);
                    for(int j=0;j<7;j++)
                    {
                        float angle=Next(0,Mathf.PI*2),radius=Next(.7f,1.9f);
                        Vector3 pos=c+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
                        Place(bush,"Layered boxwood",pos,Next(.32f,.85f),Next(0,360),true);
                        Place(fern,"Fern understory",pos+new Vector3(Next(-.6f,.6f),0,Next(-.6f,.6f)),Next(.25f,.52f),Next(0,360),true);
                    }
                }
                for(int i=0;i<38;i++)
                {
                    Vector3 pos=new Vector3((i%2==0?-1:1)*Next(7,10),0,-10+i*2.1f);
                    if(pos.z<43f || (pos.z<65f && pos.x<0))continue;
                    Place(Japanese+(i%5==0?"SakuraTree_B.prefab":"BlackPineTree_A.prefab"),"Journey grove",pos,Next(5.5f,8),Next(0,360),true);
                }
                BuildFlowers();
                Place("Assets/WoodenParkBench/Prefabs/WoodenParkBench.prefab","Shaded resting bench",new Vector3(-3,0,13),1.05f,90,false);
                Place("Assets/LowPolyLivingRoomPack/Prefabs/Armchair_Classic.prefab","Garden reading chair",new Vector3(3.2f,0,38),.95f,-90,false);
                Anchor("FlowerWalk",new Vector3(0,0,8));
                Anchor("RestStop",new Vector3(0,0,15));
                Anchor("BallGlade",new Vector3(0,0,28));
                Anchor("HouseApproach",new Vector3(0,0,54));
                BuildBall();
                // Sparse stones are accents in the gravel, not a repeated tiled runway.
                for(int i=0;i<9;i++)
                {
                    float z=20f+i*.95f;
                    var step=Place(Free+"Misc/Rocks/JG_TobiIshi_A_01.prefab","Inset stepping stone",new Vector3(Mathf.Sin(z*.13f)*.65f,0,z),1,Next(-35,35),false);
                    Fit(step,new Vector3(Next(.65f,.95f),.035f,Next(.52f,.78f)));
                    Ground(step,new Vector3(Mathf.Sin(z*.13f)*.65f+Next(-.18f,.18f),-.014f,z));
                }
                Anchor("OwnerSpawn",new Vector3(0,0,-2.15f));
                Anchor("CatSpawn",new Vector3(1,0,-1.7f));
                Anchor("HideSpot",new Vector3(3.7f,0,-.1f));
                Anchor("ReunionSpot",new Vector3(0,0,2.7f));
                Anchor("BallPlaySpot",new Vector3(0,0,5.4f));
                BuildJourneyRoute();
                PrefabUtility.SaveAsPrefabAsset(root.gameObject,PrefabPath);
                AssetDatabase.SaveAssets();
                EditorSceneManager.OpenScene("Assets/CatMe/Scenes/GardenCompanion.unity");
                var old=GameObject.Find("GardenEnvironment");if(old!=null)Object.DestroyImmediate(old);
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
                EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
                Debug.Log("[CatMe][GardenAuthoring] Built open Japanese garden with layered planting and wind foliage.");
                File.WriteAllText("/tmp/catme-garden-build-result.txt","SUCCESS " + DateTime.Now.ToString("O"));
            }
            finally { if(root!=null)Object.DestroyImmediate(root.gameObject); }
        }

        static void BuildBall()
        {
            var holder=new GameObject("RollingFetchBall");
            var model=Place("Assets/Rolling_Balls-Sci-fi_Pack/Rolling_Balls-Sci-fi_Pack_SRP_Build/Prefab/RollingBalls_Sci-fi_1_1.prefab","Rolling ball model",Vector3.zero,.24f,0,false);
            model.transform.SetParent(holder.transform,true);
            model.transform.position-=BoundsOf(model).center;
            PrefabUtility.SaveAsPrefabAsset(holder,"Assets/CatMe/Resources/CompanionGarden/RollingFetchBall.prefab");
            Object.DestroyImmediate(holder);
        }

        static void BuildFlowers()
        {
            const string folder="Assets/ALP_Assets/GrassFlowersFREE/Textures/GrassFlowers/";
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            for(int i=0;i<3;i++)
            {
                float a=i*Mathf.PI/3;Vector3 side=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*.5f;int n=vertices.Count;
                vertices.Add(-side);vertices.Add(side);vertices.Add(side+Vector3.up);vertices.Add(-side+Vector3.up);
                uv.Add(new Vector2(0,0));uv.Add(new Vector2(1,0));uv.Add(new Vector2(1,1));uv.Add(new Vector2(0,1));
                triangles.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
            }
            string path=Output+"/FlowerClump.asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}
            mesh.Clear();mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            var mats=new Material[10];
            for(int i=0;i<10;i++)
            {
                mats[i]=Solid("Wildflowers "+(i+1),Color.white);mats[i].shader=Shader.Find("CatMe/Garden Foliage");
                mats[i].SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"grassFlower"+(i+1).ToString("00")+".tga"));
                mats[i].SetFloat("_Cutoff",.4f);mats[i].SetFloat("_Wind",.025f);
            }
            for(int i=0;i<250;i++)
            {
                float z=Next(-5,58),side=i%2==0?-1:1;
                float x=Mathf.Sin(z*.13f)*.65f+side*Next(1.7f,2.6f);
                var go=new GameObject("Wildflower drift",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);
                go.transform.localPosition=new Vector3(x,0,z);go.transform.localRotation=Quaternion.Euler(0,Next(0,360),0);go.transform.localScale=Vector3.one*Next(.3f,.65f);
                go.GetComponent<MeshFilter>().sharedMesh=mesh;
                go.GetComponent<MeshRenderer>().sharedMaterial=mats[Mathf.Clamp((int)(z/7),0,9)];
            }
        }

        static void BuildGround()
        {
            // The walkable centre climbs gently after the shelter; steep shoulders are scenery.
            const int across=65, along=193;
            var vertices=new Vector3[across*along];var triangles=new int[(across-1)*(along-1)*6];
            for(int z=0;z<along;z++)for(int x=0;x<across;x++)
            {
                float wx=(x-32)*.5f,wz=z*.8f-18f;
                float outside=Mathf.SmoothStep(0,1,Mathf.Clamp01((Mathf.Abs(wx)-6)/6));
                float riverZone=Mathf.SmoothStep(0,1,Mathf.Clamp01((wz-11f)/3f))*(1f-Mathf.SmoothStep(0,1,Mathf.Clamp01((wz-60f)/3f)));
                float riverBed=riverZone*Mathf.SmoothStep(0,1,Mathf.Clamp01((-wx-4f)/2.5f));
                float height=RouteHeight(wz)+outside*(.3f+Mathf.PerlinNoise(wx*.12f,wz*.12f)*1.4f)-riverBed*2.1f;
                vertices[z*across+x]=new Vector3(wx,height-.025f,wz);
                if(x==across-1||z==along-1)continue;
                int t=(z*(across-1)+x)*6,v=z*across+x;
                triangles[t]=v;triangles[t+1]=v+across;triangles[t+2]=v+1;
                triangles[t+3]=v+1;triangles[t+4]=v+across;triangles[t+5]=v+across+1;
            }
            string path=Output+"/GardenGround.asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}
            mesh.Clear();mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            var mat=Solid("Gravel and moss",Color.white);mat.shader=Shader.Find("CatMe/Garden Ground");
            var go=new GameObject("Gravel clearing and moss floor",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=mat;
            go.AddComponent<MeshCollider>().sharedMesh=mesh;
        }

        static float RouteHeight(float z) => Mathf.SmoothStep(0f, 2.4f, Mathf.Clamp01((z-105f)/25f));

        static void BuildJourneyRoute()
        {
            var wall=Solid("Courtyard low stone",new Color(.69f,.66f,.56f));
            var water=Solid("Riverside water",new Color(.22f,.46f,.49f),.65f);
            var bank=Solid("Riverside bank",new Color(.42f,.50f,.35f));
            var gate=Solid("Courtyard timber gate",new Color(.36f,.25f,.17f));
            // An enclosed first walk opens through a permanently clear 3.6 m gateway.
            for(int side=-1;side<=1;side+=2)
            {
                SolidBox("Courtyard side wall",new Vector3(side*4.8f,.48f,0),new Vector3(.35f,.96f,20),wall);
                SolidBox("Gate flanking wall",new Vector3(side*3.4f,.48f,10),new Vector3(2.8f,.96f,.45f),wall);
                SolidBox("Open gate post",new Vector3(side*1.9f,1.1f,10),new Vector3(.25f,2.2f,.25f),gate);
            }
            Anchor("GardenGate",new Vector3(0,0,10));
            // Scenic river is offset from the safe walking corridor. Its bank has a solid edge.
            Box("Riverside flow",new Vector3(-7.5f,-.22f,36),new Vector3(5,.04f,46),water);
            SolidBox("Riverside safe bank",new Vector3(-4.55f,.34f,36),new Vector3(.5f,.68f,46),bank);
            Anchor("Journey_Home",new Vector3(0,0,-11));
            Anchor("Journey_Garden",new Vector3(0,0,24));
            Anchor("Journey_Woodland",new Vector3(0,0,53));
            Anchor("Journey_Tent",new Vector3(0,0,76));
            Anchor("Journey_Shelter",new Vector3(0,0,101));
            Anchor("Journey_Retreat",new Vector3(0,RouteHeight(132),132));
            Anchor("Journey_Return",new Vector3(0,0,-10));

            // Replaceable, deliberately simple structures. Each is grouped below its own
            // stable root so a supplied pack can replace it without moving route anchors.
            var timber=Solid("Journey placeholder timber",new Color(.38f,.25f,.17f));
            var canvas=Solid("Journey placeholder canvas",new Color(.72f,.60f,.42f));
            var roof=Solid("Journey placeholder warm roof",new Color(.31f,.25f,.20f));
            var stone=Solid("Journey placeholder stone",new Color(.45f,.43f,.36f));
            var house=Group("TEMP_Replaceable_HomePorch",new Vector3(0,0,-13));
            BoxChild(house,"open home back",new Vector3(0,1.8f,-2.3f),new Vector3(7,3.6f,.35f),stone);
            BoxChild(house,"home left wall",new Vector3(-3.3f,1.7f,-.2f),new Vector3(.35f,3.4f,4.4f),stone);
            BoxChild(house,"home right wall",new Vector3(3.3f,1.7f,-.2f),new Vector3(.35f,3.4f,4.4f),stone);
            BoxChild(house,"home porch roof",new Vector3(0,3.65f,.1f),new Vector3(7.2f,.3f,5.5f),roof);
            BoxChild(house,"porch floor",new Vector3(0,.06f,.5f),new Vector3(7, .12f,4.8f),timber);
            for(int side=-1;side<=1;side+=2)BoxChild(house,"porch post",new Vector3(side*3.1f,1.8f,2.5f),new Vector3(.22f,3.5f,.22f),timber);
            var homeThreshold=Anchor("HomeThreshold",new Vector3(0,0,-8.5f));
            homeThreshold.SetParent(house,true);

            var tent=Group("TEMP_Replaceable_Tent",new Vector3(0,0,76));
            BoxChild(tent,"tent ground mat",new Vector3(0,.04f,0),new Vector3(4.6f,.08f,4.5f),canvas);
            // Leave both ends clear for the through-route. The blockout stays readable
            // as a shelter with a floor, roof and side wings, without narrowing movement.
            BoxChild(tent,"tent left wing",new Vector3(-2.02f,1.1f,0),new Vector3(.16f,2.2f,3.8f),canvas);
            BoxChild(tent,"tent right wing",new Vector3(2.02f,1.1f,0),new Vector3(.16f,2.2f,3.8f),canvas);
            BoxChild(tent,"tent roof",new Vector3(0,2.35f,0),new Vector3(4.7f,.18f,4.6f),canvas);
            Anchor("TentEntry",new Vector3(0,0,3.1f));

            var shelter=Group("TEMP_Replaceable_RainShelter",new Vector3(0,0,101));
            BoxChild(shelter,"shelter roof",new Vector3(0,3.2f,0),new Vector3(7.2f,.35f,6.2f),roof);
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
                BoxChild(shelter,"open shelter post",new Vector3(x*3.25f,1.55f,z*2.75f),new Vector3(.22f,3.1f,.22f),timber);
            BoxChild(shelter,"shelter floor",new Vector3(0,.04f,0),new Vector3(6.8f,.08f,5.8f),timber);
            Anchor("ShelterSafe",new Vector3(0,0,101));
            Place("Assets/WoodenParkBench/Prefabs/WoodenParkBench.prefab","Shelter resting bench",new Vector3(1.5f,0,101),.85f,180,false);

            var retreat=Group("TEMP_Replaceable_MountainRetreat",new Vector3(0,RouteHeight(132),132));
            BoxChild(retreat,"retreat far wall left",new Vector3(-1.38f,1.65f,2.6f),new Vector3(1.7f,3.3f,.35f),stone);
            BoxChild(retreat,"retreat far wall right",new Vector3(1.38f,1.65f,2.6f),new Vector3(1.7f,3.3f,.35f),stone);
            BoxChild(retreat,"retreat left wall",new Vector3(-3.3f,1.6f,0),new Vector3(.35f,3.2f,5.2f),stone);
            BoxChild(retreat,"retreat right wall",new Vector3(3.3f,1.6f,0),new Vector3(.35f,3.2f,5.2f),stone);
            BoxChild(retreat,"retreat porch roof",new Vector3(0,3.5f,0),new Vector3(7.2f,.3f,6.5f),roof);
            BoxChild(retreat,"retreat porch floor",new Vector3(0,.06f,.2f),new Vector3(6.8f,.12f,5.7f),timber);
            for(int side=-1;side<=1;side+=2)BoxChild(retreat,"retreat porch post",new Vector3(side*3.1f,1.75f,2.8f),new Vector3(.22f,3.4f,.22f),timber);
            Anchor("RetreatRest",new Vector3(0,RouteHeight(132),132));
            var bench=Place("Assets/LowPolyLivingRoomPack/Prefabs/Armchair_Classic.prefab","Retreat reading chair",new Vector3(-1.65f,0,132),.9f,180,false);

            var mountainMat=Solid("Journey distant mountain",new Color(.31f,.39f,.40f));
            for(int i=0;i<7;i++)
            {
                float x=-27+i*9;
                var peak=Shape(PrimitiveType.Capsule,"Distant mountain silhouette",new Vector3(x,8+(i%3)*2,142),new Vector3(13,18+(i%3)*4,5),mountainMat);
                peak.transform.rotation=Quaternion.Euler(0,0,Next(-8,8));
            }
            // Companion route remains broad and level here; raised shoulders are scenic only.
            for(int i=0;i<16;i++)
            {
                float z=62+i*4.7f;
                Place(Japanese+(i%4==0?"SakuraTree_B.prefab":"BlackPineTree_A.prefab"),"TEMP Woodland approach tree",new Vector3((i%2==0?-1:1)*Next(5.3f,8.2f),0,z),Next(6,9),Next(0,360),true);
            }
        }

        static Transform Group(string name,Vector3 position)
        {var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=position;return go.transform;}
        static void BoxChild(Transform parent,string name,Vector3 localPosition,Vector3 size,Material material)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=localPosition;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;
            // Decorative mats sit above the continuous ground mesh; a solid lip would stop a capsule at the entrance.
            if(name.Contains("floor") || name.Contains("mat"))go.GetComponent<Collider>().isTrigger=true;}

        static void SolidBox(string name,Vector3 position,Vector3 size,Material material)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root,false);go.transform.localPosition=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;}

        static float Next(float a,float b)=>(float)(a+(b-a)*random.NextDouble());
        static Transform Anchor(string name,Vector3 position){var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.localPosition=position;return go.transform;}
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
            if(cutout)
            {
                result.shader=Shader.Find("CatMe/Garden Foliage");
                result.SetFloat("_Wind",.035f);
                result.SetColor("_BaseColor",new Color(.82f,.86f,.78f,1));
            }
            else
            {
                result.shader=Shader.Find("Universal Render Pipeline/Lit");
                var normal=SavedTexture(source,"_BumpMap","_NormalMap");
                if(normal!=null){result.SetTexture("_BumpMap",normal);result.EnableKeyword("_NORMALMAP");}
            }
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
