#import <AVFoundation/AVFoundation.h>
#import <PhotosUI/PhotosUI.h>
#import <UIKit/UIKit.h>
#import <Vision/Vision.h>
#import "UnityAppController.h"
#import "UnityInterface.h"

static NSString *catMePhotoPickerTarget = nil;
static NSString *catMePhotoPickerCallback = @"OnCatPhotoPicked";

// Sends the saved JPEG path, "" for cancel, or "error:<reason>" for a person photo that cannot be used.
static void CatMePhotoPickerSend(NSString *message) {
    UnitySendMessage(catMePhotoPickerTarget.UTF8String, catMePhotoPickerCallback.UTF8String, (message ?: @"").UTF8String);
}

static BOOL CatMePhotoPickerIsPerson(void) {
    return [catMePhotoPickerCallback isEqualToString:@"OnHumanPhotoPicked"];
}

// Draws the image upright (applying its orientation) at no more than maxSide pixels on the long side.
static UIImage *CatMePhotoPickerUpright(UIImage *source, CGFloat maxSide) {
    CGFloat longest = MAX(source.size.width, source.size.height);
    CGFloat factor = MIN(1.0, maxSide / MAX(longest, 1.0));
    CGSize size = CGSizeMake(floor(source.size.width * factor), floor(source.size.height * factor));
    UIGraphicsImageRendererFormat *format = [UIGraphicsImageRendererFormat defaultFormat];
    format.scale = 1; format.opaque = YES;
    UIGraphicsImageRenderer *renderer = [[UIGraphicsImageRenderer alloc] initWithSize:size format:format];
    return [renderer imageWithActions:^(UIGraphicsImageRendererContext *context) {
        [source drawInRect:CGRectMake(0, 0, size.width, size.height)];
    }];
}

static NSString *CatMePhotoPickerWriteJpeg(UIImage *image) {
    NSData *jpeg = UIImageJPEGRepresentation(image, 0.9);
    NSString *filename = [NSString stringWithFormat:@"catme-photo-%@.jpg", NSUUID.UUID.UUIDString];
    NSString *path = [NSTemporaryDirectory() stringByAppendingPathComponent:filename];
    if (!jpeg || ![jpeg writeToFile:path atomically:YES]) return nil;
    return path;
}

// A person photo is used only when it shows exactly one face. Only the head and shoulders are kept, so the rest of the
// photo (other people, the room, the body) never leaves the phone.
static NSString *CatMePhotoPickerCropToOneFace(UIImage *upright) {
    CGImageRef image = upright.CGImage;
    if (!image) return @"error:unreadable";
    VNDetectFaceRectanglesRequest *request = [[VNDetectFaceRectanglesRequest alloc] init];
    VNImageRequestHandler *handler = [[VNImageRequestHandler alloc] initWithCGImage:image options:@{}];
    NSError *error = nil;
    if (![handler performRequests:@[request] error:&error]) return @"error:unreadable";

    // Ignore tiny faces far in the background (posters, passers-by).
    NSMutableArray<VNFaceObservation *> *faces = [NSMutableArray array];
    for (VNFaceObservation *face in request.results) {
        if (face.boundingBox.size.width * face.boundingBox.size.height >= 0.004) [faces addObject:face];
    }
    if (faces.count == 0) return @"error:no_face";
    if (faces.count > 1) return @"error:many_faces";

    CGFloat width = CGImageGetWidth(image), height = CGImageGetHeight(image);
    CGRect box = faces.firstObject.boundingBox;   // normalised, origin at the bottom left
    CGFloat faceWidth = box.size.width * width, faceHeight = box.size.height * height;
    CGFloat centerX = CGRectGetMidX(box) * width;
    CGFloat faceTop = (1.0 - CGRectGetMaxY(box)) * height;
    // Hair above the face, neck and shoulders below it.
    CGFloat cropWidth = faceWidth * 2.6, cropHeight = faceHeight * 3.0;
    CGRect crop = CGRectMake(centerX - cropWidth * 0.5, faceTop - faceHeight * 0.7, cropWidth, cropHeight);
    crop = CGRectIntegral(CGRectIntersection(crop, CGRectMake(0, 0, width, height)));
    if (CGRectIsEmpty(crop) || crop.size.width < 256 || crop.size.height < 256) return @"error:face_small";

    CGImageRef cropped = CGImageCreateWithImageInRect(image, crop);
    if (!cropped) return @"error:unreadable";
    UIImage *head = [UIImage imageWithCGImage:cropped scale:1 orientation:UIImageOrientationUp];
    CGImageRelease(cropped);
    NSString *path = CatMePhotoPickerWriteJpeg(CatMePhotoPickerUpright(head, 1536.0));
    return path ?: @"error:save";
}

// Called on the main queue with a picked or captured image.
static void CatMePhotoPickerFinish(UIImage *source) {
    UIImage *upright = CatMePhotoPickerUpright(source, 3000.0);
    if (!CatMePhotoPickerIsPerson()) {
        CatMePhotoPickerSend(CatMePhotoPickerWriteJpeg(upright) ?: @"");
        return;
    }
    dispatch_async(dispatch_get_global_queue(QOS_CLASS_USER_INITIATED, 0), ^{
        NSString *result = CatMePhotoPickerCropToOneFace(upright);
        dispatch_async(dispatch_get_main_queue(), ^{ CatMePhotoPickerSend(result); });
    });
}

@interface CatMePhotoPickerDelegate : NSObject <PHPickerViewControllerDelegate>
@end

