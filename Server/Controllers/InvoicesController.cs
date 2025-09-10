using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoDealerSphere.Server.Services;
using AutoDealerSphere.Shared.Models;

namespace AutoDealerSphere.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoicesController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Invoice>>> GetInvoices()
        {
            var invoices = await _invoiceService.GetAllInvoicesAsync();
            return Ok(invoices);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Invoice>> GetInvoice(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }
            return Ok(invoice);
        }

        [HttpGet("new-number/{clientId}")]
        public async Task<ActionResult<string>> GetNewInvoiceNumber(int clientId)
        {
            var number = await _invoiceService.GenerateInvoiceNumberAsync(clientId);
            return Ok(new { invoiceNumber = number });
        }

        [HttpPost]
        public async Task<ActionResult<Invoice>> CreateInvoice(Invoice invoice)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdInvoice = await _invoiceService.CreateInvoiceAsync(invoice);
            return CreatedAtAction(nameof(GetInvoice), new { id = createdInvoice.Id }, createdInvoice);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateInvoice(int id, Invoice invoice)
        {
            if (id != invoice.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var updated = await _invoiceService.UpdateInvoiceAsync(invoice);
            if (!updated)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInvoice(int id)
        {
            var deleted = await _invoiceService.DeleteInvoiceAsync(id);
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpGet("by-invoice-number/{invoiceNumber}")]
        public async Task<ActionResult<Dictionary<int, Invoice>>> GetInvoicesByInvoiceNumber(string invoiceNumber)
        {
            var invoices = await _invoiceService.GetInvoicesByInvoiceNumberAsync(invoiceNumber);
            return Ok(invoices);
        }

        [HttpGet("{id}/export")]
        public async Task<IActionResult> ExportToExcel(int id)
        {
            try
            {
                var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
                if (invoice == null)
                {
                    return NotFound($"Invoice with ID {id} not found");
                }

                var excelData = await _invoiceService.ExportToExcelAsync(id);
                var fileName = $"請求書{invoice.InvoiceNumber}.xlsx";
                
                return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error exporting invoice: {ex.Message}");
            }
        }

        // 明細エンドポイント
        [HttpPost("{invoiceId}/details")]
        public async Task<ActionResult<InvoiceDetail>> CreateInvoiceDetail(int invoiceId, InvoiceDetail detail)
        {
            var created = await _invoiceService.CreateInvoiceDetailAsync(invoiceId, detail);
            return CreatedAtAction(nameof(GetInvoice), new { id = invoiceId }, created);
        }

        [HttpPut("{invoiceId}/details/{detailId}")]
        public async Task<IActionResult> UpdateInvoiceDetail(int invoiceId, int detailId, InvoiceDetail detail)
        {
            if (detailId != detail.Id)
            {
                return BadRequest("Detail ID mismatch");
            }

            var updated = await _invoiceService.UpdateInvoiceDetailAsync(invoiceId, detailId, detail);
            if (updated == null)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpDelete("{invoiceId}/details/{detailId}")]
        public async Task<IActionResult> DeleteInvoiceDetail(int invoiceId, int detailId)
        {
            var deleted = await _invoiceService.DeleteInvoiceDetailAsync(invoiceId, detailId);
            if (!deleted)
            {
                return NotFound();
            }

            return NoContent();
        }
    }
}