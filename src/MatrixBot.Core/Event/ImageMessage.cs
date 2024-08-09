using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MatrixBot.Core
{
    public class ImageMessage : Message
    {
        public ImageMessage(MatrixMedia media, decimal? width = null, decimal? height = null)
        {
            MsgType = "m.image";
            Body = media.FileName;
            Url = media.MatrixUrl;
            Info = new ImageInfo
            {
                Size = media.FileSize,
                MimeType = media.MediaType,
                ThumbnailUrl = media.MatrixUrl
            };
            if (width.HasValue)
            {
                Info.Width = width.Value;
            }
            if (height.HasValue)
            {
                Info.Height = height.Value;
            }
        }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("info")]
        public ImageInfo? Info { get; set; }
        public class ImageInfo
        {
            [JsonPropertyName("size")]
            public long Size { get; set; }
            [JsonPropertyName("mimetype")]
            public string? MimeType { get; set; }
            [JsonPropertyName("w")]
            public decimal Width { get; set; }
            [JsonPropertyName("h")]
            public decimal Height { get; set; }
            [JsonPropertyName("thumbnail_url")]
            public string? ThumbnailUrl { get; set; }
        }
    }
}
