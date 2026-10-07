using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using System.Text.RegularExpressions;

namespace SYSTEM4444
{
    internal class Program
    {
        public static SemaphoreSlim semaphore = new SemaphoreSlim(3);
        public static CancellationTokenSource source = new CancellationTokenSource();
        public static CancellationToken token = source.Token;
        public static async Task DownVideoAsync(string url)
        {
            token.ThrowIfCancellationRequested();

            var youtube = new YoutubeClient();

            var video = await youtube.Videos
                .GetAsync(url, token);

            token.ThrowIfCancellationRequested();

            Console.WriteLine($" \n  Video tapildi: {video.Title}");

            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id, token);

            var muxedStreams = streamManifest.GetMuxedStreams();
            IStreamInfo? streamInfo = null;

            if (muxedStreams.Any())
            {
                streamInfo = muxedStreams.GetWithHighestVideoQuality();
            }
            else
            {
                
                streamInfo = streamManifest.GetVideoOnlyStreams().GetWithHighestVideoQuality();
            }

            if (streamInfo == null)
            {
                Console.WriteLine(" \n Taplmadi ");
                return;
            }

            string fileName = $"{string.Join("_", video.Title.Split(Path.GetInvalidFileNameChars()))}.mp4";

            Console.WriteLine($" \n {video.Title} Video endirilir...");

            await youtube.Videos.Streams.DownloadAsync(streamInfo, fileName, cancellationToken: token);

            Console.WriteLine($" \n Endirme Tamamlandi: {fileName}");
        }

        public static async Task DownYoutubeVideoAsync(object? state)
        {
            string? url = state as string;
            if (url == null) return;

            while (!semaphore.Wait(100))
            {
                if (token.IsCancellationRequested) return;
            }

            try
            {
                await DownVideoAsync(url);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($" \n Endirme dayandirildi: {url}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                semaphore.Release();
            }
        }

        public static void StartDown()
        {
            while (!token.IsCancellationRequested)
            {
                Console.Write(" \n Youtube video link daxil et: ");
                string? url = Console.ReadLine();

                if (token.IsCancellationRequested) break;

                if (!string.IsNullOrWhiteSpace(url) &&
                    (url.StartsWith("https://www.youtube.com/watch?v") ||
                     url.StartsWith("https://youtu.be")))
                {

                    Thread downloadThread = new Thread(async () => await DownYoutubeVideoAsync(url));
                    downloadThread.Start();
                }
                else
                {
                    Console.WriteLine(" \n Link etibarli deyil");
                }
            }
        }

        static async Task Main(string[] args)
        {
            token = source.Token;

            Thread exitThread = new Thread(() =>
            {
                while (true)
                {
                    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                    {
                        source.Cancel();
                        break;
                    }
                    Thread.Sleep(50);
                }
            });
            exitThread.IsBackground = true;
            exitThread.Start();

            Thread startDownThread = new Thread(StartDown);
            startDownThread.Start();

            while (!token.IsCancellationRequested)
            {
                Thread.Sleep(100);
            }
        }
    }
}