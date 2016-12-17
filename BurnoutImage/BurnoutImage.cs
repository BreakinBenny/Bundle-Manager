﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace BurnoutImage
{
    public static class GameImage
    {
        private static bool Matches(this byte[] self, byte[] other)
        {
            if (self == null || other == null)
                return false;
            if (self.Length != other.Length)
                return false;

            for (int i = 0; i < self.Length; i++)
            {
                if (self[i] != other[i])
                    return false;
            }

            return true;
        }

        public struct ImageInfo
        {
            public readonly byte[] Data;
            public readonly byte[] ExtraData;

            public ImageInfo(byte[] Data, byte[] ExtraData)
            {
                this.Data = Data;
                this.ExtraData = ExtraData;
            }
        }


        /*Bitmap bitmap = new Bitmap(image);
        int width = bitmap.Width;
        int height = bitmap.Height;

        MemoryStream mspixels = new MemoryStream();

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                Color pixel = bitmap.GetPixel(j, i);
                mspixels.WriteByte(pixel.R);
                mspixels.WriteByte(pixel.G);
                mspixels.WriteByte(pixel.B);
                mspixels.WriteByte(pixel.A);
            }
        }

        byte[] rgba = mspixels.ToArray();*/

        public static ImageInfo SetImage(Image image, DXTCompression compression)
        {
            int width = image.Width;
            int height = image.Height;
            
            byte[] ExtraData = ImageUtil.CompressImage(image/*, width, height*/, compression);

            MemoryStream msx = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(msx);

            bw.Write((int)0);
            bw.Write((int)0);
            bw.Write((int)0);
            bw.Write((int)1);

            bw.Write(Encoding.ASCII.GetBytes(compression.ToString()));
            bw.Write((short)width);
            bw.Write((short)height);
            bw.Write((int)0x15);
            bw.Write((int)0);

            bw.Flush();

            byte[] Data = msx.ToArray();

            bw.Close();

            return new ImageInfo(Data, ExtraData);
        }

        public static Image GetImagePC(byte[] data, byte[] extraData)
        {
            if (extraData != null && data.Length == 32)
            {
                try
                {
                    MemoryStream ms = new MemoryStream(data);
                    BinaryReader br = new BinaryReader(ms);
                    br.BaseStream.Seek(0x10, SeekOrigin.Begin);
                    CompressionType type = CompressionType.UNKNOWN;
                    byte[] compression = br.ReadBytes(4);
                    string compressionString = Encoding.ASCII.GetString(compression);
                    if (compression.Matches(new byte[] { 0x15, 0x00, 0x00, 0x00 }))
                    {
                        type = CompressionType.BGRA;
                    }
                    else if (compression.Matches(new byte[] { 0xFF, 0x00, 0x00, 0x00 }))
                    {
                        type = CompressionType.ARGB;
                    }
                    else if (compressionString.StartsWith("DXT"))
                    {
                        switch (compressionString[3])
                        {
                            case '1':
                                type = CompressionType.DXT1;
                                break;
                            case '3':
                                type = CompressionType.DXT3;
                                break;
                            case '5':
                                type = CompressionType.DXT5;
                                break;
                        }
                    }

                    int width = br.ReadInt16();
                    int height = br.ReadInt16();
                    br.Close();

                    byte[] pixels = extraData;

                    if (type == CompressionType.DXT1)
                    {
                        pixels = ImageUtil.DecompressImage(pixels, width, height, DXTCompression.DXT1);
                    }
                    else if (type == CompressionType.DXT3)
                    {
                        pixels = ImageUtil.DecompressImage(pixels, width, height, DXTCompression.DXT3);
                    }
                    else if (type == CompressionType.DXT5)
                    {
                        pixels = ImageUtil.DecompressImage(pixels, width, height, DXTCompression.DXT5);
                    }

                    //ms = new MemoryStream(pixels);
                    //br = new BinaryReader(ms);

                    Bitmap bitmap = new Bitmap(width, height);
                    //bitmap.
                    int index = 0;
                    for (int i = 0; i < height; i++)
                    {
                        for (int j = 0; j < width; j++)
                        {
                            byte red;
                            byte green;
                            byte blue;
                            byte alpha;
                            if (type == CompressionType.BGRA)
                            {
                                //blue = br.ReadByte();
                                //green = br.ReadByte();
                                //red = br.ReadByte();
                                //alpha = br.ReadByte();
                                blue = pixels[index + 0];
                                green = pixels[index + 1];
                                red = pixels[index + 2];
                                alpha = pixels[index + 3];
                            }
                            else// if (type == CompressionType.ARGB)
                            {
                                //alpha = br.ReadByte();
                                //red = br.ReadByte();
                                //green = br.ReadByte();
                                //blue = br.ReadByte();

                                alpha = pixels[index + 0];
                                red = pixels[index + 1];
                                green = pixels[index + 2];
                                blue = pixels[index + 3];
                            }
                            //else
                            //{
                                //red = br.ReadByte();
                                //green = br.ReadByte();
                                //blue = br.ReadByte();
                                //alpha = br.ReadByte();

                                /*red = pixels[index + 0];
                                alpha = pixels[index + 1];
                                blue = pixels[index + 2];
                                green = pixels[index + 3];*/

                                //red = pixels[index + 0];
                                //green = pixels[index + 1];
                               // blue = pixels[index + 2];
                              //  alpha = pixels[index + 3];
                            //}

                            Color color = Color.FromArgb(alpha, red, green, blue);
                            bitmap.SetPixel(j, i, color);
                            index += 4;
                        }
                    }

                    //br.Close();
                    return bitmap;
                }
                catch
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public static Image GetImagePS3(byte[] data, byte[] extraData)
        {
            if (extraData != null && data.Length == 48)
            {
                try
                {
                    MemoryStream ms = new MemoryStream(data);
                    BinaryReader br = new BinaryReader(ms);
                    //br.BaseStream.Seek(0x10, SeekOrigin.Begin);
                    byte compression = br.ReadByte();
                    byte[] unknown1 = br.ReadBytes(3);
                    CompressionType type = CompressionType.UNKNOWN;
                    if (compression == 0x85)
                    {
                        type = CompressionType.ARGB;
                    }
                    else if (compression == 0x86)
                    {
                        type = CompressionType.DXT1;
                    }
                    else if (compression == 0x88)
                    {
                        type = CompressionType.DXT5;
                    }
                    int unknown2 = Util.ReverseBytes(br.ReadInt32());
                    int width = Util.ReverseBytes(br.ReadInt16());
                    int height = Util.ReverseBytes(br.ReadInt16());
                    /*byte[] compression = br.ReadBytes(4);
                    string compressionString = Encoding.ASCII.GetString(compression);
                    if (compression.Matches(new byte[] { 0x15, 0x00, 0x00, 0x00 }))
                    {
                        type = CompressionType.BGRA;
                    }
                    else if (compression.Matches(new byte[] { 0xFF, 0x00, 0x00, 0x00 }))
                    {
                        type = CompressionType.ARGB;
                    }
                    else if (compressionString.StartsWith("DXT"))
                    {
                        switch (compressionString[3])
                        {
                            case '1':
                                type = CompressionType.DXT1;
                                break;
                            case '3':
                                type = CompressionType.DXT3;
                                break;
                            case '5':
                                type = CompressionType.DXT5;
                                break;
                        }
                    }

                    int width = br.ReadInt16();
                    int height = br.ReadInt16();*/
                    br.Close();

                    byte[] pixels = extraData;

                    if (type == CompressionType.DXT1)
                    {
                        pixels = ImageUtil.DecompressImage(pixels, width, height, DXTCompression.DXT1);
                    }
                    else if (type == CompressionType.DXT3)
                    {
                        pixels = ImageUtil.DecompressImage(pixels, width, height, DXTCompression.DXT3);
                    }
                    else if (type == CompressionType.DXT5)
                    {
                        pixels = ImageUtil.DecompressImage(pixels, width, height, DXTCompression.DXT5);
                    }

                    //ms = new MemoryStream(pixels);
                    //br = new BinaryReader(ms);

                    Bitmap bitmap = new Bitmap(width, height);
                    //bitmap.
                    int index = 0;
                    for (int i = 0; i < height; i++)
                    {
                        for (int j = 0; j < width; j++)
                        {
                            byte red;
                            byte green;
                            byte blue;
                            byte alpha;
                            if (type == CompressionType.BGRA)
                            {
                                //blue = br.ReadByte();
                                //green = br.ReadByte();
                                //red = br.ReadByte();
                                //alpha = br.ReadByte();
                                blue = pixels[index + 0];
                                green = pixels[index + 1];
                                red = pixels[index + 2];
                                alpha = pixels[index + 3];
                            }
                            else //if (type == CompressionType.ARGB)
                            {
                                //alpha = br.ReadByte();
                                //red = br.ReadByte();
                                //green = br.ReadByte();
                                //blue = br.ReadByte();

                                alpha = pixels[index + 0];
                                red = pixels[index + 1];
                                green = pixels[index + 2];
                                blue = pixels[index + 3];
                            }
                            //else
                            //{
                                //red = br.ReadByte();
                                //green = br.ReadByte();
                                //blue = br.ReadByte();
                                //alpha = br.ReadByte();

                               // red = pixels[index + 0];
                                //green = pixels[index + 1];
                               // blue = pixels[index + 2];
                               // alpha = pixels[index + 3];
                            //}

                            Color color = Color.FromArgb(alpha, red, green, blue);
                            bitmap.SetPixel(j, i, color);
                            index += 4;
                        }
                    }

                    //br.Close();
                    return bitmap;

                }
                catch
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public enum CompressionType
        {
            RGBA,
            ARGB,
            BGRA,
            DXT1,
            DXT3,
            DXT5,
            UNKNOWN
        }
    }
}
