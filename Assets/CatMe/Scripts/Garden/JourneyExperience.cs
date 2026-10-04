using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace CatMe.Garden
{
    /// <summary>Small, local-only chapter/weather/story/photo layer for the connected outing.</summary>
    internal sealed class JourneyExperience : MonoBehaviour
    {
        const string ActiveKey = "CatMe.Journey.Active.v1";
        const string CheckpointKey = "CatMe.Journey.CheckpointZ.v1";
        const string StoryKey = "CatMe.Journey.StoryEnabled";
        const string StoryIndexKey = "CatMe.Journey.LastStory.v1";
        const string StoryMaskKey = "CatMe.Journey.StoryMask.v1";
        const string RainKey = "CatMe.Journey.RainStarted.v1";
        const string ShelterKey = "CatMe.Journey.ShelterReached.v1";
        const string RetreatKey = "CatMe.Journey.RetreatReached.v1";
        const string RetreatUtcKey = "CatMe.Journey.RetreatUtc.v1";
        const string CompletedKey = "CatMe.Journey.Completed.v1";
        const int PhotoLimit = 20;
        GardenCompanionDemo garden;
        Transform owner, cat;
        Camera view;
        Canvas canvas;
        Text existingStatus, storyText, photoNotice;
        Image storyBacking;
        Shadow storyShadow;
        GameObject gallery, photoMode;
        Image galleryImage;
        Text galleryCaption;
        Button previousPhoto, nextPhoto, deletePhoto, savePhoto;
        Light sun;
        ParticleSystem rain;
        AudioSource weatherAudio;
        AudioClip rainLoop;
        string[] photos = Array.Empty<string>();
        int photoIndex;
        bool capturePending, deleteArmed;
        bool active, rainStarted, shelterReached, retreatReached, completed;
        float rainStart, shelteredSince = -1f, retreatPauseSince = -1f, storyUntil, storyAlpha, noticeUntil;
        int lastStory = -1;
        float lastStoryAt = -1000f;
        float baseSun = 1.15f;
        Color baseFog = new Color(.86f,.48f,.31f);
        /// <summary>When true the place-based SessionDirector owns route, weather and lighting; this keeps photos, album and story.</summary>
        internal bool SessionMode;
        float retreatStartedAt = -1f, skyExposure = 1f, rainBlend, eveningBlend, nightBlend;
        Light shelterLight;

        public void Initialize(GardenCompanionDemo ownerDemo, Transform ownerTransform, Transform catTransform, Camera camera, Transform canvasRoot, Text status)
        {
            garden = ownerDemo; owner = ownerTransform; cat = catTransform; view = camera;
            canvas = canvasRoot.GetComponent<Canvas>(); existingStatus = status;
            sun = GameObject.Find("Garden sunlight")?.GetComponent<Light>();
            if (sun != null) baseSun = sun.intensity;
            if (RenderSettings.skybox != null)
            {
                // Drive a runtime copy so lighting never edits the shared skybox asset (a darkened asset left a black home sky).
                RenderSettings.skybox = new Material(RenderSettings.skybox);
                if (RenderSettings.skybox.HasProperty("_Exposure")) { skyExposure = Mathf.Max(1f, RenderSettings.skybox.GetFloat("_Exposure")); RenderSettings.skybox.SetFloat("_Exposure", skyExposure); }
            }
            BuildStory(canvasRoot);
            CreateWeather();
            BuildGallery(canvasRoot);
            BuildPhotoMode(canvasRoot);
            RestoreLocalSnapshot();
            bool wasActive = PlayerPrefs.GetInt(ActiveKey, 0) == 1 && !SessionMode;
            if (wasActive)
            {
                garden.RestoreJourneyPosition(PlayerPrefs.GetFloat(CheckpointKey, -10f));
                active = true;
                rainStarted = PlayerPrefs.GetInt(RainKey,0)==1;
                shelterReached = PlayerPrefs.GetInt(ShelterKey,0)==1;
                retreatReached = PlayerPrefs.GetInt(RetreatKey,0)==1;
                lastStory = PlayerPrefs.GetInt(StoryIndexKey,-1);
                if (retreatReached && DateTime.TryParse(PlayerPrefs.GetString(RetreatUtcKey,""),out DateTime retreatUtc))
                    retreatStartedAt = Time.time-Mathf.Max(0f,(float)(DateTime.UtcNow-retreatUtc.ToUniversalTime()).TotalSeconds);
                if (rainStarted && !shelterReached) { rainStart=Time.time;SetRain(true); }
                HideStatusBar();
            }
        }

        public void StartOuting()
        {
            active = true; completed = false; lastStory = -1; lastStoryAt = -1000f;
            rainStarted = shelterReached = retreatReached = false; shelteredSince = retreatPauseSince = -1f;
            retreatStartedAt = -1f;
            lastStory = -1;lastStoryAt=-1000f;storyUntil=0;storyAlpha=0;
            foreach(string key in new[]{StoryIndexKey,StoryMaskKey,RainKey,ShelterKey,RetreatKey,RetreatUtcKey,"CatMe.Journey.LastStoryUtc"})PlayerPrefs.DeleteKey(key);
            PlayerPrefs.SetInt(ActiveKey, 1);
            PlayerPrefs.SetInt(CompletedKey,0);
            PlayerPrefs.SetFloat(CheckpointKey, -10f);
            SaveNow();
            HideStatusBar();
            storyText.text = string.Empty;
        }

        public void ResumeOuting()
        {
            if (PlayerPrefs.GetInt(ActiveKey, 0) != 1) return;
            active = true;
            HideStatusBar();
        }

        internal bool IsActive => PlayerPrefs.GetInt(ActiveKey,0)==1;
        internal bool IsCompleted => PlayerPrefs.GetInt(CompletedKey,0)==1;

        internal void SaveNow()
        {
            PlayerPrefs.Save();
            try
            {
                var data = new JourneySnapshot {
                    schema = 1, mapRevision = 2,
                    checkpointId = CheckpointId(PlayerPrefs.GetFloat(CheckpointKey,-10f)),
                    active = PlayerPrefs.GetInt(ActiveKey,0)==1,
                    completed = PlayerPrefs.GetInt(CompletedKey,0)==1,
                    rainStarted = PlayerPrefs.GetInt(RainKey,0)==1,
                    sheltered = PlayerPrefs.GetInt(ShelterKey,0)==1,
                    retreatReached = PlayerPrefs.GetInt(RetreatKey,0)==1
                };
                string path = SnapshotPath(), pending = path + ".pending", backup = path + ".bak";
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(pending,JsonUtility.ToJson(data));
                if(File.Exists(path))File.Replace(pending,path,backup);
                else File.Move(pending,path);
            }
            catch(Exception error){Debug.LogWarning("[CatMe][Journey] Local checkpoint snapshot failed: " + error.Message);}
        }

        void RestoreLocalSnapshot()
        {
            JourneySnapshot data = ReadSnapshot(SnapshotPath()) ?? ReadSnapshot(SnapshotPath()+".bak");
            if(data==null || data.schema!=1)return;
            float z = CheckpointZ(data.checkpointId);
            if(float.IsNaN(z))return;
            PlayerPrefs.SetFloat(CheckpointKey,z);
            PlayerPrefs.SetInt(ActiveKey,data.active?1:0);
            PlayerPrefs.SetInt(CompletedKey,data.completed?1:0);
            PlayerPrefs.SetInt(RainKey,data.rainStarted?1:0);
            PlayerPrefs.SetInt(ShelterKey,data.sheltered?1:0);
            PlayerPrefs.SetInt(RetreatKey,data.retreatReached?1:0);
            PlayerPrefs.Save();
        }

        static JourneySnapshot ReadSnapshot(string path)
        {
            if(!File.Exists(path))return null;
            try{return JsonUtility.FromJson<JourneySnapshot>(File.ReadAllText(path));}
            catch(Exception error){Debug.LogWarning("[CatMe][Journey] Invalid checkpoint snapshot: " + error.Message);return null;}
        }
        static string SnapshotPath()=>Path.Combine(Application.persistentDataPath,"CatMeJourney","checkpoint-v1.json");
        static string CheckpointId(float z)=>z>=120f?"retreat":z>=96f?"shelter":z>=68f?"tent":z>=47f?"woodland":z>=12f?"garden":"home";
        static float CheckpointZ(string id)
        {
            string anchor = id=="retreat"?"Journey_Retreat":id=="shelter"?"Journey_Shelter":id=="tent"?"Journey_Tent":id=="woodland"?"Journey_Woodland":id=="garden"?"Journey_Garden":id=="home"?"Journey_Home":null;
            if(anchor==null)return float.NaN;
            var found=GameObject.Find(anchor);
            if(found!=null)return found.transform.position.z;
            return id=="retreat"?128f:id=="shelter"?101f:id=="tent"?74f:id=="woodland"?52f:id=="garden"?21f:-10f;
        }

        void HideStatusBar()
        {
            // Hide the glass plate along with its loading/status label once the quiet
            // outing presentation takes over.
            if (existingStatus == null) return;
            if (existingStatus.transform.parent != null) existingStatus.transform.parent.gameObject.SetActive(false);
            else existingStatus.gameObject.SetActive(false);
        }

        public bool CanThrowBall(Vector3 p) => SessionMode || !(p.z > 68f && p.z < 83f) && !(p.z > 94f && p.z < 108f) && p.z < 124f;
        public void SetSoundEnabled(bool enabled) { if (weatherAudio != null) weatherAudio.mute = !enabled; }
        public void SetReducedMotion(bool enabled)
        {
            PlayerPrefs.SetInt("CatMe.ReduceMotion",enabled?1:0);PlayerPrefs.Save();
            if (enabled && rain != null) rain.Stop(true,ParticleSystemStopBehavior.StopEmitting);
            else if (active && rainStarted && !shelterReached) SetRain(true);
        }

        void Update()
        {
            if (SessionMode) { FadeStory(); return; }
            if (!active || owner == null || cat == null) { FadeStory(); if (owner != null) UpdateLighting(); return; }
            float z = owner.position.z;
            if (z >= 12f) Story(0, "The garden feels familiar.");
            if (z >= 52f && !rainStarted)
            {
                rainStarted = true; rainStart = Time.time; SetRain(true);
                PlayerPrefs.SetInt(RainKey,1);SaveNow();
            }
            bool ownerSheltered = InShelter(owner.position);
            bool catSheltered = InShelter(cat.position);
            if (ownerSheltered && catSheltered)
            {
                if (shelteredSince < 0f) shelteredSince = Time.time;
                if (Time.time - shelteredSince > 13f)
                {
                    shelterReached = true;
                    PlayerPrefs.SetInt(ShelterKey,1);SaveNow();
                    SetRain(false);
                    Story(1, "We can wait here.");
                }
            }
            else shelteredSince = -1f;
            if (rainStarted && !shelterReached && Time.time - rainStart > 95f) SetRain(false);

            if (z > 120f && !retreatReached)
            {
                retreatReached = true; retreatStartedAt = Time.time;
                PlayerPrefs.SetInt(RetreatKey,1);PlayerPrefs.SetString(RetreatUtcKey,DateTime.UtcNow.ToString("O"));SaveNow();
            }
            if (z > 120f)
            {
                if (garden.CurrentMoveMagnitude < .08f && Vector3.Distance(owner.position, cat.position) < 3.3f)
                {
                    if (retreatPauseSince < 0f) retreatPauseSince = Time.time;
                    if (Time.time - retreatPauseSince > 5f) Story(2, "Worth the walk.");
                }
                else retreatPauseSince = -1f;
            }
            if (retreatReached && z < -8.1f && !completed)
            {
                completed = true;
                Story(3, "Home again.");
                PlayerPrefs.SetInt(ActiveKey, 0);
                PlayerPrefs.SetInt(CompletedKey, 1);
                PlayerPrefs.SetFloat(CheckpointKey,-10f);
                SaveNow();
                active = false;
                SetRain(false);
            }
            SaveCheckpoint(z > 120f ? 128f : z > 96f ? 101f : z > 68f ? 74f : z > 47f ? 52f : z > 12f ? 21f : -10f);
            if (gallery != null && gallery.activeSelf) RefreshGalleryCaption();
            FadeStory();
            UpdateLighting();
            FollowRainEmitter();
        }

        void SaveCheckpoint(float z)
        {
            float old = PlayerPrefs.GetFloat(CheckpointKey, -10f);
            if (Mathf.Abs(old - z) < .01f) return;
            PlayerPrefs.SetFloat(CheckpointKey, z); SaveNow();
        }

        static bool InShelter(Vector3 p) => Mathf.Abs(p.x) < 2.6f && Mathf.Abs(p.z - 101f) < 2.15f;

        void Story(int index, string line)
        {
            int mask=PlayerPrefs.GetInt(StoryMaskKey,0);
            if (index <= lastStory || (mask & (1<<index))!=0) return;
            if (PlayerPrefs.GetInt(StoryKey,0)==0) { ConsumeStory(index,mask);return; }
            string previous=PlayerPrefs.GetString("CatMe.Journey.LastStoryUtc","");
            if(DateTime.TryParse(previous,out DateTime previousUtc)&&(DateTime.UtcNow-previousUtc.ToUniversalTime()).TotalSeconds<45)return;
            ConsumeStory(index,mask);
            lastStoryAt=Time.time; storyUntil = Time.time + PlayerPrefs.GetFloat("CatMe.Journey.StoryHold",4.2f);
            storyText.text = line;
        }

        void ConsumeStory(int index,int mask)
        {
            lastStory=index;PlayerPrefs.SetInt(StoryIndexKey,index);PlayerPrefs.SetInt(StoryMaskKey,mask|(1<<index));
            PlayerPrefs.SetString("CatMe.Journey.LastStoryUtc",DateTime.UtcNow.ToString("O"));PlayerPrefs.Save();
        }

        void FadeStory()
        {
            if (storyText == null) return;
            bool reducedMotion = PlayerPrefs.GetInt("CatMe.ReduceMotion",0)==1;
            float target = Time.time < storyUntil && PlayerPrefs.GetInt(StoryKey,0)==1 ? 1f : 0f;
            storyAlpha = reducedMotion ? target : Mathf.MoveTowards(storyAlpha, target, Time.unscaledDeltaTime * .75f);
            var c = storyText.color; c.a = storyAlpha; storyText.color = c;
            var bg = storyBacking.color; bg.a = storyAlpha * .14f; storyBacking.color = bg;
            if (storyShadow != null) storyShadow.effectColor = new Color(.08f,.1f,.08f,storyAlpha*.7f);
        }

        void CreateWeather()
        {
            var go = new GameObject("Journey rain emitter");
            rain = go.AddComponent<ParticleSystem>();
            var main = rain.main; main.loop = true; main.playOnAwake = false; main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 420; main.startLifetime = 1.15f; main.startSpeed = 14f; main.startSize = .035f;
            main.startColor = new Color(.78f,.84f,.91f,.55f); main.gravityModifier = .08f;
            var emission = rain.emission; emission.rateOverTime = 220f;
            // Drops die on roofs, ground and bodies, so rain visibly stops at the shelter roof.
            var collision = rain.collision; collision.enabled = true; collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D; collision.quality = ParticleSystemCollisionQuality.Low;
            collision.lifetimeLoss = 1f; collision.bounce = 0f; collision.dampen = 1f; collision.radiusScale = .5f; collision.maxCollisionShapes = 64;
            var lantern = new GameObject("Shelter lantern"); lantern.transform.position = new Vector3(0f, 2.7f, 101f);
            shelterLight = lantern.AddComponent<Light>(); shelterLight.type = LightType.Point; shelterLight.range = 8f;
            shelterLight.color = new Color(1f, .72f, .42f); shelterLight.intensity = 0f; shelterLight.shadows = LightShadows.None;
            var shape = rain.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(20f, 9f, 22f);
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default");
            if (shader != null) { renderer.material = new Material(shader); renderer.material.color = new Color(.74f,.81f,.9f,.5f); }
            renderer.renderMode = ParticleSystemRenderMode.Stretch; renderer.velocityScale = .025f;
            go.transform.position = new Vector3(0, 7, 0);
            weatherAudio = gameObject.AddComponent<AudioSource>(); weatherAudio.loop = true; weatherAudio.volume = .13f;
            rainLoop = Resources.Load<AudioClip>("CatMeAudio/rain-light");
            weatherAudio.clip = rainLoop; weatherAudio.mute = PlayerPrefs.GetInt("CatMe.SoundEnabled",1) == 0;
        }

        void SetRain(bool enabled)
        {
            if (rain == null) return;
            if (enabled) { if (PlayerPrefs.GetInt("CatMe.ReduceMotion",0)==0 && !rain.isPlaying) rain.Play(); if (rainLoop != null && !weatherAudio.isPlaying) weatherAudio.Play(); }
            else { rain.Stop(true, ParticleSystemStopBehavior.StopEmitting); if (weatherAudio.isPlaying) weatherAudio.Stop(); }
        }

        void FollowRainEmitter()
        {
            if (rain == null || view == null) return;
            rain.transform.position = view.transform.position + Vector3.up * 7f;
            // Both companions must be under the roof before the local rain view is suppressed.
            if (InShelter(owner.position) && InShelter(cat.position)) rain.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            else if (rainStarted && !shelterReached && PlayerPrefs.GetInt("CatMe.ReduceMotion",0)==0 && !rain.isPlaying) SetRain(true);
            if (weatherAudio != null) weatherAudio.volume = InShelter(owner.position) ? .045f : .13f;
        }

        void UpdateLighting()
        {
            bool homecoming = !active && PlayerPrefs.GetInt(CompletedKey,0)==1;
            float eveningTarget = homecoming ? 1f : Mathf.Max(retreatReached ? 1f : 0f, Mathf.Clamp01((owner.position.z - 101f) / 31f));
            float nightTarget = homecoming ? 1f : retreatStartedAt < 0f ? 0f : Mathf.Clamp01((Time.time - retreatStartedAt) / 32f);
            float rainTarget = active && rainStarted && !shelterReached ? 1f : 0f;
            float step = Time.unscaledDeltaTime / 3f;
            rainBlend = Mathf.MoveTowards(rainBlend, rainTarget, step);
            eveningBlend = Mathf.MoveTowards(eveningBlend, eveningTarget, step);
            nightBlend = Mathf.MoveTowards(nightBlend, nightTarget, step);
            float dusk = Mathf.Max(eveningBlend, nightBlend);
            float dim = Mathf.Max(dusk, rainBlend * .55f);
            if (sun != null)
            {
                sun.intensity = Mathf.Lerp(baseSun, .24f, dim);
                Color day = Color.Lerp(new Color(1f,.7f,.42f), new Color(.7f,.76f,.85f), rainBlend);
                sun.color = Color.Lerp(day, new Color(.53f,.62f,.78f), dusk);
            }
            RenderSettings.ambientIntensity = Mathf.Lerp(.8f,.43f,nightBlend);
            Color fog = Color.Lerp(baseFog, new Color(.55f,.6f,.66f), rainBlend);
            RenderSettings.fogColor = Color.Lerp(fog, new Color(.19f,.24f,.32f), nightBlend);
            RenderSettings.fogDensity = Mathf.Lerp(.011f,.021f,Mathf.Max(nightBlend,rainBlend));
            if (RenderSettings.skybox != null && RenderSettings.skybox.HasProperty("_Exposure"))
                RenderSettings.skybox.SetFloat("_Exposure",skyExposure*Mathf.Lerp(1f,.32f,nightBlend)*Mathf.Lerp(1f,.7f,rainBlend));
            // Warm local light keeps the shelter inviting against the cool exterior.
            if (shelterLight != null) shelterLight.intensity = Mathf.Max(rainBlend, nightBlend) * 1.6f;
        }

        void BuildStory(Transform root)
        {
            var backing = new GameObject("Journey story backing",typeof(RectTransform),typeof(Image)); backing.transform.SetParent(root,false);
            var rect = backing.GetComponent<RectTransform>(); rect.anchorMin=rect.anchorMax=new Vector2(.5f,.91f); rect.sizeDelta=new Vector2(640,58);
            storyBacking=backing.GetComponent<Image>(); storyBacking.color=new Color(.12f,.14f,.11f,0f); storyBacking.raycastTarget=false;
            var label=new GameObject("Journey story line",typeof(RectTransform),typeof(Text)); label.transform.SetParent(backing.transform,false);
            var textRect=label.GetComponent<RectTransform>(); textRect.anchorMin=Vector2.zero;textRect.anchorMax=Vector2.one;textRect.offsetMin=new Vector2(14,2);textRect.offsetMax=new Vector2(-14,-2);
            storyText=label.GetComponent<Text>();storyText.font=CatMe.Garden.CatMeUIStyle.UiFont;storyText.fontSize=30;storyText.alignment=TextAnchor.MiddleCenter;storyText.color=new Color(.99f,.97f,.91f,0);storyText.raycastTarget=false;
            var shadow=label.AddComponent<Shadow>();shadow.effectColor=new Color(.08f,.1f,.08f,.7f);shadow.effectDistance=new Vector2(1,-2);
            storyShadow=shadow;
        }

        void BuildGallery(Transform root)
        {
            gallery=new GameObject("Journey photo album",typeof(RectTransform),typeof(Image));gallery.transform.SetParent(root,false);
            var rect=gallery.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            gallery.GetComponent<Image>().color=new Color(.035f,.055f,.045f,.28f);
            var sheet=new GameObject("Photo album sheet",typeof(RectTransform),typeof(Image));sheet.transform.SetParent(gallery.transform,false);
            var sheetRect=sheet.GetComponent<RectTransform>();sheetRect.anchorMin=sheetRect.anchorMax=new Vector2(.5f,.5f);sheetRect.sizeDelta=new Vector2(1320,850);
            CatMeUIStyle.ApplySurface(sheet.GetComponent<Image>(),CatMeUIStyle.Paper);CatMeUIStyle.SetElevation(sheetRect,CatMeUIStyle.Elevation.Floating);
            {var at=MakeLabel(sheet.transform,"Photo album title","Photo album",new Vector2(.5f,1f),new Vector2(780,70));at.rectTransform.anchoredPosition=new Vector2(0,-54);at.font=CatMeUIStyle.DisplayFont;at.fontSize=50;at.color=CatMeUIStyle.Ink;}
            var frame=new GameObject("Photo preview",typeof(RectTransform),typeof(Image));frame.transform.SetParent(sheet.transform,false);
            var f=frame.GetComponent<RectTransform>();f.anchorMin=f.anchorMax=new Vector2(.5f,.55f);f.sizeDelta=new Vector2(1120,620);
            galleryImage=frame.GetComponent<Image>();galleryImage.preserveAspect=true;galleryImage.color=Color.white;
            galleryCaption=MakeLabel(sheet.transform,"Photo caption","Photos stay on this phone.",new Vector2(.5f,.5f),new Vector2(780,44));galleryCaption.rectTransform.anchoredPosition=new Vector2(0,-297);galleryCaption.color=CatMeUIStyle.MutedInk;
            previousPhoto=Round(sheet.transform,"back","Previous photo",new Vector2(0f,.55f),new Vector2(-585,0),()=>MovePhoto(-1));
            nextPhoto=Round(sheet.transform,"arrow","Next photo",new Vector2(1f,.55f),new Vector2(585,0),()=>MovePhoto(1));
            deletePhoto=MakeButton(sheet.transform,"Delete selected photo","Delete photo",new Vector2(.5f,0f),Vector2.zero,new Vector2(260,72),DeleteSelectedPhoto);deletePhoto.GetComponent<RectTransform>().anchoredPosition=new Vector2(200,62);CatMeUIStyle.StyleButton(deletePhoto,CatMeUIStyle.Mist,false);{var dt=deletePhoto.GetComponentInChildren<Text>();if(dt!=null)dt.color=CatMeUIStyle.Danger;}
            savePhoto=MakeButton(sheet.transform,"Save selected photo","Save to Photos",new Vector2(.5f,0f),Vector2.zero,new Vector2(280,72),SaveSelectedPhoto);savePhoto.GetComponent<RectTransform>().anchoredPosition=new Vector2(-120,62);CatMeUIStyle.StyleButton(savePhoto,CatMeUIStyle.SageDeep,true);
            Round(sheet.transform,"back","Close photo album",new Vector2(0f,1f),new Vector2(70,-58),CloseGallery);
            gallery.SetActive(false);
        }

        void BuildPhotoMode(Transform root)
        {
            photoMode=new GameObject("Journey photo mode",typeof(RectTransform),typeof(Image));photoMode.transform.SetParent(root,false);
            var rect=photoMode.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            photoMode.GetComponent<Image>().color=new Color(0,0,0,0);
            Round(photoMode.transform,"close","Close photo mode",new Vector2(0f,1f),new Vector2(80,-70),ClosePhotoMode);
            {var shutter=Round(photoMode.transform,"camera","Photo shutter",new Vector2(.5f,0f),new Vector2(0,120),Shutter);((RectTransform)shutter.transform).sizeDelta=new Vector2(150,150);shutter.transform.Find("Icon camera").GetComponent<RectTransform>().sizeDelta=new Vector2(76,76);}
            Round(photoMode.transform,"album","Open album",new Vector2(1f,1f),new Vector2(-80,-70),()=>{photoMode.SetActive(false);OpenGallery();});
            photoMode.SetActive(false);
        }

        void ClosePhotoMode(){photoMode.SetActive(false);Time.timeScale=1f;garden.SetInputBlocked(false);garden.ClearMovement();}
        /// <summary>Runs once when the album closes (the menu sets it so Back returns to the menu).</summary>
        internal Action AlbumClosed;
        void CloseGallery(){gallery.SetActive(false);Time.timeScale=1f;garden.SetInputBlocked(false);garden.ClearMovement();var closed=AlbumClosed;AlbumClosed=null;closed?.Invoke();}

        void CapturePhoto() { if (!active && !SessionMode) { ShowNotice("Start an outing to save a photo."); return; } garden.SetInputBlocked(true);garden.ClearMovement();Time.timeScale=0f;photoMode.SetActive(true);photoMode.transform.SetAsLastSibling(); }
        internal void TakePhoto() => CapturePhoto();
        void Shutter(){if(!capturePending)StartCoroutine(CaptureFrame());}
        IEnumerator CaptureFrame()
        {
            capturePending=true;
            if (ListPhotos().Length >= PhotoLimit) { capturePending=false;ShowNotice("Your album is full."); yield break; }
            canvas.gameObject.SetActive(false);
            yield return new WaitForEndOfFrame();
            Texture2D frame=null;
            try
            {
                frame=ScreenCapture.CaptureScreenshotAsTexture();
                string folder=PhotoFolder();Directory.CreateDirectory(folder);
                string file=DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")+".png";
                string full=Path.Combine(folder,file);File.WriteAllBytes(full,frame.EncodeToPNG());
                File.WriteAllText(full+".json",JsonUtility.ToJson(new PhotoMetadata{file=file,createdUtc=DateTime.UtcNow.ToString("O"),zone=ZoneName(owner.position.z)}));
                ShowNotice("Photo saved on this phone.");
            }
            catch(Exception e){Debug.LogWarning("[CatMe][Journey] Photo capture failed: "+e.Message);ShowNotice("Photo could not be saved.");}
            finally { if(frame!=null)Destroy(frame);canvas.gameObject.SetActive(true);capturePending=false;photoMode.transform.SetAsLastSibling(); }
        }

        void OpenGallery()
        {
            garden.SetInputBlocked(true);garden.ClearMovement();Time.timeScale=0f;deleteArmed=false;
            photos=ListPhotos();photoIndex=Mathf.Clamp(photoIndex,0,Mathf.Max(0,photos.Length-1));gallery.SetActive(true);gallery.transform.SetAsLastSibling();ShowPhoto();
        }
        internal void OpenPhotoAlbum() => OpenGallery();
        void SaveSelectedPhoto()
        {
            if(photos==null||photos.Length==0)return;
            ShowNotice(DeviceBridge.SaveToPhotos(photos[photoIndex])?"Saved to your Photos.":"This photo stays in the album on this phone.");
        }
        void MovePhoto(int direction){if(photos.Length==0)return;deleteArmed=false;photoIndex=(photoIndex+direction+photos.Length)%photos.Length;ShowPhoto();}
        void ShowPhoto()
        {
            if(photos.Length==0){galleryImage.sprite=null;galleryImage.color=new Color(.2f,.22f,.2f,1);galleryCaption.text="No photos yet. Tap the camera on your walk.";deletePhoto.interactable=false;if(savePhoto!=null)savePhoto.interactable=false;return;}
            deletePhoto.interactable=true;if(savePhoto!=null)savePhoto.interactable=true;
            var bytes=File.ReadAllBytes(photos[photoIndex]);var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
            if(!texture.LoadImage(bytes)){Destroy(texture);galleryCaption.text="This photo could not be opened.";return;}
            var old=galleryImage.sprite;galleryImage.sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f));galleryImage.color=Color.white;if(old!=null){Destroy(old.texture);Destroy(old);}
            string json=photos[photoIndex]+".json";string detail="Photo "+(photoIndex+1)+" of "+photos.Length;
            if(File.Exists(json)){try{var data=JsonUtility.FromJson<PhotoMetadata>(File.ReadAllText(json));detail=ZoneLabel(data.zone)+"  ·  "+DateTime.Parse(data.createdUtc).ToLocalTime().ToString("d MMM, HH:mm")+"  ·  "+(photoIndex+1)+" of "+photos.Length;}catch{}}
            galleryCaption.text=detail;
        }
        void RefreshGalleryCaption(){if(gallery.activeSelf&&photos.Length==0)galleryCaption.text="No photos yet. Tap the camera on your walk.";}
        void DeleteSelectedPhoto()
        {
            if(photos.Length==0)return;
            if(!deleteArmed){deleteArmed=true;galleryCaption.text="Tap Delete photo again to remove this keepsake.";return;}
            deleteArmed=false;
            string file=photos[photoIndex];File.Delete(file);if(File.Exists(file+".json"))File.Delete(file+".json");
            photos=ListPhotos();photoIndex=Mathf.Clamp(photoIndex,0,Mathf.Max(0,photos.Length-1));ShowPhoto();
        }
        string[] ListPhotos(){string folder=PhotoFolder();return Directory.Exists(folder)?Directory.GetFiles(folder,"*.png").OrderBy(f=>f,StringComparer.Ordinal).ToArray():Array.Empty<string>();}
        string PhotoFolder()=>Path.Combine(CatMe.Onboarding.AccountStorage.ProfileFolder,"album");   // per account
        void ShowNotice(string message){if(photoNotice==null)photoNotice=MakeLabel(canvas.transform,"Photo notice",message,new Vector2(.5f,.28f),new Vector2(640,56));photoNotice.text=message;photoNotice.gameObject.SetActive(true);photoNotice.transform.SetAsLastSibling();noticeUntil=Time.unscaledTime+2.7f;StartCoroutine(HideNotice());}
        IEnumerator HideNotice(){yield return new WaitForSecondsRealtime(2.7f);if(photoNotice!=null&&Time.unscaledTime>=noticeUntil)photoNotice.gameObject.SetActive(false);}

        // Round glass icon button anchored to a screen or panel edge.
        Button Round(Transform root,string icon,string name,Vector2 anchor,Vector2 position,Action action)
        {
            var b=UiKit.IconButton(root,icon,Vector2.zero,action,name);var r=(RectTransform)b.transform;
            r.anchorMin=r.anchorMax=anchor;r.anchoredPosition=position;return b;
        }

        Button MakeButton(Transform root,string name,string label,Vector2 anchor,Vector2 position,Vector2 size,Action action,out Button button)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(root,false);
            var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=anchor;rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=size;
            var image=go.GetComponent<Image>();CatMeUIStyle.ApplySurface(image,CatMeUIStyle.CreamGlass);
            button=go.GetComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(()=>action());CatMeUIStyle.StyleButton(button,CatMeUIStyle.CreamGlass,false);
            var text=MakeLabel(go.transform,"Label",label,new Vector2(.5f,.5f),size-new Vector2(12,8));
            var textRect=text.rectTransform;textRect.pivot=new Vector2(.5f,.5f);textRect.anchoredPosition=Vector2.zero;text.color=CatMeUIStyle.Ink;
            return button;
        }
        Button MakeButton(Transform root,string name,string label,Vector2 anchor,Vector2 position,Vector2 size,Action action)
        {MakeButton(root,name,label,anchor,position,size,action,out var button);return button;}
        static Text MakeLabel(Transform parent,string name,string value,Vector2 anchor,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=anchor;r.sizeDelta=size;
            var t=go.GetComponent<Text>();t.font=CatMe.Garden.CatMeUIStyle.UiFont;t.text=value;t.fontSize=25;t.alignment=TextAnchor.MiddleCenter;t.color=CatMeUIStyle.Ink;t.raycastTarget=false;return t;
        }
        static string ZoneName(float z)=>z>120?"Mountain retreat":z>90?"Sheltered bend":z>68?"Tent clearing":z>43?"Woodland path":z>6?"Garden":"Home threshold";
        static string ZoneLabel(string zone)=>string.IsNullOrEmpty(zone)?"Outing photo":zone;
        sealed class PhotoMetadata{public string file,createdUtc,zone;}
        [Serializable] sealed class JourneySnapshot{public int schema,mapRevision;public string checkpointId;public bool active,completed,rainStarted,sheltered,retreatReached;}
    }
}
