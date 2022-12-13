namespace Fiona.Core.Helpers
{
    public class FionaCommand
    {
        // commands
        public const string ServerStatus = "serverstatus";
        public const string Albums = "albums";
        public const string Artists = "artists";
        public const string Songs = "songs";
        public const string Players = "players";
        public const string Status = "status";
        public const string Genres = "genres";
        public const string Favorites = "favorites";

        // misc
        public const string MaxItems = "100000";

        // tags
        public const string AlbumTags = "tags:lyjtiqwaaaSSSsEX";
        public const string ArtistTags = "tags:sE";
        public const string GenreTags = "tags:s";
        public const string TrackTags = "tags:aABcCdefgGiIjJkKlLmMnNopPDUqrROstTuvwxXyY";
        public const string PlayerSelectTags = "tags:aABcCdefgGiIjJkKlLmMnNopPDUqrROstTuvwxXyY";

        // select
        public const string AlbumSelect = "album_id:{0}";
        public const string ArtistSelect = "artist_id:{0}";
    }
}
