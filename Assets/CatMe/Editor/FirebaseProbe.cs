#if UNITY_EDITOR
using System;
using System.IO;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using UnityEditor;
using UnityEngine;

namespace CatMe.Editor
{
    /// <summary>
    /// Batch probe: does the Firebase SDK start in this editor, and can it sign in, create an email account and delete it?
    /// Tasks complete on the editor update loop, so this polls from EditorApplication.update instead of blocking.
    /// Result goes to CATME_RENDER_OUT/firebase-probe.txt.
    /// </summary>
    public static class FirebaseProbe
    {
        static readonly System.Text.StringBuilder log = new System.Text.StringBuilder();
        static string outDir; static int step; static DateTime stepStart; static Task current; static FirebaseAuth auth; static FirebaseUser user; static string email;

        static void Say(string line) { log.AppendLine(line); Debug.Log("[FirebaseProbe] " + line); }
        static void Finish(int code)
        {
            File.WriteAllText(Path.Combine(outDir, "firebase-probe.txt"), log.ToString());
            EditorApplication.update -= Tick; EditorApplication.Exit(code);
        }
        static void Next(Task task) { current = task; step++; stepStart = DateTime.UtcNow; }

        public static void Run()
        {
            outDir = Environment.GetEnvironmentVariable("CATME_RENDER_OUT") ?? "/private/tmp/catme-renders";
            Directory.CreateDirectory(outDir);
            Say("platform=" + Application.platform + " unity=" + Application.unityVersion);
            step = 0; stepStart = DateTime.UtcNow;
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            try
            {
                if (step == 0) { Next(FirebaseApp.CheckAndFixDependenciesAsync()); return; }
                if (current != null && !current.IsCompleted)
                {
                    if ((DateTime.UtcNow - stepStart).TotalSeconds > 40) { Say("step " + step + " timed out"); Finish(2); }
                    return;
                }
                if (current != null && current.IsFaulted) { Say("step " + step + " failed: " + current.Exception.GetBaseException().Message); Finish(3); return; }
                switch (step)
                {
                    case 1:
                        Say("dependency status=" + ((Task<DependencyStatus>)current).Result);
                        var app = FirebaseApp.DefaultInstance; Say("app project=" + app.Options.ProjectId);
                        auth = FirebaseAuth.GetAuth(app); Next(auth.SignInAnonymouslyAsync()); break;
                    case 2:
                        user = ((Task<AuthResult>)current).Result.User; Say("anonymous sign-in OK uid=" + user.UserId.Substring(0, 6) + "...");
                        email = "probe-" + Guid.NewGuid().ToString("N").Substring(0, 8) + "@example.invalid";
                        Next(user.LinkWithCredentialAsync(EmailAuthProvider.GetCredential(email, "ProbePass12345"))); break;
                    case 3:
                        Say("email account created (create-account path works)"); Next(user.TokenAsync(false)); break;
                    case 4:
                        Say("id token length=" + ((Task<string>)current).Result.Length); Next(user.DeleteAsync()); break;
                    case 5:
                        Say("probe user deleted"); Finish(0); break;
                }
            }
            catch (Exception e) { Say("EXCEPTION " + e.GetType().Name + ": " + e.Message); Finish(1); }
        }
    }
}
#endif
