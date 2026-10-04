#import <AVFoundation/AVFoundation.h>

// WornTag audio session (World/AudioSession.cs). Unity's default iOS category obeys the ring/silent switch, so a phone on
// silent played nothing at all (build 18). Playback + MixWithOthers plays on silent and lets the player's own music continue.
extern "C" void CatMeAudioSessionActivate() {
    AVAudioSession *session = [AVAudioSession sharedInstance];
    NSError *error = nil;
    [session setCategory:AVAudioSessionCategoryPlayback withOptions:AVAudioSessionCategoryOptionMixWithOthers error:&error];
    if (error) NSLog(@"[WornTag][Audio] setCategory failed: %@", error);
    error = nil;
    [session setActive:YES error:&error];
    if (error) NSLog(@"[WornTag][Audio] setActive failed: %@", error);
}
