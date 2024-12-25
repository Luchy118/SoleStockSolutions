using SoleStockSolutions.Filters;
using SoleStockSolutions.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SoleStockSolutions.Controllers
{
    [LoadModelosRelevantes]
    public class CheckoutController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.CurrentAction = "Checkout";
            return View();
        }

        public ActionResult Cart()
        {
            var cart = Session["Cart"] as Cart ?? new Cart();
            ViewBag.CurrentAction = "Cart";
            return View(cart);
        }

        [HttpPost]
        public ActionResult AddToCart(string productId, string productName, string imageUrl, decimal price)
        {
            var cart = Session["Cart"] as Cart ?? new Cart();
            var cartItem = cart.Items.FirstOrDefault(item => item.ProductId == productId);

            if (cartItem == null)
            {
                cart.Items.Add(new CartItem
                {
                    ProductId = productId,
                    ProductName = productName,
                    ImageUrl = imageUrl,
                    Quantity = 1,
                    Price = price
                });
            }
            else
            {
                cartItem.Quantity++;
            }

            Session["Cart"] = cart;
            return Json(cart, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult RemoveFromCart(string productId)
        {
            var cart = Session["Cart"] as Cart ?? new Cart();
            var cartItem = cart.Items.FirstOrDefault(item => item.ProductId == productId);

            if (cartItem != null)
            {
                cart.Items.Remove(cartItem);
            }

            Session["Cart"] = cart;
            return Json(cart, JsonRequestBehavior.AllowGet);
        }
    }
}