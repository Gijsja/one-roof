using System.Collections.Generic;
using System.IO;
using OneRoof.Presentation.Tower;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace OneRoof.Editor.Tower
{
    /// <summary>Reproducible first-party environment assets; no downloaded art or vendor edits.</summary>
    public static class GoldStandardLevelBuilder
    {
        public const string ScenePath = "Assets/Scenes/Tower_GoldStandard30.unity";
        private const string Folder = "Assets/OneRoof/Art/GoldCity";
        private static Material _material;
        private static Material _skyMaterial;
        private static readonly Color Ink = C(24, 41, 48);
        private static readonly Color Brass = C(198, 150, 85);
        private static readonly Color Glass = C(147, 187, 188, 0);
        private static readonly Color Warm = C(237, 188, 119, 0);

        [MenuItem("One Roof/Build Gold Standard 30-Floor Playground")]
        public static void Build()
        {
            if (!UnityEngine.Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Folder);
            AssetDatabase.Refresh();
            _material = MaterialAsset("City", false);
            _skyMaterial = MaterialAsset("Sky", true);
            var environment = new GameObject("Gold City Environment");
            try
            {
                var sky = new Geometry();
                sky.Rect(0, 55, 360, 150, 35, Color.white);
                Part(environment.transform, "Atmospheric Sky", sky, _skyMaterial);
                for (var layer = 0; layer < 3; layer++)
                    Part(environment.transform, "Skyline " + layer, Skyline(layer), _material);
                Part(environment.transform, "Street and Promenade", Street(), _material);

                var carMesh = Vehicle(false).Save("Electric Taxi");
                var busMesh = Vehicle(true).Save("City Tram");
                var traffic = new List<Transform>();
                for (var i = 0; i < 12; i++)
                {
                    var car = MeshObject(environment.transform, "Traffic " + i, i%5 == 0 ? busMesh : carMesh, _material);
                    car.localPosition = new Vector3(i < 6 ? -18-i*8 : 12+(i-6)*8, -1.08f-(i%2)*.4f, .8f);
                    if (i%2 != 0) car.localScale = new Vector3(-1,1,1);
                    traffic.Add(car);
                }
                var clouds = new List<Transform>();
                var cloud = new Geometry();
                cloud.Ellipse(0, 0, 7, .45f, 0, C(177,187,188));
                cloud.Ellipse(-1, .2f, 4, .65f, -.01f, C(186,195,194));
                cloud.Ellipse(1.7f, .14f, 3.2f, .48f, -.02f, C(194,200,196));
                var cloudMesh = cloud.Save("Wind Clouds");
                for (var i = 0; i < 7; i++)
                {
                    var tr = MeshObject(environment.transform, "Cloud " + i, cloudMesh, _material);
                    tr.localPosition = new Vector3(-90+i*31, 21+i*6, 28);
                    tr.localScale = Vector3.one * (1+i%3*.35f);
                    clouds.Add(tr);
                }
                var bird = new Geometry();
                bird.Line(-.25f,.1f,0,0,.025f,0,Ink); bird.Line(0,0,.25f,.1f,.025f,0,Ink);
                var birdMesh = bird.Save("Swallow");
                var birds = new List<Transform>();
                for (var i = 0; i < 7; i++) birds.Add(MeshObject(environment.transform,"Swallow " + i,birdMesh,_material));
                var component = environment.AddComponent<GoldCityEnvironment>();
                var env = new SerializedObject(component);
                SetArray(env, "_traffic", traffic.ToArray());
                SetArray(env, "_clouds", clouds.ToArray());
                SetArray(env, "_birds", birds.ToArray());
                SetArray(env, "_surfaces", environment.GetComponentsInChildren<Renderer>());
                env.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(environment, Folder + "/GoldCityEnvironment.prefab");
                AssetDatabase.SaveAssets();
                BuildScene();
                AssetDatabase.SaveAssets();
                Debug.Log("GOLD_CITY_BUILT " + ScenePath);
            }
            finally { Object.DestroyImmediate(environment); }
        }

        private static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var towerObject = new GameObject("Tower World — 30 Floors / 300 Residents");
            towerObject.SetActive(false);
            var tower = towerObject.AddComponent<TowerPlayableController>();
            var settings = new SerializedObject(tower);
            settings.FindProperty("_startMode").enumValueIndex = (int)TowerStartMode.GoldStandardCity;
            settings.ApplyModifiedPropertiesWithoutUndo();
            var bootstrap = new GameObject("Gold Standard Playground").AddComponent<GoldStandardPlayground>();
            var boot = new SerializedObject(bootstrap);
            boot.FindProperty("_tower").objectReferenceValue = tower;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/GoldCityEnvironment.prefab");
            if (prefab == null || prefab.GetComponent<GoldCityEnvironment>() == null)
                throw new System.InvalidOperationException("Gold City environment prefab was not imported.");
            boot.FindProperty("_environmentPrefab").objectReferenceValue = prefab.GetComponent<GoldCityEnvironment>();
            boot.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static Geometry Skyline(int layer)
        {
            var g = new Geometry();
            var rng = new System.Random(30100 + layer);
            var z = 22f - layer*5f;
            for (var x = -110f; x < 110;)
            {
                var width = 2.4f+(float)rng.NextDouble()*3.8f;
                var height = (4+(float)rng.NextDouble()*22)*(1-layer*.24f);
                if (x+width > -14 && x < 9) { x=9; continue; }
                var color = layer == 0 ? C(91,123,140) : layer == 1 ? C(62,91,107) : C(36,62,73);
                var baseY = -.65f;
                g.Rect(x+width/2,baseY+height/2,width,height,z,color);
                g.Rect(x+width-.15f,baseY+height/2,.3f,height,z-.01f,new Color(color.r*.83f,color.g*.83f,color.b*.83f,1));
                g.Rect(x+width/2,baseY+height-.12f,width+.16f,.24f,z-.02f,C(118-layer*20,141-layer*25,148-layer*25));
                if (rng.Next(3)==0)
                {
                    g.Rect(x+width*.5f,baseY+height+.6f,width*.56f,1.2f,z,color);
                    g.Rect(x+width*.5f,baseY+height+1.5f,.07f,1.3f,z,Brass);
                    g.Rect(x+width*.5f,baseY+height+2.12f,.13f,.13f,z-.04f,Warm);
                }
                if (layer > 0)
                {
                    for (var row=0; row<height-1; row++)
                        for(var col=.35f; col<width-.3f; col+=.55f)
                        {
                            var lit=rng.Next(5)>1;
                            g.Rect(x+col,baseY+.8f+row,.20f,.34f,z-.03f,lit ? (rng.Next(3)==0 ? Warm : Glass) : C(31,57,68));
                        }
                    for(var y=2.1f;y<height;y+=3.1f) g.Rect(x+width/2,baseY+y,width,.07f,z-.04f,C(76,99,106));
                }
                if (layer==2)
                {
                    g.Rect(x+width/2,.2f,width-.3f,1.6f,z-.05f,Ink);
                    for(var j=.3f;j<width-.35f;j+=.55f) g.Rect(x+j,.23f,.4f,1.15f,z-.07f,Glass);
                    g.Rect(x+width/2,1.07f,width-.15f,.24f,z-.08f,rng.Next(2)==0 ? C(173,94,64) : C(55,126,126));
                    g.Rect(x+width/2,1.34f,width*.65f,.22f,z-.08f,Brass);
                    for(var j=0;j<5;j++) g.Rect(x+width*.25f+j*.23f,1.34f,.12f,.06f,z-.09f,Ink);
                    g.Rect(x+width*.72f,height+.23f,.65f,.45f,z,Ink);
                }
                x+=width+.35f;
            }
            return g;
        }

        private static Geometry Street()
        {
            var g=new Geometry();
            // Continue the earth behind the cutaway so the sky never appears below the street.
            g.Rect(0, -100.8f, 360, 200, 3, C(29, 37, 40));
            foreach(var side in new[]{-1,1})
            {
                var start=side<0 ? -110f : 7f;
                var end=side<0 ? -13f : 110f;
                var center=(start+end)/2;
                g.Rect(center,-2.7f,end-start,4,2,C(28,39,46));
                g.Rect(center,-.76f,end-start,.18f,1.9f,C(119,132,132));
                g.Rect(center,-.58f,end-start,.2f,1.8f,C(88,103,106));
                g.Rect(center,-1.7f,end-start,.035f,1.7f,Brass);
                for(var x=start+.5f;x<end;x+=2.5f) g.Rect(x,-1.25f,.9f,.035f,1.6f,C(185,178,152));
                for(var x=start+2;x<end;x+=6)
                {
                    g.Rect(x,-.15f,.07f,1.4f,1.5f,Ink);
                    g.Rect(x+.22f,.55f,.5f,.075f,1.4f,Ink);
                    g.Rect(x+.36f,.48f,.26f,.07f,1.3f,Warm);
                    g.Rect(x+1,-.35f,.85f,.12f,1.3f,Brass);
                    g.Rect(x+.7f,-.53f,.05f,.25f,1.3f,Ink);
                    g.Rect(x+1.3f,-.53f,.05f,.25f,1.3f,Ink);
                    g.Rect(x+2.8f,-.47f,1.1f,.3f,1.2f,C(90,97,91));
                    g.Rect(x+2.8f,.03f,.12f,.75f,1.15f,C(93,80,66));
                    g.Ellipse(x+2.8f,.45f,.5f,.6f,1.1f,C(66,107,94));
                    g.Ellipse(x+2.63f,.6f,.32f,.4f,1.05f,C(99,135,108));
                }
                // Crosswalk stripes and a glazed transit shelter near the tower.
                var sx=side<0 ? -19f : 13f;
                for(var i=0;i<7;i++) g.Rect(sx+i*.24f,-1.36f,.13f,.6f,1.3f,C(188,187,164));
                g.Rect(sx+4,.04f,2.2f,1.2f,1.4f,C(50,79,87));
                g.Rect(sx+4,.09f,1.95f,.9f,1.3f,Glass);
                g.Rect(sx+4,.69f,2.5f,.1f,1.2f,Ink);
                g.Rect(sx+3.05f,0,.05f,1.3f,1.2f,Brass);
                g.Rect(sx+4.95f,0,.05f,1.3f,1.2f,Brass);
                g.Rect(sx+4.7f,.32f,.3f,.42f,1.1f,Warm);
            }
            return g;
        }

        private static Geometry Vehicle(bool bus)
        {
            var g=new Geometry(); var w=bus?2.3f:1.1f;
            g.Rect(0,.14f,w,.32f,0,bus?C(61,130,131):C(211,159,74));
            g.Rect(-.08f,.35f,w*.7f,.22f,-.01f,Ink);
            for(var x=-w*.3f;x<w*.3f;x+=.3f) g.Rect(x,.35f,.22f,.16f,-.02f,Glass);
            g.Rect(w*.47f,.17f,.09f,.1f,-.03f,Warm);
            g.Rect(-w*.47f,.17f,.07f,.09f,-.03f,C(199,81,60,0));
            g.Ellipse(-w*.32f,0,.12f,.12f,-.03f,Ink); g.Ellipse(w*.32f,0,.12f,.12f,-.03f,Ink);
            g.Rect(0,.08f,w*.7f,.025f,-.04f,Brass);
            return g;
        }

        private static Color C(int r,int g,int b,float a=1) => new Color(r/255f,g/255f,b/255f,a);
        private static Material MaterialAsset(string name,bool sky)
        {
            var path=Folder+"/"+name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null) {mat=new Material(Shader.Find("OneRoof/Gold City")); AssetDatabase.CreateAsset(mat,path);}
            mat.SetFloat("_Sky",sky?1:0); EditorUtility.SetDirty(mat); return mat;
        }
        private static void SetArray<T>(SerializedObject owner,string name,T[] values) where T:Object
        {
            var p=owner.FindProperty(name);p.arraySize=values.Length;
            for(var i=0;i<values.Length;i++) p.GetArrayElementAtIndex(i).objectReferenceValue=values[i];
        }
        private static void Part(Transform root,string name,Geometry g,Material mat) => MeshObject(root,name,g.Save(name),mat);
        private static Transform MeshObject(Transform root,string name,Mesh mesh,Material mat)
        {
            var go=new GameObject(name);go.transform.SetParent(root,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=mat;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            return go.transform;
        }
        private sealed class Geometry
        {
            private readonly List<Vector3> _vertices=new List<Vector3>();
            private readonly List<Color> _colors=new List<Color>();
            private readonly List<int> _indices=new List<int>();
            public void Rect(float x,float y,float w,float h,float z,Color color)
            {
                Quad(new Vector3(x-w/2,y-h/2,z),new Vector3(x-w/2,y+h/2,z),new Vector3(x+w/2,y+h/2,z),new Vector3(x+w/2,y-h/2,z),color);
            }
            private void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color)
            {
                var i=_vertices.Count;_vertices.Add(a);_vertices.Add(b);_vertices.Add(c);_vertices.Add(d);
                for(var n=0;n<4;n++)_colors.Add(color);
                _indices.Add(i);_indices.Add(i+1);_indices.Add(i+2);_indices.Add(i);_indices.Add(i+2);_indices.Add(i+3);
            }
            public void Line(float x,float y,float tx,float ty,float width,float z,Color color)
            {
                var a=new Vector3(x,y,z);var b=new Vector3(tx,ty,z);var offset=Vector3.Cross((b-a).normalized,Vector3.forward)*width*.5f;
                Quad(a-offset,a+offset,b+offset,b-offset,color);
            }
            public void Ellipse(float x,float y,float rx,float ry,float z,Color color)
            {
                for(var n=0;n<20;n++)
                {
                    var a=n*Mathf.PI/10;var b=(n+1)*Mathf.PI/10;
                    Quad(new Vector3(x,y,z),new Vector3(x+Mathf.Cos(a)*rx,y+Mathf.Sin(a)*ry,z),new Vector3(x+Mathf.Cos(b)*rx,y+Mathf.Sin(b)*ry,z),new Vector3(x,y,z),color);
                }
            }
            public Mesh Save(string name)
            {
                var path=Folder+"/"+name+".asset";
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(mesh==null){mesh=new Mesh{name=name};AssetDatabase.CreateAsset(mesh,path);} else mesh.Clear();
                mesh.indexFormat=IndexFormat.UInt32;
                mesh.SetVertices(_vertices);mesh.SetColors(_colors);mesh.SetTriangles(_indices,0);mesh.RecalculateBounds();
                EditorUtility.SetDirty(mesh);return mesh;
            }
        }
    }
}
