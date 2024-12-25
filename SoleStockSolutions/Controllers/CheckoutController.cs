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
        private List<CartItem> GetCartItems()
        {
            var cart = Session["Cart"] as Cart ?? new Cart();
            var cartItems = cart.Items.ToList();
            return cartItems;
        }

        public ActionResult Index()
        {
            ViewBag.CurrentAction = "Checkout"; 

            var cartItems = GetCartItems();
            if (!cartItems.Any())
                return Redirect(Request.UrlReferrer?.ToString() ?? Url.Action("Index", "Home"));

            return View(cartItems);
        }

        public ActionResult Cart()
        {
            var cart = Session["Cart"] as Cart ?? new Cart();
            var cartItems = cart.Items.ToList();
            ViewBag.CurrentAction = "Cart";
            return View(cartItems);
        }

        [HttpPost]
        public ActionResult AddToCart(string productId, string size, int quantity)
        {
            var db = new TFCEntities();

            var product = db.Productos.FirstOrDefault(p => p.id_producto == productId);
            var productInInventory = db.Inventario.FirstOrDefault(i => i.id_producto == productId && (i.talla_eu == size || i.talla_eu_marca == size));

            db.Dispose();

            var cart = Session["Cart"] as Cart ?? new Cart();
            var cartItem = cart.Items.FirstOrDefault(item => item.ProductId == productId && item.Size == size);

            if (cartItem == null)
            {
                cart.Items.Add(new CartItem
                {
                    ProductId = productId,
                    ProductName = product.nombre,
                    Size = size,
                    ImageUrl = product.imagen,
                    Quantity = quantity,
                    Price = productInInventory.precio,
                    MaxStock = productInInventory.cantidad
                });
            }
            else
            {
                if (cartItem.Quantity >= cartItem.MaxStock)
                    return Json(new { success = false, message = "El producto ya se encuentra en el carrito con las existencias máximas." });

                cartItem.Quantity += quantity;
                if (cartItem.Quantity > cartItem.MaxStock)
                {
                    cartItem.Quantity = cartItem.MaxStock;
                }
            }

            Session["Cart"] = cart;
            return PartialView("_CartPartial", cart);
        }

        [HttpPost]
        public ActionResult UpdateQuantity(string productId, string size, int change)
        {
            var cart = Session["Cart"] as Cart ?? new Cart();
            var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == productId && i.Size == size);
            if (cartItem != null)
            {
                cartItem.Quantity += change;
                if (cartItem.Quantity < 1)
                    cartItem.Quantity = 1;
                else if (cartItem.Quantity > cartItem.MaxStock)
                    cartItem.Quantity = cartItem.MaxStock;
            }

            return PartialView("_CartPartial", cart);
        }

        [HttpPost]
        public ActionResult RemoveFromCart(string productId, string size, bool cartPage = false)
        {
            var cart = Session["Cart"] as Cart ?? new Cart();
            var cartItem = cart.Items.FirstOrDefault(item => item.ProductId == productId && item.Size == size);

            if (cartItem != null)
                cart.Items.Remove(cartItem);

            Session["Cart"] = cart;

            if (!cartPage)
                return PartialView("_CartPartial", cart);
            else
                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult UpdateCart(List<int> quantities)
        {
            var cart = Session["Cart"] as Cart ?? new Cart();

            for (int i = 0; i < cart.Items.Count; i++)
            {
                if (i < quantities.Count) {
                    if (quantities[i] == 0)
                        cart.Items.Remove(cart.Items[i]);
                    else
                        cart.Items[i].Quantity = quantities[i];
                }
            }

            Session["Cart"] = cart;
            return Json(new { success = true }, JsonRequestBehavior.AllowGet);
        }
    }
}