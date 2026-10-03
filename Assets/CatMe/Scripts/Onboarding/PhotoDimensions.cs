namespace CatMe.Onboarding
{
    public static class PhotoDimensions
    {
        public static bool IsSafe(byte[] data)
        {
            if (data == null || data.Length < 24 || data.Length > 9 * 1024 * 1024) return false;
            if (data[0] == 137 && data[1] == 80 && data[2] == 78 && data[3] == 71 && data[4] == 13 && data[5] == 10 && data[6] == 26 && data[7] == 10)
                return Within(Big32(data,16), Big32(data,20));
            if (data[0] != 255 || data[1] != 216) return false;
            int offset = 2;
            while (offset + 4 < data.Length)
            {
                if (data[offset++] != 255) return false;
                while (offset < data.Length && data[offset] == 255) offset++;
                if (offset >= data.Length) return false;
                int marker = data[offset++];
                if (marker == 217 || marker == 218) return false;
                if (marker == 1 || (marker >= 208 && marker <= 215)) continue;
                if (offset + 2 > data.Length) return false;
                int length = (data[offset] << 8) | data[offset + 1];
                if (length < 2 || offset + length > data.Length) return false;
                if (marker >= 192 && marker <= 207 && marker != 196 && marker != 200 && marker != 204)
                    return length >= 8 && Within((uint)((data[offset+5] << 8) | data[offset+6]), (uint)((data[offset+3] << 8) | data[offset+4]));
                offset += length;
            }
            return false;
        }
        static uint Big32(byte[] b, int i) => ((uint)b[i] << 24) | ((uint)b[i+1] << 16) | ((uint)b[i+2] << 8) | b[i+3];
        static bool Within(uint w, uint h) => w > 0 && h > 0 && w <= 4096 && h <= 4096 && (long)w * h <= 12000000;
    }
}
