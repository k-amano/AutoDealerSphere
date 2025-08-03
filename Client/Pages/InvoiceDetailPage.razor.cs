using AutoDealerSphere.Shared.Models;
using AutoDealerSphere.Client.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Net.Http.Json;

namespace AutoDealerSphere.Client.Pages
{
    public partial class InvoiceDetailPage
    {
        [Parameter]
        public int InvoiceId { get; set; }

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = default!;

        [Inject]
        private HttpClient Http { get; set; } = default!;

        [Inject]
        private NavigationManager NavigationManager { get; set; } = default!;

        private Invoice? _invoice;
        private AutoDealerSphere.Client.Components.InvoiceBasicInfoDialog? _basicInfoDialog;
        private AutoDealerSphere.Client.Components.InvoiceDetailDialog? _detailDialog;

        // UI表示制御プロパティ
        protected bool IsLoading => _invoice == null;
        protected bool IsNewInvoice => InvoiceId == 0;
        protected bool HasInvoice => _invoice != null;
        protected bool CanShowDetails => InvoiceId > 0;
        protected bool HasNotes => !string.IsNullOrEmpty(_invoice?.Notes);
        
        // CSS制御用プロパティ
        protected string MainContentClass => IsLoading ? "hidden" : "";
        protected string LoadingContentClass => IsLoading ? "" : "hidden";
        protected string NewInvoiceButtonClass => IsNewInvoice ? "" : "hidden";
        protected string ExistingInvoiceButtonClass => !IsNewInvoice ? "" : "hidden";
        protected string InvoiceDetailsClass => CanShowDetails ? "" : "hidden";
        protected string InvoiceSummaryClass => CanShowDetails ? "" : "hidden";
        protected string InvoiceNotesClass => HasNotes ? "" : "hidden";
        protected string NextInspectionRowClass => !string.IsNullOrEmpty(NextInspectionDateDisplay) ? "info-row" : "info-row hidden";
        protected string MileageRowClass => !string.IsNullOrEmpty(MileageDisplay) ? "info-row" : "info-row hidden";

        protected override async Task OnInitializedAsync()
        {
            await LoadInvoice();
        }

        private async Task LoadInvoice()
        {
            if (InvoiceId == 0)
            {
                // 新規作成モード
                _invoice = new Invoice
                {
                    InvoiceDate = DateTime.Today,
                    WorkCompletedDate = DateTime.Today,
                    TaxRate = 10m,
                    InvoiceDetails = new List<AutoDealerSphere.Shared.Models.InvoiceDetail>()
                };
                
                // 請求書番号を取得
                var response = await Http.GetFromJsonAsync<InvoiceNumberResponse>("api/Invoices/new-number");
                if (response != null)
                {
                    _invoice.InvoiceNumber = response.InvoiceNumber;
                }
            }
            else
            {
                // 既存データの読み込み
                _invoice = await Http.GetFromJsonAsync<Invoice>($"api/Invoices/{InvoiceId}");
            }
        }

        protected async Task OpenBasicInfoDialog()
        {
            if (_basicInfoDialog != null && _invoice != null)
            {
                await _basicInfoDialog.Open(_invoice);
            }
        }

        protected async Task OpenDetailDialog()
        {
            if (_detailDialog != null && _invoice != null)
            {
                var newDetail = new AutoDealerSphere.Shared.Models.InvoiceDetail
                {
                    InvoiceId = InvoiceId,
                    Quantity = 1,
                    IsTaxable = true,
                    DisplayOrder = _invoice.InvoiceDetails.Count
                };
                await _detailDialog.Open(newDetail, false);
            }
        }

        protected async Task EditDetail(AutoDealerSphere.Shared.Models.InvoiceDetail detail)
        {
            if (_detailDialog != null)
            {
                await _detailDialog.Open(detail, true);
            }
        }

        protected async Task DeleteDetail(AutoDealerSphere.Shared.Models.InvoiceDetail detail)
        {
            if (detail.Id > 0)
            {
                var response = await Http.DeleteAsync($"api/Invoices/{InvoiceId}/details/{detail.Id}");
                if (response.IsSuccessStatusCode)
                {
                    await LoadInvoice();
                }
            }
        }

