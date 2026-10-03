using System;
namespace CatMe.Onboarding
{
    // Consumes semantic gameplay events; never inspects coordinates or button labels.
    public sealed class TutorialController
    {
        readonly LocalPlayerProfile profile;
        float distance;
        bool callPending, ballPending;
        public float WalkThreshold = 2f;
        public bool Active { get; private set; }
        public TutorialProgress Progress => profile.Data.tutorial;
        public event Action Changed;
        public event Action Finished;
        public TutorialController(LocalPlayerProfile profile) { this.profile = profile; }
        public void Begin() { Active = !Progress.complete; callPending = ballPending = false; distance = 0; Changed?.Invoke(); }
        public void OwnerMoved(float amount, bool userDriven)
        {
            if (!Active || Progress.objective != "walk" || !userDriven || float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0) return;
            distance += amount;
            if (distance >= WalkThreshold) { Progress.walk = true; Advance("call"); }
        }
        public void CallRequested() { if (Active && Progress.objective == "call") callPending = true; }
        public void CatArrived() { if (Active && Progress.objective == "call" && callPending) { Progress.call = true; Advance("ball"); } }
        public void CallUnavailable() { callPending = false; }
        public void BallReleased() { if (Active && Progress.objective == "ball") ballPending = true; }
        public void BallFinished(bool resolved) { if (Active && Progress.objective == "ball" && ballPending) { ballPending = false; if (resolved) { Progress.ball = true; Complete(false); } } }
        public void SkipBall() { if (Active && Progress.objective == "ball") Complete(false); }
        public void Skip() { if (Active) Complete(true); }
        public void Suspend() { profile.Save(); }
        public void Replay()
        {
            if (profile.ReadOnly) return;
            profile.Data.tutorial = new TutorialProgress(); profile.Save(); Begin();
        }
        void Advance(string objective) { Progress.objective = objective; profile.Save(); Changed?.Invoke(); }
        void Complete(bool skipped)
        {
            Progress.complete = true; Progress.skipped = skipped; Progress.objective = "free_play";
            Active = false; profile.Save(); Changed?.Invoke(); Finished?.Invoke();
        }
    }
}
