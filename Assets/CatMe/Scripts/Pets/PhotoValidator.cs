using UnityEngine;

namespace CatMe.Pets
{
    public sealed class PhotoCheck
    {
        public bool ok = true;
        public string reason;       // short machine reason
        public string message;      // friendly line shown with the good-example thumbnail
        public static PhotoCheck Pass() => new PhotoCheck();
        public static PhotoCheck Fail(string reason, string message) => new PhotoCheck { ok = false, reason = reason, message = message };
    }

    /// <summary>
    /// Pluggable photo check. This phase ships a local stand-in that only looks at size and brightness. The real "is this a cat / a dog / one
    /// person" vision check belongs on the server (it needs a vision model) and replaces this class without changing the screens.
    /// </summary>
    public interface IPhotoValidator
    {
        PhotoCheck Validate(Texture2D image, Species species, bool person);
    }

    public sealed class LocalPhotoValidator : IPhotoValidator
    {
        public PhotoCheck Validate(Texture2D image, Species species, bool person)
        {
            if (image == null) return PhotoCheck.Fail("unreadable", "That photo could not be opened. Try another one.");
            if (Mathf.Min(image.width, image.height) < 300)
                return PhotoCheck.Fail("too_small", "This photo is a bit small. A bigger one gives a better result.");
            // Very dark photos hide the markings we need.
            var pixels = image.GetPixels32(); double sum = 0; int step = Mathf.Max(1, pixels.Length / 4000), n = 0;
            for (int i = 0; i < pixels.Length; i += step) { var c = pixels[i]; sum += (c.r + c.g + c.b) / 3.0; n++; }
            if (n > 0 && sum / n < 28) return PhotoCheck.Fail("too_dark", "It's too dark to see them. Try again near a window.");
            return PhotoCheck.Pass();
        }
    }
}