        protected async Task OnBasicInfoSaved(Invoice invoice)
        {
            if (InvoiceId == 0)
            {
                // 新規作成
                var response = await Http.PostAsJsonAsync("api/Invoices", invoice);
                if (response.IsSuccessStatusCode)
                {
                    var created = await response.Content.ReadFromJsonAsync<Invoice>();
                    if (created != null)
                    {
                        NavigationManager.NavigateTo($"/invoice/detail/{created.Id}");
                    }
                }
            }
            else
            {
                // 更新
                var response = await Http.PutAsJsonAsync($"api/Invoices/{InvoiceId}", invoice);
                if (response.IsSuccessStatusCode)
                {
                    await LoadInvoice();
                }
            }
        }

        protected async Task OnDetailSaved(AutoDealerSphere.Shared.Models.InvoiceDetail detail)
        {
            if (detail.Id == 0)
            {
                // 新規追加
                var response = await Http.PostAsJsonAsync($"api/Invoices/{InvoiceId}/details", detail);
                if (response.IsSuccessStatusCode)
                {
                    await LoadInvoice();
                }
            }
            else
            {
                // 更新
                var response = await Http.PutAsJsonAsync($"api/Invoices/{InvoiceId}/details/{detail.Id}", detail);
                if (response.IsSuccessStatusCode)
                {
                    await LoadInvoice();
                }
            }
        }

        protected async Task DeleteInvoice()
        {
            var response = await Http.DeleteAsync($"api/Invoices/{InvoiceId}");
            if (response.IsSuccessStatusCode)
            {
                NavigationManager.NavigateTo("/invoicelist");
            }
        }

        protected async Task ExportExcel()
        {
            var response = await Http.GetAsync($"api/Invoices/{InvoiceId}/export");
            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                var contentDisposition = response.Content.Headers.ContentDisposition;
                var fileName = contentDisposition?.FileName?.Trim('"') ?? $"invoice_{InvoiceId}_{DateTime.Now:yyyyMMdd}.xlsx";
                
                // JavaScript経由でファイルをダウンロード
                await JSRuntime.InvokeVoidAsync("downloadFileFromStream", fileName, bytes);
            }
        }

        protected void NavigateToInvoiceList()
        {
            NavigationManager.NavigateTo("/invoicelist");
        }

        // UI表示用のプロパティ
        protected string InvoiceNumberDisplay => _invoice?.InvoiceNumber ?? string.Empty;
        protected string ClientNameDisplay => _invoice?.Client?.Name ?? string.Empty;
        protected string VehicleDisplay => _invoice?.Vehicle != null 
            ? $"{_invoice.Vehicle.VehicleName} ({_invoice.Vehicle.LicensePlateNumber})"
            : string.Empty;
        protected string InvoiceDateDisplay => _invoice?.InvoiceDate.ToString("yyyy/MM/dd") ?? string.Empty;
        protected string WorkCompletedDateDisplay => _invoice?.WorkCompletedDate.ToString("yyyy/MM/dd") ?? string.Empty;
        protected string NextInspectionDateDisplay => _invoice?.NextInspectionDate?.ToString("yyyy/MM/dd") ?? string.Empty;
        protected string MileageDisplay => _invoice?.Mileage?.ToString("N0") ?? string.Empty;
        protected string NotesDisplay => _invoice?.Notes ?? string.Empty;
        
        protected decimal TaxableSubTotal => _invoice?.TaxableSubTotal ?? 0;
        protected decimal NonTaxableSubTotal => _invoice?.NonTaxableSubTotal ?? 0;
        protected decimal TaxRate => _invoice?.TaxRate ?? 10m;
        protected decimal Tax => _invoice?.Tax ?? 0;
        protected decimal Total => _invoice?.Total ?? 0;

        private class InvoiceNumberResponse
        {
            public string InvoiceNumber { get; set; } = string.Empty;
        }
    }
}