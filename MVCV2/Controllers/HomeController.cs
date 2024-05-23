using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProgramaRafaAnass.Models;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Net;

namespace ProgramaRafaAnass.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HttpClient _client;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("X-RapidAPI-Key", "3aa7a7d0f3msh6b01a7de1821d4ap1379cbjsn3cdde99e7aea");
            _client.DefaultRequestHeaders.Add("X-RapidAPI-Host", "genius-song-lyrics1.p.rapidapi.com");
        }

        public async Task<IActionResult> Index()
        {
            ProgramaRafaAnass.Models.API3.Root3 chartData = null!;

            // Primera API: Genius Albums Chart
            var requestUri = new Uri("https://genius-song-lyrics1.p.rapidapi.com/chart/songs/?time_period=day&per_page=20&page=1");
            var response = await _client.GetAsync(requestUri);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();
            chartData = JsonConvert.DeserializeObject<ProgramaRafaAnass.Models.API3.Root3>(responseBody)!;

            return View(chartData);
        }

        [HttpGet]
        public async Task<IActionResult> FetchSongUrl(string nameWithArtist)
        {
            string urlSong = string.Empty;

            var encodedNameWithArtist = Uri.EscapeDataString($"{nameWithArtist}official lyrics");
            Console.WriteLine(encodedNameWithArtist); // Debugging: Check encoded name with artist

            // Clear and set request headers
            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("X-RapidAPI-Key", "e8868d5af6msh9c028f705d0bb2fp18150bjsn16101430214b");
            _client.DefaultRequestHeaders.Add("X-RapidAPI-Host", "youtube-v2.p.rapidapi.com");

            var youtubeSearchUri = new Uri($"https://youtube-v2.p.rapidapi.com/search/?query={encodedNameWithArtist}&lang=en&order_by=this_month&country=us");

            var youtubeSearchResponse = await _client.GetAsync(youtubeSearchUri);
            if (!youtubeSearchResponse.IsSuccessStatusCode)
            {
                var responseContent = await youtubeSearchResponse.Content.ReadAsStringAsync(); // Obtener el contenido de la respuesta
                if (youtubeSearchResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogError($"El recurso solicitado no se encontró. Verifica los datos de entrada. Respuesta: {responseContent}");
                }
                else
                {
                    _logger.LogError($"Error al hacer la solicitud a la API de YouTube: {youtubeSearchResponse.StatusCode}. Respuesta: {responseContent}");
                }
                return Json(new { success = false, message = "Error al obtener los datos de las APIs de YouTube." });
            }

            var youtubeSearchBody = await youtubeSearchResponse.Content.ReadAsStringAsync();
            var ytSearchObject = JsonConvert.DeserializeObject<ProgramaRafaAnass.Models.ApiYTDownloadURL.RootYTDownload>(youtubeSearchBody);

            if (ytSearchObject?.videos != null && ytSearchObject.videos.Count > 0)
            {
                string videoId = ytSearchObject.videos[0].video_id;
                Console.WriteLine(videoId); // Debugging: Check video ID

                // Añadir retraso antes de la segunda solicitud
                await Task.Delay(1100); // Esperar 1.1 segundos (1100 ms) para no exceder el límite de 1 solicitud por segundo

                _client.DefaultRequestHeaders.Clear();
                _client.DefaultRequestHeaders.Add("X-RapidAPI-Key", "e8868d5af6msh9c028f705d0bb2fp18150bjsn16101430214b");
                _client.DefaultRequestHeaders.Add("X-RapidAPI-Host", "youtube-media-downloader.p.rapidapi.com");

                var youtubeDownloadUri = new Uri($"https://youtube-media-downloader.p.rapidapi.com/v2/video/details?videoId={videoId}");
                var youtubeDownloadResponse = await _client.GetAsync(youtubeDownloadUri);
                if (!youtubeDownloadResponse.IsSuccessStatusCode)
                {
                    var downloadResponseContent = await youtubeDownloadResponse.Content.ReadAsStringAsync(); // Obtener el contenido de la respuesta
                    _logger.LogError($"Error al hacer la solicitud a la API de descarga de YouTube: {youtubeDownloadResponse.StatusCode}. Respuesta: {downloadResponseContent}");
                    return Json(new { success = false, message = "Error al obtener los datos de las APIs de YouTube." });
                }

                var youtubeDownloadBody = await youtubeDownloadResponse.Content.ReadAsStringAsync();
                var ytDownloadObject = JsonConvert.DeserializeObject<ProgramaRafaAnass.Models.ApiYTSearch.Root>(youtubeDownloadBody);

                if (ytDownloadObject != null && ytDownloadObject.audios != null && ytDownloadObject.audios.items.Count > 0)
                {
                    urlSong = ytDownloadObject.audios.items[0].url;
                }
            }

            return Json(new { success = true, url = urlSong });
        }

        public async Task<IActionResult> Privacy()
        {
            ProgramaRafaAnass.Models.API2.Root chartData1 = null!;

            var requestUri = new Uri("https://genius-song-lyrics1.p.rapidapi.com/chart/artists/?per_page=25&page=1");
            var response = await _client.GetAsync(requestUri);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            chartData1 = JsonConvert.DeserializeObject<ProgramaRafaAnass.Models.API2.Root>(responseBody)!;

            return View(chartData1);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        
        public IActionResult GetLyrics()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetLyrics(string artist, string songTitle)
        {
            if (string.IsNullOrEmpty(artist) || string.IsNullOrEmpty(songTitle))
            {
                ModelState.AddModelError("", "Please provide both artist and song title.");
                return View();
            }

            ProgramaRafaAnass.Models.APILyrics.Root lyricsResult = null;

            try
            {
                var requestUri = new Uri($"https://api.lyrics.ovh/v1/{artist}/{songTitle}");
                var response = await _client.GetAsync(requestUri);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                lyricsResult = JsonConvert.DeserializeObject<ProgramaRafaAnass.Models.APILyrics.Root>(responseBody);
            }
            catch (HttpRequestException e)
            {
                _logger.LogError(e, "An error occurred while fetching data from the API.");
            }
            catch (JsonSerializationException e)
            {
                _logger.LogError(e, "An error occurred while deserializing the JSON response.");
            }

            return View("ResultadoLyrics", lyricsResult);
        }
    }
}
