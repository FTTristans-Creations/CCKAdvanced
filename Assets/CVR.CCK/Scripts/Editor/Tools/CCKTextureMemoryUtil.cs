using UnityEngine;

namespace CVR.CCKEditor.Tools
{
    public static class CCKTextureMemoryUtil
    {
        public static long CalculateVRAMUsageBytes(Texture2D tex)
        {
            if (!tex) return 0;

            int bpp = GetBitsPerPixel(tex.format);
            int width = tex.width;
            int height = tex.height;
            int mipCount = tex.mipmapCount;

            long totalBytes = 0;
            for (int i = 0; i < mipCount; i++)
            {
                totalBytes += Mathf.Max(1, width) * Mathf.Max(1, height) * bpp / 8;
                width /= 2;
                height /= 2;
            }

            return totalBytes;
        }

        // https://docs.unity3d.com/6000.1/Documentation/ScriptReference/TextureFormat.html
        private static int GetBitsPerPixel(TextureFormat format)
        {
        #pragma warning disable CS0618 // Type or member is obsolete
            switch (format)
            {
                #region Single Channel Formats
                
                /// Alpha-only texture format, 8 bit integer.
                case TextureFormat.Alpha8: return 8;
                
                /// Single channel (R) texture format, 8-bits unsigned integer.
                case TextureFormat.R8: return 8;
                
                /// Single channel (R) texture format, 16-bits unsigned integer.
                case TextureFormat.R16: return 16;
                
                /// Scalar (R) texture format, 32 bit floating point.
                case TextureFormat.RFloat: return 32;
                
                /// Scalar (R) texture format, 16 bit floating point.
                case TextureFormat.RHalf: return 16;
                
                /// BC4 format compresses textures to 4 bits per pixel, keeping only the red color channel.
                case TextureFormat.BC4: return 4;
                
                /// ETC2 / EAC (GL ES 3.0) 4 bits/pixel compressed unsigned single-channel texture format.
                case TextureFormat.EAC_R: return 4;
                
                /// ETC2 / EAC (GL ES 3.0) 4 bits/pixel compressed signed single-channel texture format.
                case TextureFormat.EAC_R_SIGNED: return 4;
                
                #endregion Single Channel Formats

                #region Two Channel Formats
                
                /// Two channel (RG) texture format, 16-bits unsigned integer per channel.
                case TextureFormat.RG32: return 32;
                
                /// Two channel (RG) texture format, 8-bits unsigned integer per channel.
                case TextureFormat.RG16: return 16;
                
                /// Two color (RG) texture format, 32 bit floating point per channel.
                case TextureFormat.RGFloat: return 64;
                
                /// Two color (RG) texture format, 16 bit floating point per channel.
                case TextureFormat.RGHalf: return 32;
                
                /// BC5 format compresses textures to 8 bits per pixel, keeping only the red and green color channels.
                case TextureFormat.BC5: return 8;
                
                /// ETC2 / EAC (GL ES 3.0) 8 bits/pixel compressed unsigned dual-channel (RG) texture format.
                case TextureFormat.EAC_RG: return 8;
                
                /// ETC2 / EAC (GL ES 3.0) 8 bits/pixel compressed signed dual-channel (RG) texture format.
                case TextureFormat.EAC_RG_SIGNED: return 8;
                
                #endregion Two Channel Formats
                
                # region RGB Formats
                
                /// A 16 bit color texture format.
                case TextureFormat.RGB565: return 16;
                
                /// Three channel (RGB) texture format, 8-bits unsigned integer per channel.
                /// Note that there are almost no GPUs that support this format natively,
                /// so at texture load time it is converted into an RGBA32 format.
                // case TextureFormat.RGB24: return 24; 
                case TextureFormat.RGB24: return /*24*/ 32; // same as RGBA32
                
                /// Three channel (RGB) texture format, 16-bits unsigned integer per channel.
                case TextureFormat.RGB48: return 48;
                
                /// RGB HDR format, with 9 bit mantissa per channel and a 5 bit shared exponent.
                case TextureFormat.RGB9e5Float: return 32;
                
                /// BC6H format compresses RGB HDR textures to 8 bits per pixel.
                /// When loading BC6H textures on a platform that doesn't support it,
                /// the texture decompresses into RGBAHalf format (64 bits per pixel) at load time.
                case TextureFormat.BC6H: return 8; // We are DX11-class PC hardware, so we can use BC6H natively
                
                /// DXT1 (BC1) format compresses textures to 4 bits per pixel.
                case TextureFormat.DXT1: return 4;
                
                /// ETC (GLES2.0) 4 bits/pixel compressed RGB texture format.
                case TextureFormat.ETC_RGB4: return 4;
                
                /// ETC2 (GL ES 3.0) 4 bits/pixel compressed RGB texture format.
                case TextureFormat.ETC2_RGB: return 4;
                
                /// No documetation page
                case TextureFormat.ETC_RGB4_3DS: return 4;
                
                /// No documentation page
                case TextureFormat.PVRTC_RGB2: return 2;
                
                /// No documentation page
                case TextureFormat.PVRTC_RGB4: return 4;
                
                /// The DXT1Crunched format is similar to the DXT1 format but with additional JPEG-like lossy
                /// compression for storage size reduction. Textures are transcoded into the DXT1 format at load time.
                case TextureFormat.DXT1Crunched: return 4; // Variable compression, but base format is 4
                
                /// The ETC_RGB4Crunched format is similar to the ETC_RGB4 format but with additional JPEG-like
                /// lossy compression for storage size reduction. Textures are transcoded into the ETC_RGB4 format at load time.
                case TextureFormat.ETC_RGB4Crunched: return 4; // Variable compression, but base format is 4
                
                #endregion RGB Formats

                #region RGBA Formats
                
                /// A 16 bits/pixel texture format. Texture stores color with an alpha channel.
                case TextureFormat.ARGB4444: return 16;
                
                /// Color and alpha texture format, 4 bit per channel.
                case TextureFormat.RGBA4444: return 16;
                
                /// Color with alpha texture format, 8-bits per channel.
                case TextureFormat.ARGB32: return 32;
                
                /// Four channel (RGBA) texture format, 8-bits unsigned integer per channel.
                case TextureFormat.RGBA32: return 32;
                
                /// Color with alpha texture format, 8-bits per channel.
                case TextureFormat.BGRA32: return 32;
                
                /// Four channel (RGBA) texture format, 16-bits unsigned integer per channel.
                case TextureFormat.RGBA64: return 64;
                
                /// RGB color and alpha texture format, 32-bit floats per channel.
                case TextureFormat.RGBAFloat: return 128;
                
                /// RGB color and alpha texture format, 16 bit floating point per channel.
                case TextureFormat.RGBAHalf: return 64;
                
                /// ETC2 (GL ES 3.0) 4 bits/pixel RGB+1-bit alpha texture format.
                case TextureFormat.ETC2_RGBA1: return 4;
                
                /// ETC2 (GL ES 3.0) 8 bits/pixel compressed RGBA texture format.
                case TextureFormat.ETC2_RGBA8: return 8;
                
                // No documentation page
                case TextureFormat.ETC_RGBA8_3DS: return 8;
                
                // No documentation page
                case TextureFormat.PVRTC_RGBA2: return 2;
                
                // No documentation page
                case TextureFormat.PVRTC_RGBA4: return 4;
                
                /// DXT5 (BC3) format compresses textures to 8 bits per pixel.
                case TextureFormat.DXT5: return 8;
                
                /// The DXT5Crunched format is similar to the DXT5 format but with additional JPEG-like lossy
                /// compression for storage size reduction. Textures are transcoded into the DXT5 format at load time.
                case TextureFormat.DXT5Crunched: return 8; // Variable compression, but base format is 8
                
                /// The ETC2_RGBA8Crunched format is similar to the ETC2_RGBA8 format but with additional JPEG-like
                /// lossy compression for storage size reduction. Textures are transcoded into the ETC2_RGBA8 format at load time.
                case TextureFormat.ETC2_RGBA8Crunched: return 8; // Variable compression, but base format is 8
                
                /// BC7 format compresses textures to 8 bits per pixel.
                case TextureFormat.BC7: return 8;
                
                #endregion RGBA Formats

                #region ASTC Formats
                
                // ASTC formats - Fixed calculations based on 128-bit blocks
                // Formula: Math.Ceil(128 bits / (block_width * block_height))
                // I am ceiling because I don't actually know how to handle the fractional bits.
                
                /// ASTC (4x4 pixel block in 128 bits) compressed RGB(A) texture format.
                case TextureFormat.ASTC_4x4:
                    
                /// No documentation page
                case TextureFormat.ASTC_RGBA_4x4:
                    
                /// ASTC (4x4 pixel block in 128 bits) compressed RGB(A) HDR texture format.
                case TextureFormat.ASTC_HDR_4x4:
                    return 8; // 128 / (4 * 4)
                    
                /// ASTC (5x5 pixel block in 128 bits) compressed RGB(A) texture format.
                case TextureFormat.ASTC_5x5:
                    
                /// No documentation page
                case TextureFormat.ASTC_RGBA_5x5:
                    
                /// ASTC (5x5 pixel block in 128 bits) compressed RGB(A) HDR texture format.
                case TextureFormat.ASTC_HDR_5x5: 
                    return 6; // 128 / (5 * 5) = 5.12 -> 6
                    
                /// ASTC (6x6 pixel block in 128 bits) compressed RGB(A) texture format.
                case TextureFormat.ASTC_6x6:
                    
                /// No documentation page
                case TextureFormat.ASTC_RGBA_6x6:
                    
                /// ASTC (6x6 pixel block in 128 bits) compressed RGB(A) HDR texture format.
                case TextureFormat.ASTC_HDR_6x6: 
                    return 4; // 128 / (6 * 6) = 3.56 -> 4
                    
                /// ASTC (8x8 pixel block in 128 bits) compressed RGB(A) texture format.
                case TextureFormat.ASTC_8x8:
                    
                /// No documentation page
                case TextureFormat.ASTC_RGBA_8x8:
                    
                /// ASTC (8x8 pixel block in 128 bits) compressed RGB(A) texture format.
                case TextureFormat.ASTC_HDR_8x8: 
                    return 2; // 128 / (8 * 8)
                    
                /// ASTC (10x10 pixel block in 128 bits) compressed RGB(A) texture format.
                case TextureFormat.ASTC_10x10:
                    
                /// No documentation page
                case TextureFormat.ASTC_RGBA_10x10:
                    
                /// ASTC (10x10 pixel block in 128 bits) compressed RGB(A) HDR texture format.
                case TextureFormat.ASTC_HDR_10x10: 
                    return 2; // 128 / (10 * 10) = 1.28 -> 2
                    
                /// ASTC (12x12 pixel block in 128 bits) compressed RGB(A) texture format.
                case TextureFormat.ASTC_12x12:
                    
                /// No documentation page
                case TextureFormat.ASTC_RGBA_12x12:
                    
                /// ASTC (12x12 pixel block in 128 bits) compressed RGB(A) HDR texture format.
                case TextureFormat.ASTC_HDR_12x12: 
                    return 1; // 128 / (12 * 12) = 0.89 -> 1
                
                #endregion ASTC Formats
                
                #region Fucked Formats
                
                /// Currently, this texture format is only useful for native code plugins as there is no
                /// support for texture importing or pixel access for this format.
                case TextureFormat.YUY2: return 16; // Google said 16, thanks Unity not including it
                
                /// ATC_RGB4
                /// No documentation page
                case (TextureFormat)(-127): return 4; // Obsolete with error, need to check like this
                
                #endregion Fucked Formats
                
                default: 
                    return int.MaxValue; // I fucked up, make it obvious
            }
        #pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}