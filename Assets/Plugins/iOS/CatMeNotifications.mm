#import <UserNotifications/UserNotifications.h>

// Local notifications for WornTag (World/LocalNotifications.cs): one reminder while a pet is being made.
static NSString *CatMeString(const char *s) { return s ? [NSString stringWithUTF8String:s] : @""; }

extern "C" void CatMeNotificationsRequest() {
    [[UNUserNotificationCenter currentNotificationCenter]
        requestAuthorizationWithOptions:(UNAuthorizationOptionAlert | UNAuthorizationOptionSound)
                      completionHandler:^(BOOL granted, NSError *error) {}];
}

extern "C" void CatMeNotificationsSchedule(const char *identifier, const char *title, const char *body, int seconds) {
    UNMutableNotificationContent *content = [[UNMutableNotificationContent alloc] init];
    content.title = CatMeString(title);
    content.body = CatMeString(body);
    content.sound = [UNNotificationSound defaultSound];
    UNTimeIntervalNotificationTrigger *trigger = [UNTimeIntervalNotificationTrigger triggerWithTimeInterval:MAX(1, seconds) repeats:NO];
    UNNotificationRequest *request = [UNNotificationRequest requestWithIdentifier:CatMeString(identifier) content:content trigger:trigger];
    [[UNUserNotificationCenter currentNotificationCenter] addNotificationRequest:request withCompletionHandler:nil];
}

extern "C" void CatMeNotificationsCancel(const char *identifier) {
    NSString *ident = CatMeString(identifier);
    [[UNUserNotificationCenter currentNotificationCenter] removePendingNotificationRequestsWithIdentifiers:@[ident]];
    [[UNUserNotificationCenter currentNotificationCenter] removeDeliveredNotificationsWithIdentifiers:@[ident]];
}
