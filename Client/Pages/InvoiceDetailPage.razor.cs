using AutoDealerSphere.Shared.Models;
using AutoDealerSphere.Client.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Linq;

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
        private AutoDealerSphere.Client.Components.StatutoryFeeDialog? _statutoryFeeDialog;
        private bool _showDeleteConfirmation = false;

        // UI表示制御プロパティ
        protected bool IsLoading => _invoice == null;
        protected bool HasInvoice => _invoice != null;
        protected bool CanShowDetails => InvoiceId > 0;
        protected bool HasNotes => !string.IsNullOrEmpty(_invoice?.Notes);
        
        // CSS制御用プロパティ
        protected string MainContentClass => IsLoading ? "hidden" : "";
        protected string LoadingContentClass => IsLoading ? "" : "hidden";
        protected string InvoiceDetailsClass => CanShowDetails ? "" : "hidden";
        protected string InvoiceSummaryClass => CanShowDetails ? "" : "hidden";
        protected string InvoiceNotesClass => HasNotes ? "" : "hidden";
        protected string NextInspectionRowClass => !string.IsNullOrEmpty(NextInspectionDateDisplay) ? "info-row" : "info-row hidden";
        protected string MileageRowClass => !string.IsNullOrEmpty(MileageDisplay) ? "info-row" : "info-row hidden";
        protected string StatutoryFeesClass => "";
        
        // 明細の分類
        protected IEnumerable<AutoDealerSphere.Shared.Models.InvoiceDetail> RegularDetails => _invoice?.InvoiceDetails?.Where(d => d.Type != "法定費用") ?? Enumerable.Empty<AutoDealerSphere.Shared.Models.InvoiceDetail>();
        protected IEnumerable<AutoDealerSphere.Shared.Models.InvoiceDetail> StatutoryFees => _invoice?.InvoiceDetails?.Where(d => d.Type == "法定費用") ?? Enumerable.Empty<AutoDealerSphere.Shared.Models.InvoiceDetail>();

        protected override async Task OnInitializedAsync()
        {
            await LoadInvoice();
        }

        private async Task LoadInvoice()
        {
            if (InvoiceId == 0)
            {
                // ID 0 はサポートしない - 請求書一覧へリダイレクト
                NavigationManager.NavigateTo("/invoicelist");
                return;
            }
            
            // 既存データの読み込み
            _invoice = await Http.GetFromJsonAsync<Invoice>($"api/Invoices/{InvoiceId}");
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

        protected async Task OpenStatutoryFeeDialog()
        {
            if (_statutoryFeeDialog != null && _invoice != null)
            {
                await _statutoryFeeDialog.Open(InvoiceId, _invoice.VehicleId);
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
            // 更新のみサポート（新規作成は請求書一覧画面から行う）
            var response = await Http.PutAsJsonAsync($"api/Invoices/{InvoiceId}", invoice);
            if (response.IsSuccessStatusCode)
            {
                await LoadInvoice();
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

        protected async Task OnStatutoryFeeSaved(List<AutoDealerSphere.Shared.Models.StatutoryFee> statutoryFees)
        {
            // 既存の法定費用明細を取得
            var existingStatutoryDetails = _invoice?.InvoiceDetails?.Where(d => d.Type == "法定費用").ToList() ?? new List<AutoDealerSphere.Shared.Models.InvoiceDetail>();

            // 選択された法定費用を追加（重複チェック付き）
            foreach (var statutoryFee in statutoryFees)
            {
                var existingDetail = existingStatutoryDetails.FirstOrDefault(d => d.ItemName == statutoryFee.FeeType);
                if (existingDetail == null)
                {
                    // 新規追加
                    var detail = new AutoDealerSphere.Shared.Models.InvoiceDetail
                    {
                        InvoiceId = InvoiceId,
                        ItemName = statutoryFee.FeeType,
                        Type = "法定費用",
                        Quantity = 1,
                        UnitPrice = statutoryFee.Amount,
                        LaborCost = 0,
                        IsTaxable = statutoryFee.IsTaxable,
                        DisplayOrder = _invoice?.InvoiceDetails.Count ?? 0
                    };

                    var response = await Http.PostAsJsonAsync($"api/Invoices/{InvoiceId}/details", detail);
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Failed to add statutory fee: {statutoryFee.FeeType}");
                    }
                }
            }

            // 選択されていない法定費用を削除
            foreach (var existingDetail in existingStatutoryDetails)
            {
                var isSelected = statutoryFees.Any(f => f.FeeType == existingDetail.ItemName);
                if (!isSelected)
                {
                    var response = await Http.DeleteAsync($"api/Invoices/{InvoiceId}/details/{existingDetail.Id}");
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Failed to delete statutory fee: {existingDetail.ItemName}");
                    }
                }
            }

            // 明細を再読み込み
            await LoadInvoice();
        }

        protected void ShowDeleteConfirmation()
        {
            _showDeleteConfirmation = true;
        }

        protected void CloseDeleteConfirmation()
        {
            _showDeleteConfirmation = false;
        }

        protected async Task DeleteInvoice()
        {
            _showDeleteConfirmation = false;
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
                // クライアント側で請求書番号を使用してファイル名を生成
                var fileName = $"請求書{_invoice?.InvoiceNumber ?? InvoiceId.ToString()}.xlsx";
                
                // JavaScript経由でファイルをダウンロード (MemoryStreamを使用)
                using var stream = new System.IO.MemoryStream(bytes);
                using var streamRef = new Microsoft.JSInterop.DotNetStreamReference(stream: stream);
                await JSRuntime.InvokeVoidAsync("downloadFileFromStream", fileName, streamRef);
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
        
        protected decimal SubTotal => (_invoice?.TaxableSubTotal ?? 0) + (_invoice?.NonTaxableSubTotal ?? 0);
        protected decimal TaxRate => _invoice?.TaxRate ?? 10m;
        protected decimal Tax => _invoice?.Tax ?? 0;
        protected decimal Total => _invoice?.Total ?? 0;

        private class InvoiceNumberResponse
        {
            public string InvoiceNumber { get; set; } = string.Empty;
        }
    }
}