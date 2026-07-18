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
    public class ApiTableController : ControllerBase
    {
        private readonly RestaurantDbContext _context;

        public ApiTableController(RestaurantDbContext context)
        {
            _context = context;
        }

        // GET: api/ApiTable
        [HttpGet]
        public IActionResult GetAreasAndTables()
        {
            try
            {
                var areas = _context.Areas
                    .Include(a => a.Tables)
                    .Select(a => new
                    {
                        a.Id,
                        a.Name,
                        Tables = a.Tables.Select(t => new
                        {
                            t.Id,
                            t.TableNumber,
                            t.Capacity,
                            t.Status,
                            t.AreaId
                        }).ToList()
                    }).ToList();

                return Ok(areas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // POST: api/ApiTable/area
        [HttpPost("area")]
        public IActionResult CreateArea([FromBody] AreaModel model)
        {
            if (string.IsNullOrEmpty(model?.Name))
                return BadRequest(new { message = "Area name is required" });

            try
            {
                var area = new Area { Name = model.Name };
                _context.Areas.Add(area);
                _context.SaveChanges();
                return Ok(area);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // POST: api/ApiTable/table
        [HttpPost("table")]
        public IActionResult CreateTable([FromBody] TableModel model)
        {
            if (string.IsNullOrEmpty(model?.TableNumber) || model.AreaId <= 0)
                return BadRequest(new { message = "Table number and Area ID are required" });

            try
            {
                var table = new Table
                {
                    TableNumber = model.TableNumber,
                    AreaId = model.AreaId,
                    Capacity = model.Capacity > 0 ? model.Capacity : 4,
                    Status = "Empty"
                };
                _context.Tables.Add(table);
                _context.SaveChanges();
                return Ok(table);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        public class AreaModel
        {
            public string Name { get; set; }
        }

        public class TableModel
        {
            public string TableNumber { get; set; }
            public int AreaId { get; set; }
            public int Capacity { get; set; }
        }
    }
}
