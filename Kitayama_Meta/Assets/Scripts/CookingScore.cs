using System;

namespace Kitayama.Cooking
{
    public static class CookingScore
    {
        public static int[] DefaultCookingPoints()
        {
            // 左からメーター0、1、2……10の点数。
            return new[]
            {
                0, 14, 29, 43, 57, 71,
                86, 100, 67, 33, 0
            };
        }

        public static int CookingPoints(int meter, int[] table)
        {
            if (table == null || table.Length != 11)
            {
                table = DefaultCookingPoints();
            }

            int index = Math.Max(0, Math.Min(10, meter));

            return Math.Max(0, table[index]);
        }

        public static int HeightPoints(float peakMeters)
        {
            if (float.IsNaN(peakMeters) ||
                float.IsInfinity(peakMeters))
            {
                return 0;
            }

            // メートルからセンチメートルに変換。
            // 微小な計算誤差を補正し、1cm未満を切り捨てる。
            double centimeters = Math.Floor(
                Math.Max(0d, peakMeters) * 100d + 0.0001d
            );

            return (int)Math.Min(int.MaxValue, centimeters);
        }
    }
}