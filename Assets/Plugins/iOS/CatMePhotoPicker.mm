#import <PhotosUI/PhotosUI.h>
#import <UIKit/UIKit.h>
#import "UnityAppController.h"
#import "UnityInterface.h"

static NSString *catMePhotoPickerTarget = nil;
static NSString *catMePhotoPickerCallback = @"OnCatPhotoPicked";

@interface CatMePhotoPickerDelegate : NSObject <PHPickerViewControllerDelegate>
@end

@implementation CatMePhotoPickerDelegate
- (void)picker:(PHPickerViewController *)picker didFinishPicking:(NSArray<PHPickerResult *> *)results {
    [picker dismissViewControllerAnimated:YES completion:nil];
    if (results.count == 0) {
        UnitySendMessage(catMePhotoPickerTarget.UTF8String, catMePhotoPickerCallback.UTF8String, "");
        return;
    }

    NSItemProvider *provider = results.firstObject.itemProvider;
    if (![provider canLoadObjectOfClass:UIImage.class]) {
        UnitySendMessage(catMePhotoPickerTarget.UTF8String, catMePhotoPickerCallback.UTF8String, "");
        return;
    }

    [provider loadObjectOfClass:UIImage.class completionHandler:^(id<NSItemProviderReading> object, NSError *error) {
        dispatch_async(dispatch_get_main_queue(), ^{
            if (error || ![object isKindOfClass:UIImage.class]) {
                UnitySendMessage(catMePhotoPickerTarget.UTF8String, catMePhotoPickerCallback.UTF8String, "");
                return;
            }
            NSData *jpeg = UIImageJPEGRepresentation((UIImage *)object, 0.9);
            NSString *filename = [NSString stringWithFormat:@"catme-photo-%@.jpg", NSUUID.UUID.UUIDString];
            NSString *path = [NSTemporaryDirectory() stringByAppendingPathComponent:filename];
            if (!jpeg || ![jpeg writeToFile:path atomically:YES]) {
                UnitySendMessage(catMePhotoPickerTarget.UTF8String, "OnCatPhotoPicked", "");
                return;
            }
            UnitySendMessage(catMePhotoPickerTarget.UTF8String, catMePhotoPickerCallback.UTF8String, path.UTF8String);
        });
    }];
}
@end

static CatMePhotoPickerDelegate *catMePhotoPickerDelegate;

static void CatMePhotoPicker_OpenWithCallback(const char *gameObjectName, NSString *callback) {
    NSString *target = gameObjectName == nullptr ? @"" : [NSString stringWithUTF8String:gameObjectName];
    dispatch_async(dispatch_get_main_queue(), ^{
        catMePhotoPickerTarget = target;
        catMePhotoPickerCallback = callback;
        UIViewController *presenter = UnityGetGLViewController();
        if (!presenter) return;
        if (@available(iOS 14.0, *)) {
            PHPickerConfiguration *configuration = [[PHPickerConfiguration alloc] init];
            configuration.filter = PHPickerFilter.imagesFilter;
            configuration.selectionLimit = 1;
            PHPickerViewController *picker = [[PHPickerViewController alloc] initWithConfiguration:configuration];
            catMePhotoPickerDelegate = [[CatMePhotoPickerDelegate alloc] init];
            picker.delegate = catMePhotoPickerDelegate;
            [presenter presentViewController:picker animated:YES completion:nil];
        } else {
            UnitySendMessage(catMePhotoPickerTarget.UTF8String, catMePhotoPickerCallback.UTF8String, "");
        }
    });
}

extern "C" void CatMePhotoPicker_Open(const char *gameObjectName) {
    CatMePhotoPicker_OpenWithCallback(gameObjectName, @"OnCatPhotoPicked");
}

extern "C" void CatMePhotoPicker_OpenForHuman(const char *gameObjectName) {
    CatMePhotoPicker_OpenWithCallback(gameObjectName, @"OnHumanPhotoPicked");
}
