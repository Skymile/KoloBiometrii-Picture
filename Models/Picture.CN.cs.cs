﻿using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    using Minutiaes = Dictionary<MinutiaeType, List<(int X, int Y)>>;

    public enum MinutiaeType
    {
        Bifurcation,
        Crossing,
        Ending
    }

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

                    if (ptr[offset] == Zero)
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
                            MinutiaeType type = MinutiaeType.Ending;

                            ptr[offset + 0] = 250;
                            ptr[offset + 1] = 120;
                            ptr[offset + 2] = 0;

                            if (minutiaes.ContainsKey(type))
                                minutiaes[type].Add((j / 3, i));
                            else
                                minutiaes[type] = new List<(int X, int Y)>() { (j / 3, i) };

                            break;
                        case 3:
                            break;
                        case 4:
                            break;
                    }
                }

            UnlockBits(data);
            return minutiaes;
        }

        public bool IsValid(byte b) => b != Zero;
    }
}