@implementation CatMePhotoPickerDelegate
- (void)picker:(PHPickerViewController *)picker didFinishPicking:(NSArray<PHPickerResult *> *)results {
    [picker dismissViewControllerAnimated:YES completion:nil];
    if (results.count == 0) {
        CatMePhotoPickerSend(@"");
        return;
    }

    NSItemProvider *provider = results.firstObject.itemProvider;
    if (![provider canLoadObjectOfClass:UIImage.class]) {
        CatMePhotoPickerSend(@"");
        return;
    }

    [provider loadObjectOfClass:UIImage.class completionHandler:^(id<NSItemProviderReading> object, NSError *error) {
        dispatch_async(dispatch_get_main_queue(), ^{
            if (error || ![object isKindOfClass:UIImage.class]) {
                CatMePhotoPickerSend(@"");
                return;
            }
            CatMePhotoPickerFinish((UIImage *)object);
        });
    }];
}
@end

@interface CatMeSelfieCameraDelegate : NSObject <UIImagePickerControllerDelegate, UINavigationControllerDelegate>
@end

@implementation CatMeSelfieCameraDelegate
- (void)imagePickerController:(UIImagePickerController *)picker didFinishPickingMediaWithInfo:(NSDictionary<UIImagePickerControllerInfoKey, id> *)info {
    UIImage *image = info[UIImagePickerControllerOriginalImage];
    [picker dismissViewControllerAnimated:YES completion:nil];
    if (![image isKindOfClass:UIImage.class]) {
        CatMePhotoPickerSend(@"");
        return;
    }
    CatMePhotoPickerFinish(image);
}

- (void)imagePickerControllerDidCancel:(UIImagePickerController *)picker {
    [picker dismissViewControllerAnimated:YES completion:nil];
    CatMePhotoPickerSend(@"");
}
@end

static CatMePhotoPickerDelegate *catMePhotoPickerDelegate;
static CatMeSelfieCameraDelegate *catMeSelfieCameraDelegate;

static void CatMePhotoPicker_PresentLibrary(void) {
    UIViewController *presenter = UnityGetGLViewController();
    if (!presenter) { CatMePhotoPickerSend(@""); return; }
    if (@available(iOS 14.0, *)) {
        PHPickerConfiguration *configuration = [[PHPickerConfiguration alloc] init];
        configuration.filter = PHPickerFilter.imagesFilter;
        configuration.selectionLimit = 1;
        PHPickerViewController *picker = [[PHPickerViewController alloc] initWithConfiguration:configuration];
        catMePhotoPickerDelegate = [[CatMePhotoPickerDelegate alloc] init];
        picker.delegate = catMePhotoPickerDelegate;
        [presenter presentViewController:picker animated:YES completion:nil];
    } else {
        CatMePhotoPickerSend(@"");
    }
}

static void CatMePhotoPicker_PresentFrontCamera(void) {
    UIViewController *presenter = UnityGetGLViewController();
    if (!presenter) { CatMePhotoPickerSend(@""); return; }
    UIImagePickerController *camera = [[UIImagePickerController alloc] init];
    camera.sourceType = UIImagePickerControllerSourceTypeCamera;
    if ([UIImagePickerController isCameraDeviceAvailable:UIImagePickerControllerCameraDeviceFront])
        camera.cameraDevice = UIImagePickerControllerCameraDeviceFront;
    camera.allowsEditing = NO;
    catMeSelfieCameraDelegate = [[CatMeSelfieCameraDelegate alloc] init];
    camera.delegate = catMeSelfieCameraDelegate;
    [presenter presentViewController:camera animated:YES completion:nil];
}

static void CatMePhotoPicker_OpenWithCallback(const char *gameObjectName, NSString *callback, BOOL useCamera) {
    NSString *target = gameObjectName == nullptr ? @"" : [NSString stringWithUTF8String:gameObjectName];
    dispatch_async(dispatch_get_main_queue(), ^{
        catMePhotoPickerTarget = target;
        catMePhotoPickerCallback = callback;
        if (!useCamera || ![UIImagePickerController isSourceTypeAvailable:UIImagePickerControllerSourceTypeCamera]) {
            CatMePhotoPicker_PresentLibrary();
            return;
        }
        // Without camera permission the library still works, and its photos get the same face check.
        AVAuthorizationStatus status = [AVCaptureDevice authorizationStatusForMediaType:AVMediaTypeVideo];
        if (status == AVAuthorizationStatusAuthorized) {
            CatMePhotoPicker_PresentFrontCamera();
        } else if (status == AVAuthorizationStatusNotDetermined) {
            [AVCaptureDevice requestAccessForMediaType:AVMediaTypeVideo completionHandler:^(BOOL granted) {
                dispatch_async(dispatch_get_main_queue(), ^{
                    if (granted) CatMePhotoPicker_PresentFrontCamera(); else CatMePhotoPicker_PresentLibrary();
                });
            }];
        } else {
            CatMePhotoPicker_PresentLibrary();
        }
    });
}

extern "C" void CatMePhotoPicker_Open(const char *gameObjectName) {
    CatMePhotoPicker_OpenWithCallback(gameObjectName, @"OnCatPhotoPicked", NO);
}

extern "C" void CatMePhotoPicker_OpenForHuman(const char *gameObjectName) {
    CatMePhotoPicker_OpenWithCallback(gameObjectName, @"OnHumanPhotoPicked", NO);
}

extern "C" void CatMePhotoPicker_TakeSelfie(const char *gameObjectName) {
    CatMePhotoPicker_OpenWithCallback(gameObjectName, @"OnHumanPhotoPicked", YES);
}
