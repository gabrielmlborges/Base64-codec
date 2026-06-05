namespace base64.Services;

public static class DecodeService
{
    private const string lookupTable = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
    private static readonly int[] revTable = new int[256];

    static DecodeService()
    {
        for (int i = 0; i < 256; i++)
        {
            revTable[i] = -1;
        }
        for (int i = 0; i < lookupTable.Length; i++)
        {
            revTable[lookupTable[i]] = i;
        }
    }

    public static byte[] Decode(string input, int length)
    {
        if (length % 4 != 0)
            throw new ArgumentException("Invalid Base64 size");

        int outputLength = length * 3 / 4;

        if (length > 0 && input[length - 1] == '=') outputLength--;
        if (length > 1 && input[length - 2] == '=') outputLength--;

        byte[] result = new byte[outputLength];
        int rIndex = 0;

        for (int i = 0; i < length; i += 4)
        {
            if (revTable[input[i]] == -1) throw new ArgumentException("Invalid char in base64");
            if (revTable[input[i + 1]] == -1) throw new ArgumentException("Invalid char in base64");
            if (i == length - 4)
            {
                if (revTable[input[i + 2]] == -1 && input[i + 2] != '=') throw new ArgumentException("Invalid char in base64");
                if (revTable[input[i + 3]] == -1 && input[i + 3] != '=') throw new ArgumentException("Invalid char in base64");
            }
            else
            {
                if (revTable[input[i + 2]] == -1) throw new ArgumentException("Invalid char in base64");
                if (revTable[input[i + 3]] == -1) throw new ArgumentException("Invalid char in base64");
            }
            int v1 = revTable[input[i]];
            int v2 = revTable[input[i + 1]];
            int v3 = revTable[input[i + 2]];
            int v4 = revTable[input[i + 3]];

            result[rIndex++] = (byte)((v1 << 2) | (v2 >> 4));

            if (rIndex < outputLength)
                result[rIndex++] = (byte)(((v2 & 15) << 4) | (v3 >> 2));

            if (rIndex < outputLength)
                result[rIndex++] = (byte)(((v3 & 3) << 6) | v4);
        }
        return result;
    }
}
