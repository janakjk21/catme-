#import <UIKit/UIKit.h>

// Light impact feedback for the CatMe haptics wrapper. style: 0 soft, 1 light, 2 medium.
extern "C" void CatMeHapticImpact(int style) {
    UIImpactFeedbackStyle s = style == 0 ? UIImpactFeedbackStyleSoft : (style == 1 ? UIImpactFeedbackStyleLight : UIImpactFeedbackStyleMedium);
    if (@available(iOS 13.0, *)) {
        UIImpactFeedbackGenerator *generator = [[UIImpactFeedbackGenerator alloc] initWithStyle:s];
        [generator prepare];
        [generator impactOccurred];
    }
}
