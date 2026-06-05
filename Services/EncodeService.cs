namespace base64.Services;

public static class EncodeService
{
    private const string lookupTable = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

    public static string Encode(byte[] input, int length)
    {
        int outputLength = (length + 2) / 3 * 4;
        char[] result = new char[outputLength];

        int rIndex = 0;

        int i;
        for (i = 0; i <= length - 3; i += 3)
        {
            int b1 = input[i];
            int b2 = input[i + 1];
            int b3 = input[i + 2];

            int idx1 = b1 >> 2;
            int idx2 = ((b1 & 3) << 4) | b2 >> 4;
            int idx3 = ((b2 & 15) << 2) | b3 >> 6;
            int idx4 = b3 & 63;

            result[rIndex++] = lookupTable[idx1];
            result[rIndex++] = lookupTable[idx2];
            result[rIndex++] = lookupTable[idx3];
            result[rIndex++] = lookupTable[idx4];
        }

        int left = length - i;

        switch (left)
        {
            case 1:
                {
                    int b1 = input[i];
                    int idx1 = b1 >> 2;
                    int idx2 = (b1 & 3) << 4;
                    result[rIndex++] = lookupTable[idx1];
                    result[rIndex++] = lookupTable[idx2];
                    result[rIndex++] = '=';
                    result[rIndex++] = '=';
                    break;
                }
            case 2:
                {
                    int b1 = input[i];
                    int b2 = input[i + 1];
                    int idx1 = b1 >> 2;
                    int idx2 = ((b1 & 3) << 4) | b2 >> 4;
                    int idx3 = (b2 & 15) << 2;
                    result[rIndex++] = lookupTable[idx1];
                    result[rIndex++] = lookupTable[idx2];
                    result[rIndex++] = lookupTable[idx3];
                    result[rIndex++] = '=';
                    break;
                }
        }
        return new string(result);
    }
}
