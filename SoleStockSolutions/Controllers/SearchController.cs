using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Mvc;
using HttpGetAttribute = System.Web.Http.HttpGetAttribute;
using RouteAttribute = System.Web.Http.RouteAttribute;
using RoutePrefixAttribute = System.Web.Http.RoutePrefixAttribute;

namespace SoleStockSolutions.Controllers
{
    [RoutePrefix("api/search")]
    public class SearchController : ApiController
    {
        private static readonly HttpClient client = new HttpClient();

        [HttpGet]
        [Route("")]
        public async Task<IHttpActionResult> Get(string query)
        {
            var searchRequest = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://stockx-api.p.rapidapi.com/search?query={query}"),
                Headers =
                {
                    { "x-rapidapi-key", "a65cdaab74msh2a979964bac5ca6p1609e9jsna66da403c24f" },
                    { "x-rapidapi-host", "stockx-api.p.rapidapi.com" },
                },
            };

            var response = await client.SendAsync(searchRequest);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            var jsonData = JObject.Parse(body);

            return Ok(jsonData);
        }
    }
}
