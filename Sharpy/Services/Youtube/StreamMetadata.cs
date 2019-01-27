using Newtonsoft.Json;
using System.Linq;

namespace Sharpy.Services.YouTube
{
    public class StreamFormatMetadata
    {
        [JsonProperty(PropertyName = "format")]
        public string Format { get; set; }

        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }

        [JsonProperty(PropertyName = "acodec")]
        public string Codec { get; set; }
    }

    public class StreamMetadata : IPlayable
    {
        public string Url { get; set; }

        public string Uri => Formats.First().Url;

        public string Requester { get; set; }

        public string DurationString => "Live";

        public int Speed => 48;

        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "view_count")]
        public string ViewCount { get; set; }

        [JsonProperty(PropertyName = "formats")]
        public StreamMetadata[] Formats { get; set; }

        public void OnPostPlay()
        {
        }
    }
}