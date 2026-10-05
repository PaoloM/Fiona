using Fiona.Core.Models;
using Fiona.Core.Services;

using Xunit;

namespace Fiona.Core.Tests
{
    /// <summary>
    /// Reading the server's answer. Every step of this used to be unguarded, so a server that
    /// answered with an error, with an HTML error page, or with nothing at all closed the app
    /// instead of failing one request - and it was reached from XAML bindings and from a timer that
    /// ticks every second, which is what made it fatal rather than merely annoying.
    /// </summary>
    public class ServerResponseTests
    {
        [Fact]
        public void ReadsAResult()
        {
            const string body = @"{""result"":{""count"":""2"",""albums_loop"":[
                {""id"":""1"",""album"":""One""},{""id"":""2"",""album"":""Two""}]}}";

            AlbumList albums;
            string error;
            bool ok = FionaDataService.TryReadResult(body, out albums, out error);

            Assert.True(ok);
            Assert.Null(error);
            Assert.Equal("2", albums.Count);
            Assert.Equal(2, albums.Albums.Count);
            Assert.Equal("One", albums.Albums[0].Name);
        }

        [Fact]
        public void RewritesTheServersDoubledLoopKey()
        {
            // Some responses name the collection loop_loop where the models expect item_loop. The
            // rewrite is long-standing and marked as a hack in the code; this is what it is for.
            const string body = @"{""result"":{""count"":""1"",""loop_loop"":[{""id"":""7"",""name"":""Radio 4""}]}}";

            FavoriteList favorites;
            string error;

            Assert.True(FionaDataService.TryReadResult(body, out favorites, out error));
            Assert.Single(favorites.Favorites);
            Assert.Equal("Radio 4", favorites.Favorites[0].Name);
        }

        [Fact]
        public void FailsOnAnEmptyBody()
        {
            AlbumList albums;
            string error;

            Assert.False(FionaDataService.TryReadResult("", out albums, out error));
            Assert.Null(albums);
            Assert.NotNull(error);
        }

        [Fact]
        public void FailsOnSomethingThatIsNotJson()
        {
            // A proxy or a web server error page, arriving where JSON was expected.
            const string body = "<html><head><title>502 Bad Gateway</title></head></html>";

            AlbumList albums;
            string error;

            Assert.False(FionaDataService.TryReadResult(body, out albums, out error));
            Assert.Contains("not JSON", error);
        }

        [Fact]
        public void FailsWhenTheServerReportsAnError()
        {
            const string body = @"{""error"":""unknown player""}";

            AlbumList albums;
            string error;

            Assert.False(FionaDataService.TryReadResult(body, out albums, out error));
            Assert.Contains("unknown player", error);
        }

        [Fact]
        public void FailsWhenThereIsNoResult()
        {
            // Valid JSON, no result member: this is the shape that used to throw on the dereference.
            AlbumList albums;
            string error;

            Assert.False(FionaDataService.TryReadResult(@"{""id"":1}", out albums, out error));
            Assert.Contains("no result", error);
        }

        [Fact]
        public void FailsWhenTheResultIsNull()
        {
            AlbumList albums;
            string error;

            Assert.False(FionaDataService.TryReadResult(@"{""result"":null}", out albums, out error));
            Assert.Contains("no result", error);
        }

        [Fact]
        public void FailsWhenTheResultDoesNotFitTheExpectedShape()
        {
            // The server answering a different question than the one we asked.
            AlbumList albums;
            string error;

            Assert.False(FionaDataService.TryReadResult(@"{""result"":""just a string""}", out albums, out error));
            Assert.Contains("AlbumList", error);
        }

        [Fact]
        public void AnEmptyLibraryIsAResultWithNoLoop()
        {
            // Not a failure: a server with nothing in it answers this way, and the difference
            // between that and being unreachable is the whole point of reporting failures.
            const string body = @"{""result"":{""count"":""0""}}";

            AlbumList albums;
            string error;

            Assert.True(FionaDataService.TryReadResult(body, out albums, out error));
            Assert.Equal("0", albums.Count);
            Assert.Null(albums.Albums);
        }
    }
}
