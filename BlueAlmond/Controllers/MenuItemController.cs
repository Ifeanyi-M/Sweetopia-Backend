using BlueAlmond.Data;
using BlueAlmond.Models;
using BlueAlmond.Models.DTO;
using BlueAlmond.Services;
using BlueAlmond.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using static System.Net.Mime.MediaTypeNames;

namespace BlueAlmond.Controllers
{
    [Route("api/MenuItem")]
    [ApiController]
    public class MenuItemController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IBlobService _blobService;
        private ApiResponse _response;
        public MenuItemController(ApplicationDbContext db, IBlobService blobService)
        {
            _db = db;
            _blobService = blobService;
            _response = new ApiResponse();
        }

        [HttpGet]
        public async Task<IActionResult> GetMenuItems()
        {
            _response.Result = _db.MenuItems;
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }

        [HttpGet ("{id:int}", Name = "GetMenuItem")]
        public async Task<IActionResult> GetMenuItem(int id)
        {
            if(id == 0)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                return BadRequest(_response);
            }

            MenuItem menuItem = _db.MenuItems.FirstOrDefault(x => x.Id == id);

            if(menuItem == null)
            {
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.IsSuccess = false;
                return NotFound(_response);
            }
            _response.Result = menuItem;
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin)]


        public async Task<ActionResult<ApiResponse>> CreateMenuItem([FromForm] MenuItemCreateDto menuItemCreateDtO)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if(menuItemCreateDtO.File == null || menuItemCreateDtO.File.Length == 0)
                    {
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        return BadRequest();

                    }

                    string fileName = $"{Guid.NewGuid()}{Path.GetExtension(menuItemCreateDtO.File.FileName)}";

                    MenuItem menuItemToCreate = new()
                    {
                        Name = menuItemCreateDtO.Name,
                        Price = menuItemCreateDtO.Price,
                        Category = menuItemCreateDtO.Category,
                        SpecialTag = menuItemCreateDtO.SpecialTag,
                        Description = menuItemCreateDtO.Description,
                        Image= await _blobService.UploadBlob(fileName, SD.SD_Storage_Container, menuItemCreateDtO.File)

                    };

                    _db.MenuItems.Add(menuItemToCreate);
                    _db.SaveChanges();
                    _response.Result = menuItemToCreate;
                    _response.StatusCode = HttpStatusCode.Created;

                    return CreatedAtRoute("GetMenuItem", new { id = menuItemToCreate.Id }, _response);
                }
                else
                {
                    _response.IsSuccess = false;
                }

            }
            catch (Exception ex)
            {

                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
            }

            return _response;

        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = SD.Role_Admin)]

        public async Task<ActionResult<ApiResponse>> UpdateMenuItem(int id, [FromForm] MenuItemUpdateDto menuItemUpdateDtO)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (menuItemUpdateDtO == null || id != menuItemUpdateDtO.Id)
                    {
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        return BadRequest();

                    }

                    MenuItem menuItemFromDb = await _db.MenuItems.FindAsync(id);

                    if(menuItemFromDb == null)
                    {
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        return BadRequest();
                    }

                    menuItemFromDb.Name = menuItemUpdateDtO.Name;
                    menuItemFromDb.Price = menuItemUpdateDtO.Price;
                    menuItemFromDb.Category = menuItemUpdateDtO.Category;
                    menuItemFromDb.SpecialTag = menuItemUpdateDtO.SpecialTag;
                    menuItemFromDb.Description = menuItemUpdateDtO.Description;

                    if(menuItemUpdateDtO.File != null && menuItemUpdateDtO.File.Length > 0)
                    {
                        string fileName = $"{Guid.NewGuid()}{Path.GetExtension(menuItemUpdateDtO.File.FileName)}";
                        await _blobService.DeleteBlob(menuItemFromDb.Image.Split('/').Last(), SD.SD_Storage_Container);
                        menuItemFromDb.Image = await _blobService.UploadBlob(fileName, SD.SD_Storage_Container, menuItemUpdateDtO.File);
                    }



                   
                    _db.MenuItems.Update(menuItemFromDb);
                    _db.SaveChanges();
                    
                    _response.StatusCode = HttpStatusCode.NoContent;

                    return Ok( _response);
                }
                else
                {
                    _response.IsSuccess = false;
                }

            }
            catch (Exception ex)
            {

                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
            }

            return _response;

        }


        [HttpDelete("{id:int}")]
        [Authorize(Roles = SD.Role_Admin)]
        public async Task<ActionResult<ApiResponse>> DeleteMenuItem(int id)
        {
            try
            {
                
             
                    if ( id == 0)
                    {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    return BadRequest();

                    }

                    MenuItem menuItemFromDb = await _db.MenuItems.FindAsync(id);

                    if (menuItemFromDb == null)
                    {
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        return BadRequest();
                    }

                await _blobService.DeleteBlob(menuItemFromDb.Image.Split('/').Last(), SD.SD_Storage_Container);
                int milliseconds = 2000;
                Thread.Sleep(milliseconds);
                

                    _db.MenuItems.Remove(menuItemFromDb);
                    _db.SaveChanges();

                    _response.StatusCode = HttpStatusCode.NoContent;

                    return Ok(_response);
                

            }
            catch (Exception ex)
            {

                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
            }

            return _response;

        }
    }
}
