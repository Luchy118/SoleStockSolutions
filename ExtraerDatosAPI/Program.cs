using Newtonsoft.Json.Linq;
using RestSharp;
using SoleStockSolutions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ExtraerDatosAPI
{
    internal class Program
    {
        //private static readonly string subscriptionKey = "6HKarMiepDhD8Ra2A9uR3sehLXgrJn3F9qmBGy47izbLRWGiLLqxJQQJ99AKAC5RqLJXJ3w3AAAbACOGjDmc";
        //private static readonly string endpoint = "https://api.cognitive.microsofttranslator.com/";

        static async Task Main(string[] args)
        {
            var client = new HttpClient();

            var searchQueries = new List<string>
            {
                "Jordan 1 Retro High",
                "Jordan 1 Retro Low",
                "Jordan 3 Retro",
                "Jordan 4 Retro",
                "Jordan 11 Retro",
                "Dunk SB Low",
                "Dunk Low SP",
                "Air Force SP",
                "Air Max 1 SP",
                "Air Max 95 SP",
                "Adidas Samba Wales Bonner",
                "Adidas Campus",
                "Adidas Forum Bad Bunny",
                "Adidas Handball Spezial",
                "Yeezy 350",
                "Yeezy 700",
                "Yeezy Slide",
                "Yeezy Foam RNNR",
                "New Balance Protection Pack",
                "Off-White Dunk",
                "Air Max Corteiz",
                "Air Jordan 2024",
                "Adidas Campus 00s",
                "New Balance 550",
                "New Balance 990"
            };

            foreach (var query in searchQueries)
            {
                for (int pageNo = 0; pageNo <= 2; pageNo++)
                {
                    var searchRequest = Search(query, pageNo.ToString());

                    using (var response = await client.SendAsync(searchRequest))
                    {
                        response.EnsureSuccessStatusCode();
                        var body = await response.Content.ReadAsStringAsync();
                        var jsonData = JObject.Parse(body);

                        await LoadData(jsonData);
                    }
                }
            }

        }

        private static async Task LoadData(JObject datos)
        {
            using (var db = new TFCEntities())
            {
                var hits = datos["hits"];
                List<Productos> productsToAdd = new List<Productos>();
                List<Productos_Colores> products_colorsToAdd = new List<Productos_Colores>();

                if (hits == null)
                    return;

                foreach (var hit in hits)
                {
                    List<string> excludeHits = new List<string> { "(PS)", "(TD)", "(GS)", " PS ", " TD ", " GS ", " PS", " TD", " GS", " Hydro ", "(Infants)", "(Kids)", "(I)" };
                    List<string> excludedSKUs = new List<string> { "IF1889-900" };

                    if (!excludeHits.Any(exclude => hit["title"].ToString().Contains(exclude)) && !string.IsNullOrEmpty(hit["release_date"].ToString()) && !excludedSKUs.Any(exclude => hit["sku"].ToString().Contains(exclude)))
                    {
                        var sku = hit["sku"].ToString();

                        if (db.Productos.Any(p => p.id_producto == sku))
                            continue;

                        var brand = hit["brand"].ToString();

                        if (!db.Marcas.Any(m => m.nombre_marca == brand))
                        {
                            var marca = new Marcas { nombre_marca = brand };
                            db.Marcas.Add(marca);
                            await db.SaveChangesAsync();
                        }

                        var nuevoProducto = new Productos
                        {
                            id_producto = sku,
                            id_marca = hit["title"].ToString().ToLower().Contains("yeezy") ? db.Marcas.FirstOrDefault(m => m.nombre_marca.ToLower() == "yeezy").id_marca : db.Marcas.FirstOrDefault(m => m.nombre_marca == brand).id_marca,
                            nombre = hit["title"].ToString(),
                            fecha_lanzamiento = Convert.ToDateTime(hit["release_date"]),
                            imagen = hit["image"].ToString(),
                            fecha_ultimo_uso_interno = DateTime.Now,
                            precio_medio_mercado = await ObtenerPrecioMedioMercado(Convert.ToDecimal(hit["avg_price"]))
                        };

                        productsToAdd.Add(nuevoProducto);
                    }
                }

                foreach (var producto in productsToAdd)
                {
                    if (!db.Productos.Any(p => p.id_producto == producto.id_producto))
                    {
                        Console.WriteLine($"Añadiendo producto {producto.id_producto}.");
                        db.Productos.Add(producto);
                        await db.SaveChangesAsync();
                    }
                }
            }
        }

        private static HttpRequestMessage Search(string query, string pageNo)
        {
            return new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://stockx-api.p.rapidapi.com/search?query={query}&page={pageNo}"),
                Headers =
                   {
                       { "x-rapidapi-key", "a65cdaab74msh2a979964bac5ca6p1609e9jsna66da403c24f" },
                       { "x-rapidapi-host", "stockx-api.p.rapidapi.com" },
                   },
            };
        }

        private static async Task<int> ObtenerPrecioMedioMercado(decimal precio)
        {
            try
            {
                return await ConvertCurrency(precio, "USD", "EUR");
            }
            catch (Exception)
            {
                return await ConvertCurrency2(precio, "USD", "EUR");
            }
        }

        private static async Task<int> ConvertCurrency(decimal amount, string fromCurrency, string toCurrency)
        {
            var client = new RestClient("https://openexchangerates.org/api/");
            var request = new RestRequest("latest.json", Method.Get);
            request.AddParameter("app_id", "2bf904fcc3ca41d78557adc8325f2320");
            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                var jsonData = JObject.Parse(response.Content);
                var rates = jsonData["rates"];
                var fromRate = rates[fromCurrency].Value<decimal>();
                var toRate = rates[toCurrency].Value<decimal>();
                var conversionRate = toRate / fromRate;
                return (int)(amount * conversionRate);
            }
            else
                throw new Exception("Error al obtener las tasas de cambio.");
        }

        private static async Task<int> ConvertCurrency2(decimal amount, string fromCurrency, string toCurrency)
        {
            var client = new RestClient("https://v6.exchangerate-api.com/v6/74b2ae015e9fd5a65d8ba28d/latest/");
            var request = new RestRequest($"{fromCurrency}", Method.Get);
            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                var jsonData = JObject.Parse(response.Content);
                var rates = jsonData["conversion_rates"];
                var toRate = rates[toCurrency].Value<decimal>();
                return (int)(amount * toRate);
            }
            else
            {
                throw new Exception("Error al obtener las tasas de cambio.");
            }
        }

        //public static async Task<string> TranslateText(string text, string fromLanguage, string toLanguage)
        //{
        //    string route = $"/translate?api-version=3.0&from={fromLanguage}&to={toLanguage}";
        //    object[] body = new object[] { new { Text = text } };
        //    var requestBody = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

        //    using (var client = new HttpClient())
        //    using (var request = new HttpRequestMessage())
        //    {
        //        request.Method = HttpMethod.Post;
        //        request.RequestUri = new Uri(endpoint + route);
        //        request.Content = requestBody;
        //        request.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
        //        request.Headers.Add("Ocp-Apim-Subscription-Region", "westeurope");

        //        HttpResponseMessage response = await client.SendAsync(request);
        //        string result = await response.Content.ReadAsStringAsync();
        //        JArray jsonResponse = JArray.Parse(result);
        //        return jsonResponse[0]["translations"][0]["text"].ToString();
        //    }
        //}
    }
}

