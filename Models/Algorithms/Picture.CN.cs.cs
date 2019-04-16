using System;
using System.Collections.Generic;
using System.Drawing.Imaging;

namespace Models
{
    using Minutiaes = Dictionary<MinutiaeType, List<(int X, int Y)>>;

    public unsafe partial class Picture
    {
        public Minutiaes CrossingNumber()
        {
            var minutiaes = new Minutiaes();

            BitmapData data = LockBits(ImageLockMode.ReadWrite);

            byte* ptr = (byte*)data.Scan0.ToPointer();

            int stride = data.Stride;
            int height = data.Height;

            for (int i = 1; i < height - 1; i++)
                for (int j = 3; j < stride - 3; j += 3)
                {
                    int offset = i * stride + j;

                    if (ptr[offset] == 255)
                        continue;

                    int sum = 0;

                    if (IsValid(ptr[offset + 3]))
                        ++sum;

                    if (IsValid(ptr[offset - 3]))
                        ++sum;

                    for (int k = -1; k < 2; k++)
                    {
                        if (IsValid(ptr[offset + stride + k * 3]))
                            ++sum;

                        if (IsValid(ptr[offset - stride + k * 3]))
                            ++sum;
                    }

                    switch (sum)
                    {
                        case 1:
                            Extract(minutiaes, ptr, i, j, offset, MinutiaeType.Ending);
                            break;

                        case 3:
                            Extract(minutiaes, ptr, i, j, offset, MinutiaeType.Bifurcation);
                            break;

                        case 4:
                            Extract(minutiaes, ptr, i, j, offset, MinutiaeType.Crossing);
                            break;
                    }
                }

            UnlockBits(data);
            return minutiaes;
        }

        private static void Extract(
            Minutiaes minutiaes, 
            byte* ptr, 
            int i, 
            int j, 
            int offset, 
            MinutiaeType type
        )
        {
            //ptr[offset + 0] = 250;
            //ptr[offset + 1] = 120;
            //ptr[offset + 2] = 0;

            if (minutiaes.ContainsKey(type))
                minutiaes[type].Add((j / 3, i));
            else
                minutiaes[type] = new List<(int X, int Y)>() { (j / 3, i) };
        }

        public bool IsValid(byte b) => b != 255;

        public static int GetDifferences(Minutiaes first, Minutiaes second)
        {
            var values = (MinutiaeType[])Enum.GetValues(typeof(MinutiaeType));
            var count = new int[values.Length];

            for (int i = 0; i < first.Count; i++)
                if (first.TryGetValue(values[i], out var list))
                    count[i] = list.Count;
            for (int i = 0; i < first.Count; i++)
                if (second.TryGetValue(values[i], out var list))
                    count[i] -= list.Count;

            int sum = 0;
            foreach (int s in count)
                sum += s > 0 ? s : -s;
            return sum;
        }
    }
}
