#import <AuthenticationServices/AuthenticationServices.h>
#import <CommonCrypto/CommonDigest.h>
#import <Security/Security.h>
#import <UIKit/UIKit.h>
#import "UnityInterface.h"

static NSString *catMeAppleTarget;
static NSString *catMeAppleNonce;

@interface CatMeAppleSignInDelegate : NSObject <ASAuthorizationControllerDelegate, ASAuthorizationControllerPresentationContextProviding>
@end

@implementation CatMeAppleSignInDelegate
- (ASPresentationAnchor)presentationAnchorForAuthorizationController:(ASAuthorizationController *)controller {
    return UIApplication.sharedApplication.keyWindow ?: UIApplication.sharedApplication.windows.firstObject;
}

- (void)authorizationController:(ASAuthorizationController *)controller didCompleteWithAuthorization:(ASAuthorization *)authorization {
    ASAuthorizationAppleIDCredential *identity = (ASAuthorizationAppleIDCredential *)authorization.credential;
    NSString *token = [[NSString alloc] initWithData:identity.identityToken encoding:NSUTF8StringEncoding];
    NSString *code = [[NSString alloc] initWithData:identity.authorizationCode encoding:NSUTF8StringEncoding] ?: @"";
    if (!token.length || !catMeAppleNonce.length) {
        UnitySendMessage(catMeAppleTarget.UTF8String, "OnAppleAuthorizationFailed", "Apple did not return a sign-in token.");
        return;
    }
    NSDictionary *payload = @{ @"idToken": token, @"rawNonce": catMeAppleNonce, @"authorizationCode": code };
    NSData *json = [NSJSONSerialization dataWithJSONObject:payload options:0 error:nil];
    NSString *message = [[NSString alloc] initWithData:json encoding:NSUTF8StringEncoding];
    UnitySendMessage(catMeAppleTarget.UTF8String, "OnAppleAuthorization", message.UTF8String);
    catMeAppleNonce = nil;
}

- (void)authorizationController:(ASAuthorizationController *)controller didCompleteWithError:(NSError *)error {
    // Code 1000 (unknown) is what iOS returns when the app was signed without the Sign in with Apple capability (for example a free personal team).
    NSString *text;
    switch (error.code) {
        case ASAuthorizationErrorCanceled: text = @"Sign-in cancelled."; break;
        case ASAuthorizationErrorUnknown: text = @"Sign in with Apple is not set up in this test build yet. Tap Try it free for now."; break;
        case ASAuthorizationErrorNotHandled: case ASAuthorizationErrorFailed: text = @"Apple sign-in could not complete. Check your connection and that you are signed in to iCloud."; break;
        default: text = [NSString stringWithFormat:@"Apple sign-in could not complete (code %ld).", (long)error.code];
    }
    NSLog(@"[WornTag][Apple] authorization error %ld: %@", (long)error.code, error.localizedDescription);
    UnitySendMessage(catMeAppleTarget.UTF8String, "OnAppleAuthorizationFailed", text.UTF8String);
    catMeAppleNonce = nil;
}
@end

static CatMeAppleSignInDelegate *catMeAppleDelegate;

extern "C" void CatMeAppleSignIn_Open(const char *gameObjectName) {
    dispatch_async(dispatch_get_main_queue(), ^{
        catMeAppleTarget = gameObjectName ? [NSString stringWithUTF8String:gameObjectName] : @"";
        const char alphabet[] = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        uint8_t bytes[32];
        if (SecRandomCopyBytes(kSecRandomDefault, sizeof(bytes), bytes) != errSecSuccess) {
            UnitySendMessage(catMeAppleTarget.UTF8String, "OnAppleAuthorizationFailed", "Secure sign-in is unavailable.");
            return;
        }
        char nonce[33];
        for (NSUInteger i = 0; i < 32; i++) nonce[i] = alphabet[bytes[i] % (sizeof(alphabet) - 1)];
        nonce[32] = 0;
        catMeAppleNonce = [NSString stringWithUTF8String:nonce];
        unsigned char digest[CC_SHA256_DIGEST_LENGTH];
        CC_SHA256(nonce, (CC_LONG)strlen(nonce), digest);
        NSMutableString *hashedNonce = [NSMutableString stringWithCapacity:CC_SHA256_DIGEST_LENGTH * 2];
        for (NSUInteger i = 0; i < CC_SHA256_DIGEST_LENGTH; i++) [hashedNonce appendFormat:@"%02x", digest[i]];

        ASAuthorizationAppleIDProvider *provider = [[ASAuthorizationAppleIDProvider alloc] init];
        ASAuthorizationAppleIDRequest *request = provider.createRequest;
        request.requestedScopes = @[ASAuthorizationScopeEmail, ASAuthorizationScopeFullName];
        request.nonce = hashedNonce;
        catMeAppleDelegate = [[CatMeAppleSignInDelegate alloc] init];
        ASAuthorizationController *controller = [[ASAuthorizationController alloc] initWithAuthorizationRequests:@[request]];
        controller.delegate = catMeAppleDelegate;
        controller.presentationContextProvider = catMeAppleDelegate;
        [controller performRequests];
    });
}
