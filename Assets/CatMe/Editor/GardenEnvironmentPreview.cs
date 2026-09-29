#if UNITY_EDITOR
using System;
using System.IO;
using CatMe.Garden;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatMe.Editor
{
    [InitializeOnLoad]
    public static class GardenEnvironmentPreview
    {
        const string Request = "/tmp/catme-garden-author.request";
        const string CaptureKey = "CatMe.GardenEnvironmentPreview.Active";
        static double readyAt;
        static GardenEnvironmentPreview() { EditorApplication.update += Tick; }
        static void Tick()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating)return;
            if(File.Exists(Request) && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                File.Delete(Request);
                try { GardenEnvironmentAuthoring.Build(); }
                catch(Exception ex) { Debug.LogException(ex);File.WriteAllText("/tmp/catme-garden-build-result.txt",ex.ToString()); }
            }
            if(!SessionState.GetBool(CaptureKey,false) || !EditorApplication.isPlaying)return;
            var demo=UnityEngine.Object.FindAnyObjectByType<GardenCompanionDemo>();
            if(demo==null || !demo.IsReady){readyAt=0;return;}
            if(readyAt==0){readyAt=EditorApplication.timeSinceStartup;return;}
            if(EditorApplication.timeSinceStartup-readyAt<3)return;
            SessionState.SetBool(CaptureKey,false);
            Capture();
        }
        [MenuItem("CatMe/Garden/Preview First Garden")]
        public static void Preview()
        {
            if(!EditorApplication.isPlaying)EditorSceneManager.OpenScene("Assets/CatMe/Scenes/GardenCompanion.unity");
            SessionState.SetBool(CaptureKey,true);readyAt=0;EditorApplication.isPlaying=true;
        }
        [MenuItem("CatMe/Garden/Capture Garden Camera")]
        public static void Capture()
        {
            var camera=Camera.main;if(camera==null)throw new InvalidOperationException("Start garden Play Mode first.");
            const string folder="docs/implementation/garden-phase1";Directory.CreateDirectory(folder);
            var target=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32);
            var previous=camera.targetTexture;var active=RenderTexture.active;
            try
            {
                camera.targetTexture=target;camera.Render();RenderTexture.active=target;
                var texture=new Texture2D(1600,900,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1600,900),0,0);texture.Apply();
                File.WriteAllBytes(folder+"/garden-camera.png",texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
                Debug.Log("[CatMe][GardenPreview] Saved actual Unity camera to "+folder+"/garden-camera.png");
            }
            finally{camera.targetTexture=previous;RenderTexture.active=active;target.Release();UnityEngine.Object.DestroyImmediate(target);}
        }
    }
}
#endif
