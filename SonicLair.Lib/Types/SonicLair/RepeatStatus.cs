using System;

namespace SonicLair.Lib.Types.SonicLair
{
    public enum RepeatStatus
    {
        None,
        RepeatOne,
        RepeatAll
    }
    public static class RepeatStatusExtension
    {
        public static RepeatStatus Next(this RepeatStatus repeatStatus)
        {
            switch (repeatStatus)
            {
                case RepeatStatus.None:
                    return RepeatStatus.RepeatOne;
                case RepeatStatus.RepeatOne:
                    return RepeatStatus.RepeatAll;
                case RepeatStatus.RepeatAll:
                    return RepeatStatus.None;
                default:
                    throw new ArgumentOutOfRangeException(nameof(repeatStatus), repeatStatus, null);
            }
        }
    }
}