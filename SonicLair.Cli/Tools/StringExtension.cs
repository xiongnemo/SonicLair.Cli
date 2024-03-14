namespace SonicLair.Cli.Tools
{
    public static class StringExtension
    {
        public static int StandardizedStringLength(this string input)
        {
            // each CJK rune is 2 letter width when using a monospaced font
            int length = 0;
            foreach (var c in input)
            {
                length += Rune.IsWideChar(c) ? 2 : 1;
            }
            return length;
        }
        public static string RunePadRight(this string stringToPad, int totalWidth, char paddingChar = ' ')
        {
            int standardizedStringLength = stringToPad.StandardizedStringLength();
            if (standardizedStringLength >= totalWidth)
            {
                return stringToPad;
            }
            return stringToPad.PadRight(totalWidth - (standardizedStringLength - stringToPad.Length), paddingChar);
        }
        public static string RunePadLeft(this string stringToPad, int totalWidth, char paddingChar = ' ')
        {
            int standardizedStringLength = stringToPad.StandardizedStringLength();
            if (standardizedStringLength >= totalWidth)
            {
                return stringToPad;
            }
            return stringToPad.PadLeft(totalWidth - (standardizedStringLength - stringToPad.Length), paddingChar);
        }
    }
}