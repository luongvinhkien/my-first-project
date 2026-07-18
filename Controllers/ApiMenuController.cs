using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quanan.Models;

namespace Quanan.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApiMenuController : ControllerBase
    {
        private readonly RestaurantDbContext _context;

        public ApiMenuController(RestaurantDbContext context)
        {
            _context = context;
        }

        // GET: api/ApiMenu/categories
        [HttpGet("categories")]
        public IActionResult GetCategories()
        {
            try
            {
                var categories = _context.Categories
                    .Select(c => new
                    {
                        c.Id,
                        c.Name
                    }).ToList();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET: api/ApiMenu/items
        [HttpGet("items")]
        public IActionResult GetMenuItems()
        {
            try
            {
                var items = _context.MenuItems
                    .Include(m => m.Category)
                    .Include(m => m.MenuItemOptions)
                    .Where(m => m.IsActive)
                    .Select(m => new
                    {
                        m.Id,
                        m.Code,
                        m.Name,
                        m.Price,
                        m.CostPrice,
                        m.VatPercent,
                        m.Barcode,
                        m.CategoryId,
                        CategoryName = m.Category.Name,
                        m.ImageUrl,
                        m.VideoUrl,
                        Options = m.MenuItemOptions.Select(o => new
                        {
                            o.Id,
                            o.GroupName,
                            o.OptionName,
                            o.ExtraPrice
                        }).ToList()
                    }).ToList();

                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
