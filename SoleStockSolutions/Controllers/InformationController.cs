using SoleStockSolutions.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace SoleStockSolutions.Controllers
{
    [LoadModelosRelevantes]
    public class InformationController : Controller
    {
        /// <summary>
        /// Acción que devuelve la vista de la página "About".
        /// </summary>
        /// <returns>Vista de la página "About".</returns>
        public ActionResult About()
        {
            ViewBag.CurrentAction = "About";
            return View();
        }

        /// <summary>
        /// Acción que devuelve la vista de la página "Contact".
        /// </summary>
        /// <returns>Vista de la página "Contact".</returns>
        public ActionResult Contact()
        {
            ViewBag.CurrentAction = "Contact";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> SendContactMessageAsync(string name, string email, string subject, string message)
        {
            try
            {
                bool isEmailValid = await VerifyEmailAsync(email);
                if (!isEmailValid)
                    return Json(new { success = false, message = "El correo electrónico no es válido." });

                var fromAddress = new MailAddress("solestocksolutions@gmail.com", "SoleStock Solutions");
                var toAddress = new MailAddress("solestocksolutions@gmail.com");
                const string fromPassword = "mbbe uhmn vcaz ktbg";

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };

                using (var mailMessage = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = $"Nombre: {name}\nCorreo: {email}\nMensaje:\n{message}"
                })
                {
                    smtp.Send(mailMessage);
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error al enviar el mensaje: " + ex.Message });
            }
        }

        public ActionResult TermsAndConditions()
        {
            return View();
        }

        public ActionResult PrivacyPolicy()
        {
            return View();
        }

        private async Task<bool> VerifyEmailAsync(string email)
        {
            string apiKey = "fa5a05fb590f01868d259e68fabfd9f7-7113c52e-a22e55b3";
            string url = $"https://api.mailgun.net/v4/address/validate?address={email}";

            using (HttpClient client = new HttpClient())
            {
                string base64String = Convert.ToBase64String(Encoding.ASCII.GetBytes($"api:{apiKey}"));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64String);

                HttpResponseMessage response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<MailgunResponse>(responseContent);
                    return result.IsValid;
                }
            }

            return false;
        }

        private class MailgunResponse
        {
            [JsonProperty("address")]
            public string Address { get; set; }

            [JsonProperty("engagement")]
            public Engagement Engagement { get; set; }

            [JsonProperty("is_disposable_address")]
            public bool IsDisposableAddress { get; set; }

            [JsonProperty("is_role_address")]
            public bool IsRoleAddress { get; set; }

            [JsonProperty("reason")]
            public List<string> Reason { get; set; }

            [JsonProperty("result")]
            public string Result { get; set; }

            [JsonProperty("risk")]
            public string Risk { get; set; }

            public bool IsValid
            {
                get
                {
                    return !Engagement.IsBot && Result == "deliverable" && !IsDisposableAddress;
                }
            }
        }

        private class Engagement
        {
            [JsonProperty("engaging")]
            public bool Engaging { get; set; }

            [JsonProperty("is_bot")]
            public bool IsBot { get; set; }
        }
    }
}