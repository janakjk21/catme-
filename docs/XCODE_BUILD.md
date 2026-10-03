# iPhone build (Xcode)

Everything needed to put WornTag on an iPhone again. The last build that worked was **build 12** on 2026-10-03: exported from Unity, signed with the free personal team, installed and launched on the iPhone 14 Pro Max.

## What you need

| Thing | Value on this Mac |
|---|---|
| Unity | 6000.6.2f1 with **iOS Build Support** (Unity Hub > Installs > Add modules) |
| Xcode | 27.0, signed in to your Apple account (Xcode > Settings > Accounts) |
| CocoaPods | `/opt/homebrew/bin/pod` (Firebase needs it) |
| Signing certificate | "Apple Development: janakjk21@gmail.com" in the keychain |
| Team | **free personal team `658662TU43`** |
| Provisioning profile | `658662TU43.com.catme.home`, **expires 2026-10-08** (free profiles last 7 days; Xcode makes a new one when you build again) |
| iPhone | Janak's iPhone 14 Pro Max, UDID `00008120-0012606036F0201E`, Developer Mode on |
| Disk | the export is several GB. The Mac's disk has little free space, so export to the external drive `/Volumes/Janak Projects/WornTag Builds/` |

App identity: bundle id `com.catme.home`, name **WornTag**, version 0.1.0, landscape only, iOS 15 or newer.

## 1. Export the Xcode project from Unity

The script `Assets/CatMe/Editor/IOSDevelopmentBuild.cs` does the export. It:
- puts exactly two scenes in the build, `GardenCompanion` and `Place_HomeGarden_r2`;
- sets the name, bundle id, landscape orientation, IL2CPP, low code stripping (glTFast needs it) and the runtime glTF shaders;
- raises the build number by one each time (12, then 13, ...);
- adds the native photo picker, Photos, PhotosUI, AuthenticationServices and GameKit to the Xcode project.

**From the open editor:** menu **CatMe > Build iOS Development Xcode Project**. Stop Play Mode first.

**From the terminal** (Unity must be closed for this project):

```sh
cd "<project folder>/catme-home-mobile"
export CATME_IOS_EXPORT_PATH="/Volumes/Janak Projects/WornTag Builds/WornTag-$(date +%F)-iOS"
export WORNTAG_APPLE_TEAM_ID=658662TU43
/Applications/Unity/Hub/Editor/6000.6.2f1/Unity.app/Contents/MacOS/Unity -batchmode -quit \
  -projectPath . -executeMethod CatMe.Editor.IOSDevelopmentBuild.Build -logFile /tmp/worntag-ios-export.log
grep "\[CatMe\]\[iOS\]" /tmp/worntag-ios-export.log
```

The log ends with `Xcode development project exported | ... | build=N | path=...`.

| Setting (before exporting) | Effect |
|---|---|
| `CATME_IOS_EXPORT_PATH` | where the Xcode project goes (default `Builds/iOS/CatMeConnectedJourney`, which is on the nearly full Mac disk) |
| `WORNTAG_APPLE_TEAM_ID` | the team Xcode signs with; without it you pick the team in Xcode each time |
| `WORNTAG_PAID_TEAM=1` | only with the paid Apple Developer Program: adds Sign in with Apple and Game Center |
| `WORNTAG_LOCAL_HTTP=1` | only for a local plain-HTTP test server |

## 2. Pods

Unity's External Dependency Manager runs `pod install` at the end of the export, creating `Pods/` and `Unity-iPhone.xcworkspace`. If either is missing:

```sh
cd "$CATME_IOS_EXPORT_PATH" && pod install
```

Always open **`Unity-iPhone.xcworkspace`**, never the `.xcodeproj`, or Firebase will not link.

## 3. Sign and build

**In Xcode:** open the workspace, select the **Unity-iPhone** target > **Signing & Capabilities** > tick *Automatically manage signing* > Team: your personal team. Choose the iPhone at the top, then Product > Run.

