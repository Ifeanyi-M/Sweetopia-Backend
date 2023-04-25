using BlueAlmond.Data;
using BlueAlmond.Models;
using BlueAlmond.Models.DTO;
using BlueAlmond.Services;
using BlueAlmond.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace BlueAlmond.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        
        private ApiResponse _response;
        public OrderController(ApplicationDbContext db)
        {
            _db = db;
            
            _response = new ApiResponse();
        }

        [HttpGet]
        [Authorize]

        public async Task<ActionResult<ApiResponse>> GetOrders(string? userId, string searchString, string status, int pageNumber = 1, int pageSize = 5)
        {
            try
            {
                IEnumerable<OrderHeader> orderHeaders = _db.OrderHeaders
                    .Include(u => u.OrderDetails)
                    .ThenInclude(u => u.MenuItem)
                    .OrderByDescending(u => u.OrderHeaderId);

                if (!string.IsNullOrEmpty(userId))
                {
                    orderHeaders = orderHeaders.Where(u => u.ApplicationUserId == userId);
                }
                if (!string.IsNullOrEmpty(searchString))
                {
                    orderHeaders = orderHeaders.Where(u => u.PickupPhoneNumber.Contains(searchString.ToLower()) ||
                    u.PickupEmail.Contains(searchString.ToLower()) || u.PickupName.Contains(searchString.ToLower()));
                }

                if (!string.IsNullOrEmpty(status))
                {
                    orderHeaders = orderHeaders.Where(u => u.Status.ToLower() == status.ToLower());
                }


                Pagination pagination = new()
                {
                    CurrentPage = pageNumber,
                    PageSize = pageSize,
                    TotalRecords = orderHeaders.Count(),
                };

                Response.Headers.Add("X-Pagination", JsonSerializer.Serialize(pagination));



                _response.Result = orderHeaders.Skip((pageNumber - 1) * pageSize).Take(pageSize);
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);

            }

            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
            }

            return _response;
        }




        [HttpGet("{id:int}")]

        public async Task<ActionResult<ApiResponse>> GetOrders(int id)
        {
            try
            {
                
                if (id == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest(_response);
                }

                var orderHeaders = _db.OrderHeaders
                    .Include(u => u.OrderDetails)
                    .ThenInclude(u => u.MenuItem)
                    .Where(u => u.OrderHeaderId == id);

                if (orderHeaders == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    return NotFound(_response);
                }


                _response.Result = orderHeaders;
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);

            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
            }

            return _response;
        }


        [HttpPost]

        public async Task<ActionResult<ApiResponse>> CreateOrder([FromBody] OrderHeaderCreateDto orderHeaderDto)
        {
            try
            {
                OrderHeader order = new()
                {
                    ApplicationUserId = orderHeaderDto.ApplicationUserId,
                    PickupEmail = orderHeaderDto.PickupEmail,
                    PickupName = orderHeaderDto.PickupName,
                    PickupPhoneNumber = orderHeaderDto.PickupPhoneNumber,
                    OrderTotal = orderHeaderDto.OrderTotal,
                    OrderDate = DateTime.Now,
                    StripePaymentIntentID = orderHeaderDto.StripePaymentIntentID,
                    TotalItems = orderHeaderDto.TotalItems,
                    Status = String.IsNullOrEmpty(orderHeaderDto.Status) ? SD.status_pending : orderHeaderDto.Status,
                };

                if (ModelState.IsValid)
                {
                    _db.OrderHeaders.Add(order);
                    _db.SaveChanges();

                    foreach(var orderDetailDto in orderHeaderDto.OrderDetailsDto)
                    {
                        OrderDetail orderDetail = new()
                        {
                            OrderHeaderId = order.OrderHeaderId,
                            ItemName = orderDetailDto.ItemName,
                            MenuItemId = orderDetailDto.MenuItemId,
                            Price = orderDetailDto.Price,
                            Quantity = orderDetailDto.Quantity,


                        };

                        _db.OrderDetails.Add(orderDetail);
                    }

                    _db.SaveChanges();

                    _response.Result = order;
                    order.OrderDetails = null;
                    _response.StatusCode = HttpStatusCode.Created;
                    return Ok(_response);
                }

            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
            }

            return _response;
        }



        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse>> UpdateOrderHeader(int id, [FromBody] OrderHeaderUpdateDto orderHeaderUpdateDto)
        {
            try
            {
                if (orderHeaderUpdateDto == null || id != orderHeaderUpdateDto.OrderHeaderId)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest();
                }

                OrderHeader orderFromDb = _db.OrderHeaders.FirstOrDefault(u => u.OrderHeaderId == id);

                if (orderFromDb == null)
                {
                    _response.IsSuccess = false;
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    return BadRequest();
                }

                if (!string.IsNullOrEmpty(orderHeaderUpdateDto.PickupName))
                {
                    orderFromDb.PickupName = orderHeaderUpdateDto.PickupName;
                }
                if (!string.IsNullOrEmpty(orderHeaderUpdateDto.PickupPhoneNumber))
                {
                    orderFromDb.PickupPhoneNumber = orderHeaderUpdateDto.PickupPhoneNumber;
                }
                if (!string.IsNullOrEmpty(orderHeaderUpdateDto.PickupEmail))
                {
                    orderFromDb.PickupEmail = orderHeaderUpdateDto.PickupEmail;
                }
                if (!string.IsNullOrEmpty(orderHeaderUpdateDto.Status))
                {
                    orderFromDb.Status = orderHeaderUpdateDto.Status;
                }
                if (!string.IsNullOrEmpty(orderHeaderUpdateDto.StripePaymentIntentID))
                {
                    orderFromDb.StripePaymentIntentID = orderHeaderUpdateDto.StripePaymentIntentID;
                }


                _db.SaveChanges();
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);

            }
            catch (Exception ex)
            {

                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
            }

            return _response;
        }
    }
}
