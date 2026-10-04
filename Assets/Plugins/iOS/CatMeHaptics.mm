#import <UIKit/UIKit.h>

// Light impact feedback for the CatMe haptics wrapper. style: 0 soft, 1 light, 2 medium.
// Build 18 felt nothing: each call made a new generator that was released straight after impactOccurred, and calls could
// arrive off the main thread. Now one generator per style is kept alive, used on the main thread and prepared again after
// every tap so the next one fires without delay.
static UIImpactFeedbackGenerator *catMeGenerators[3];

static UIImpactFeedbackGenerator *CatMeGenerator(int style) {
    int i = style < 0 ? 0 : (style > 2 ? 2 : style);
    if (catMeGenerators[i] == nil) {
        UIImpactFeedbackStyle s = UIImpactFeedbackStyleMedium;
        if (i == 1) s = UIImpactFeedbackStyleLight;
        if (i == 0) { if (@available(iOS 13.0, *)) s = UIImpactFeedbackStyleSoft; else s = UIImpactFeedbackStyleLight; }
        catMeGenerators[i] = [[UIImpactFeedbackGenerator alloc] initWithStyle:s];
    }
    return catMeGenerators[i];
}

extern "C" void CatMeHapticImpact(int style) {
    dispatch_block_t fire = ^{
        UIImpactFeedbackGenerator *generator = CatMeGenerator(style);
        [generator impactOccurred];
        [generator prepare];
    };
    if ([NSThread isMainThread]) fire(); else dispatch_async(dispatch_get_main_queue(), fire);
}

extern "C" void CatMeHapticPrepare() {
    dispatch_async(dispatch_get_main_queue(), ^{ for (int i = 0; i < 3; i++) [CatMeGenerator(i) prepare]; });
}
