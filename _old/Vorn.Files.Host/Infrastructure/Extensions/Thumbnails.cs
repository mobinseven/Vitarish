using FFMpegCore.Extensions.SkiaSharp;
using SkiaSharp;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Vorn.Files.Host.Infrastructure.Extensions
{
    public static class Thumbnails
    {
        public const string ThumbnailContentType = "image/jpeg";
        private const int ThumbnailHeight = 200;

        /// <summary>
        /// Returns a bitmap whose height equals <paramref name="targetHeight"/>,
        /// preserving the original aspect‑ratio. If the source image is already
        /// short enough, this method returns an independent copy of it.
        /// </summary>
        /// <param name="source">The bitmap to scale.</param>
        /// <param name="targetHeight">Desired height in pixels.</param>
        private static SKBitmap ScaleBitmap(SKBitmap source, int targetHeight)
        {
            int sourceHeight = source.Height;

            // No scaling necessary – return a copy so caller can dispose independently
            if (sourceHeight <= targetHeight)
                return source.Copy();

            float scale = (float)targetHeight / sourceHeight;
            int targetWidth = (int)Math.Round(source.Width * scale);

            SKSamplingOptions sampling = new();
            return source.Resize(new SKImageInfo(targetWidth, targetHeight), sampling);
        }
        /// <summary>
        /// Creates a JPEG thumbnail that is 200 px high while keeping the aspect‑ratio
        /// of the source image. If the original is already ≤ 200 px tall, it simply
        /// re‑encodes it.
        /// </summary>
        /// <param name="original">Stream containing the source image.</param>
        /// <returns>Base‑64 encoded JPEG thumbnail.</returns>
        public static string CreateImageThumbnail(Stream original)
        {
            // Decode the input stream into a bitmap
            using SKBitmap originalImage = SKBitmap.Decode(original);

            // Scale (or copy) the bitmap to the desired height
            using SKBitmap scaled = ScaleBitmap(originalImage, ThumbnailHeight);

            // Encode to JPEG
            using SKImage image = SKImage.FromBitmap(scaled);
            using MemoryStream outputStream = new();
            image.Encode(SKEncodedImageFormat.Jpeg, 80).SaveTo(outputStream);

            return Convert.ToBase64String(outputStream.ToArray());
        }
        public static string CreateImageThumbnail(byte[] data) => CreateImageThumbnail(new MemoryStream(data));

        /// <summary>
        /// Extracts the first video frame (or a key frame, depending on FFMpeg default),
        /// scales it down to 200 px high using <see cref="ScaleBitmap"/>, and returns
        /// the JPEG thumbnail as a Base‑64 string.
        /// </summary>
        /// <param name="video">Stream containing the video file.</param>
        /// <returns>Base‑64 encoded JPEG thumbnail.</returns>
        public static async Task<string> CreateVideoThumbnail(Stream video)
        {
            // Persist the video temporarily because FFMpeg operates on files.
            string tempVideo = Path.GetTempFileName();
            await using (FileStream fs = File.Create(tempVideo))
            {
                await video.CopyToAsync(fs);
            }

            try
            {
                // Capture a frame using FFMpegCore's SkiaSharp extension.
                using SKBitmap frameBitmap = await FFMpegImage.SnapshotAsync(tempVideo);

                // Scale (or copy) the frame to the desired height.
                using SKBitmap scaled = ScaleBitmap(frameBitmap, ThumbnailHeight);

                // Encode to JPEG.
                using SKImage image = SKImage.FromBitmap(scaled);
                using MemoryStream outputStream = new();
                image.Encode(SKEncodedImageFormat.Jpeg, 80).SaveTo(outputStream);
                return Convert.ToBase64String(outputStream.ToArray());
            }
            finally
            {
                // Clean up the temporary file.
                try
                { File.Delete(tempVideo); }
                catch { /* best‑effort */ }
            }
        }

        public static Task<string> CreateVideoThumbnail(byte[] data) => CreateVideoThumbnail(new MemoryStream(data));
    }
}
