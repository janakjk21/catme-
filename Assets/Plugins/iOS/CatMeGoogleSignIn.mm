// Native Google Sign-In for WornTag. Opens Google's sign-in sheet and hands the Google ID and access tokens to Unity,
// which signs in to Firebase with them. The client id comes from GIDClientID in Info.plist (written by the iOS build step
// from GoogleService-Info.plist), and the reversed client id is registered as a URL scheme for the return trip.
#import <Foundation/Foundation.h>
#import <GoogleSignIn/GoogleSignIn.h>
#import "UnityAppController.h"
#import "PluginBase/AppDelegateListener.h"

extern UIViewController* UnityGetGLViewController(void);
extern void UnitySendMessage(const char* obj, const char* method, const char* msg);

static NSString* catMeGoogleTarget = nil;

@interface CatMeGoogleUrlListener : NSObject<AppDelegateListener>
@end

@implementation CatMeGoogleUrlListener
+ (void)load
{
    static CatMeGoogleUrlListener* listener = nil;
    listener = [[CatMeGoogleUrlListener alloc] init];
    UnityRegisterAppDelegateListener(listener);
}
- (void)onOpenURL:(NSNotification*)notification
{
    NSURL* url = notification.userInfo[@"url"];
    if (url != nil) [GIDSignIn.sharedInstance handleURL:url];
}
@end

static void CatMeGoogleFail(NSString* text)
{
    if (catMeGoogleTarget == nil) return;
    UnitySendMessage(catMeGoogleTarget.UTF8String, "OnGoogleAuthorizationFailed", text.UTF8String);
}

extern "C" void CatMeGoogleSignIn_Open(const char* target)
{
    catMeGoogleTarget = [NSString stringWithUTF8String:target ?: ""];
    NSString* clientId = [[NSBundle mainBundle] objectForInfoDictionaryKey:@"GIDClientID"];
    if (clientId.length == 0) { CatMeGoogleFail(@"Google sign-in is not set up in this build."); return; }
    GIDSignIn.sharedInstance.configuration = [[GIDConfiguration alloc] initWithClientID:clientId];
    UIViewController* presenter = UnityGetGLViewController();
    [GIDSignIn.sharedInstance signInWithPresentingViewController:presenter completion:^(GIDSignInResult* result, NSError* error) {
        if (error != nil)
        {
            NSLog(@"[WornTag][Google] sign-in error %ld: %@", (long)error.code, error.localizedDescription);
            CatMeGoogleFail(error.code == kGIDSignInErrorCodeCanceled ? @"Sign-in cancelled." : @"Google sign-in did not finish. Check your connection and try again.");
            return;
        }
        NSString* idToken = result.user.idToken.tokenString;
        NSString* accessToken = result.user.accessToken.tokenString;
        if (idToken.length == 0) { CatMeGoogleFail(@"Google did not return a sign-in token."); return; }
        NSDictionary* payload = @{ @"idToken": idToken, @"accessToken": accessToken ?: @"" };
        NSData* json = [NSJSONSerialization dataWithJSONObject:payload options:0 error:nil];
        NSString* text = [[NSString alloc] initWithData:json encoding:NSUTF8StringEncoding];
        UnitySendMessage(catMeGoogleTarget.UTF8String, "OnGoogleAuthorization", text.UTF8String);
    }];
}
