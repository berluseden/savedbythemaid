using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using netcore.Data;
using JempSoft.Core.Models.Invent;
using JempSoft.Core.Services;

namespace netcore.Controllers.Api
{

    [Produces("application/json")]
    [Route("api/SalesOrderLine")]
    public class SalesOrderLineController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalesOrderLineController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/SalesOrderLine
        [HttpGet]
        [Authorize]
        public IActionResult GetSalesOrderLine(string masterid)
        {
            return Json(new { data = _context.SalesOrderLine.Include(x => x.product).Where(x => x.salesOrderId.Equals(masterid)).ToList() });
        }

        // POST: api/SalesOrderLine
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> PostSalesOrderLine([FromBody] SalesOrderLine salesOrderLine)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            SalesOrder salesOrder = await _context.SalesOrder.Where(x => x.salesOrderId.Equals(salesOrderLine.salesOrderId)).FirstOrDefaultAsync();

            var editableResult = OrderStatusTransitions.ValidateEditable(salesOrder.salesOrderStatus);
            if (editableResult.IsFailure)
            {
                return Json(new { success = false, message = "Error. " + editableResult.Error.Description });
            }

            salesOrderLine.totalAmount = (decimal)salesOrderLine.qty * salesOrderLine.price;

            if (salesOrderLine.salesOrderLineId == string.Empty)
            {
                salesOrderLine.salesOrderLineId = Guid.NewGuid().ToString();
                _context.SalesOrderLine.Add(salesOrderLine);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Add new data success." });
            }
            else
            {
                _context.Update(salesOrderLine);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Edit data success." });
            }

        }

        // DELETE: api/SalesOrderLine/5
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult>  DeleteSalesOrderLine([FromRoute] string id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var salesOrderLine = await _context.SalesOrderLine
                .Include(x => x.salesOrder)
                .SingleOrDefaultAsync(m => m.salesOrderLineId == id);
            if (salesOrderLine == null)
            {
                return NotFound();
            }

            var deleteEditableResult = OrderStatusTransitions.ValidateEditable(salesOrderLine.salesOrder.salesOrderStatus);
            if (deleteEditableResult.IsFailure)
            {
                return Json(new { success = false, message = "Error. " + deleteEditableResult.Error.Description });
            }

            _context.SalesOrderLine.Remove(salesOrderLine);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Delete success." });
        }


        private bool SalesOrderLineExists(string id)
        {
            return _context.SalesOrderLine.Any(e => e.salesOrderLineId == id);
        }


    }

}
