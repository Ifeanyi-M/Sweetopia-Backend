using Azure;
using BlueAlmond.Data;
using BlueAlmond.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace BlueAlmond.Controllers
{
    [Route("api/ShoppingCart")]
    [ApiController]
    public class ShoppingCartController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        protected ApiResponse _response;

        public ShoppingCartController(ApplicationDbContext db)
        {
            _db = db;
            _response = new();
        }

        [HttpGet]

        public async Task<ActionResult<ApiResponse>> GetShoppingCart(string userId)
        {
            try
            {
                ShoppingCart shoppingCart;

                if (string.IsNullOrEmpty(userId))
                {
                    shoppingCart = new();
                }
                else
                {
                    shoppingCart = _db.ShoppingCarts.Include(u => u.CartItems).ThenInclude(u => u.MenuItem).FirstOrDefault(u => u.UserId == userId);

                }

                

                if(shoppingCart.CartItems !=null && shoppingCart.CartItems.Count > 0)
                {
                    shoppingCart.CartTotal = shoppingCart.CartItems.Sum(u => u.Quantity * u.MenuItem.Price);
                }

                _response.Result = shoppingCart;
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);

            }
            catch (Exception ex)
            {

                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _response.StatusCode = HttpStatusCode.BadRequest;
            }

            return Ok(_response);

        }


        [HttpPost]

        public async Task<ActionResult<ApiResponse>> AddOrUpdateItemInCart(string userId, int menuItemId, int updateQuantityBy)
        {
            ShoppingCart shoppingCart = _db.ShoppingCarts.Include(u => u.CartItems).FirstOrDefault(u => u.UserId == userId);
            MenuItem menuItem = _db.MenuItems.FirstOrDefault(u => u.Id == menuItemId);

            if(menuItem == null)
            {
                _response.StatusCode = System.Net.HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                return BadRequest(_response);
            }

            if(shoppingCart == null && updateQuantityBy > 0)
            {
                ShoppingCart newCart = new()
                {
                    UserId = userId

                };

                _db.ShoppingCarts.Add(newCart);
                _db.SaveChanges();

                CartItem newCartItem = new()
                {
                    MenuItemId = menuItemId,
                    Quantity = updateQuantityBy,
                    ShoppingCartId = newCart.Id,
                    MenuItem = null
                };

                _db.CartItems.Add(newCartItem);
                _db.SaveChanges();
            }
            else
            {
                CartItem cartItemInCart = shoppingCart.CartItems.FirstOrDefault(u => u.MenuItemId == menuItemId);
                if(cartItemInCart == null)
                {
                    CartItem newCartItem = new()
                    {
                        MenuItemId = menuItemId,
                        Quantity = updateQuantityBy,
                        ShoppingCartId = shoppingCart.Id,
                        MenuItem = null
                    };

                    _db.CartItems.Add(newCartItem);
                    _db.SaveChanges();
                }

                else
                {
                    int newQuantity = cartItemInCart.Quantity + updateQuantityBy;
                    if(updateQuantityBy == 0 || newQuantity <= 0)
                    {
                        _db.CartItems.Remove(cartItemInCart);

                        if(shoppingCart.CartItems.Count() == 1)
                        {
                            _db.ShoppingCarts.Remove(shoppingCart);
                        }

                        _db.SaveChanges();
                    }
                    else
                    {
                        cartItemInCart.Quantity = newQuantity;

                        _db.SaveChanges();
                    }

                }
            }

            return _response;
        }
    }
}