**From the terminal:**

```sh
cd "$CATME_IOS_EXPORT_PATH"
xcodebuild -workspace Unity-iPhone.xcworkspace -scheme Unity-iPhone -configuration Release \
  -destination 'id=00008120-0012606036F0201E' -allowProvisioningUpdates \
  DEVELOPMENT_TEAM=658662TU43 -derivedDataPath "/Volumes/Janak Projects/WornTag Builds/DerivedData" build
```

The app lands in `DerivedData/Build/Products/Release-iphoneos/WornTag.app`.

## 4. Install and open on the iPhone

Plug the iPhone in, unlock it and trust this Mac. Then:

```sh
xcrun devicectl list devices
xcrun devicectl device install app --device 00008120-0012606036F0201E "<DerivedData>/Build/Products/Release-iphoneos/WornTag.app"
xcrun devicectl device process launch --device 00008120-0012606036F0201E com.catme.home
```

First install from a free team: on the iPhone, Settings > General > VPN & Device Management > trust the developer, then open WornTag.

## 5. Check it on the phone

Run the phone checklist in `QUALITY.md`, then record the build number and what you saw in `context/STATUS.md`.

## Google sign-in in the build

- Google Sign-In is added as a **Swift package** (`Assets/CatMe/Editor/GoogleSignInDependencies.xml`), the same way Firebase is. Xcode downloads it on the first build, so the Mac needs internet.
- The build script writes `GIDClientID` into `Info.plist` from `Assets/GoogleService-Info.plist`. That file must contain `CLIENT_ID` and `REVERSED_CLIENT_ID`; get a fresh one with `npx firebase-tools apps:sdkconfig IOS 1:866984205544:ios:a21e8779f3d8717294bd5d --project catme-8a634 --out GoogleService-Info.plist`.
- The native bridge is `Assets/Plugins/iOS/CatMeGoogleSignIn.mm`.

## Problems we have hit

| Symptom | Fix |
|---|---|
| Duplicate symbols `GTMSessionFetcher` / `GoogleUtilities` | a CocoaPods copy of a Google library next to Firebase's Swift packages. Add Google libraries as Swift packages, not pods |
| "deployment target is set to 9.0 / 12.0 ... supported 15.0 to 27.0" on pods | Xcode 27 needs iOS 15 for every pod. Add a `post_install` to the `Podfile` that raises `IPHONEOS_DEPLOYMENT_TARGET` to 15.0, then `pod install` |
| Link error: duplicate symbols from `CatMeAppleSignIn` | Unity already adds `Assets/Plugins/iOS/CatMeAppleSignIn.mm`; the build script must not add it again (fixed in build 12) |
| "Unity iOS Build Support is not installed" | add the iOS module in Unity Hub |
| Export fails or the disk fills | export to the external drive with `CATME_IOS_EXPORT_PATH` |
| Firebase symbols missing at link | you opened the `.xcodeproj`; open the `.xcworkspace` (run `pod install` if it is missing) |
| "No profiles for com.catme.home" or the app stops opening after a week | the free profile expired (7 days). Rebuild in Xcode with the phone connected; Xcode makes a new one |
| Sign in with Apple says it is not set up | expected on the free team; needs the paid program (see `SETUP.md`, iPhone build and signing) |
| App opens to the old flow | the place scene was missing from the build; the script now always includes `Place_HomeGarden_r2` |
| Xcode shows a different team id (for example `H7U4993BB7`) | that is the certificate's own id, not the team. The team is `658662TU43` |

## Moving to the paid Apple Developer Program

Needed for TestFlight, the App Store, Sign in with Apple and Game Center. Then: set `WORNTAG_APPLE_TEAM_ID` to the paid team id and `WORNTAG_PAID_TEAM=1`, enable the capabilities on the App ID, and use the Release checklist in `RELEASE.md`.
