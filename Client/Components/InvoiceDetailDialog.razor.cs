using Microsoft.AspNetCore.Components;
using AutoDealerSphere.Shared.Models;
using Syncfusion.Blazor.Popups;
using System.Net.Http.Json;

namespace AutoDealerSphere.Client.Components
{
    public partial class InvoiceDetailDialog
    {
        [Inject] private HttpClient Http { get; set; } = default!;
        [Parameter] public EventCallback<InvoiceDetail> OnSave { get; set; }

        private SfDialog? _dialog;
        private bool _isVisible = false;
        private bool _isEdit = false;
        private InvoiceDetail? _model;
        private decimal _subTotal = 0;

        // 部品選択関連
        private List<Part> _allParts = new();
        private List<Part> _filteredParts = new();
        private List<string> _partTypes = new();
        private string _partSearchText = string.Empty;
        private string _selectedType = string.Empty;
        private int _selectedPartId = 0;
        private string _selectedPartIdString = string.Empty;

        // 修理方法選択肢
        private List<RepairMethodItem> _repairMethods = new()
        {
            new RepairMethodItem { Value = "交換", Text = "交換" },
            new RepairMethodItem { Value = "修理", Text = "修理" },
            new RepairMethodItem { Value = "調整", Text = "調整" },
            new RepairMethodItem { Value = "清掃", Text = "清掃" },
            new RepairMethodItem { Value = "点検", Text = "点検" },
            new RepairMethodItem { Value = "その他", Text = "その他" }
        };

        // ダイアログステップ管理
        private DialogStep _currentStep = DialogStep.SelectPart;

        private string DialogTitle => _currentStep == DialogStep.SelectPart 
            ? (_isEdit ? "明細編集 - 部品選択" : "明細追加 - 部品選択")
            : (_isEdit ? "明細編集 - 詳細入力" : "明細追加 - 詳細入力");

        public async Task Open(InvoiceDetail detail, bool isEdit)
        {
            _isEdit = isEdit;
            
            // 部品データを読み込み
            await LoadParts();

            if (isEdit && detail.PartId > 0)
            {
                // 編集時は既存の部品を選択状態にして詳細画面から開始
                _selectedPartId = detail.PartId ?? 0;
                _selectedPartIdString = _selectedPartId.ToString();
                _model = new InvoiceDetail
                {
                    Id = detail.Id,
                    InvoiceId = detail.InvoiceId,
                    PartId = detail.PartId,
                    ItemName = detail.ItemName ?? string.Empty,
                    Type = detail.Type,
                    RepairMethod = detail.RepairMethod,
                    Quantity = detail.Quantity,
                    UnitPrice = detail.UnitPrice,
                    LaborCost = detail.LaborCost,
                    IsTaxable = detail.IsTaxable,
                    DisplayOrder = detail.DisplayOrder
                };
                CalculateSubTotal();
                _currentStep = DialogStep.EditDetails;
            }
            else
            {
                // 新規追加時は部品選択から開始
                _model = new InvoiceDetail
                {
                    InvoiceId = detail.InvoiceId,
                    Quantity = 1,
                    IsTaxable = true,
                    DisplayOrder = detail.DisplayOrder
                };
                _currentStep = DialogStep.SelectPart;
            }

            _isVisible = true;
            StateHasChanged();
        }

        private async Task LoadParts()
        {
            try
            {
                var parts = await Http.GetFromJsonAsync<List<Part>>("api/Parts");
                if (parts != null)
                {
                    _allParts = parts.Where(p => p.IsActive).ToList();
                    _filteredParts = _allParts;
                    
                    // タイプのリストを作成
                    _partTypes = _allParts
                        .Where(p => !string.IsNullOrEmpty(p.Type))
                        .Select(p => p.Type!)
                        .Distinct()
                        .OrderBy(t => t)
                        .ToList();
                    _partTypes.Insert(0, string.Empty); // "すべて"オプション
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"部品データの読み込みに失敗しました: {ex.Message}");
            }
        }

        private async Task SearchParts()
        {
            await FilterParts();
        }

        private async Task FilterPartsByType()
        {
            await FilterParts();
        }

        private Task FilterParts()
        {
            _filteredParts = _allParts.Where(p =>
                (string.IsNullOrEmpty(_partSearchText) || p.PartName.Contains(_partSearchText, StringComparison.OrdinalIgnoreCase)) &&
                (string.IsNullOrEmpty(_selectedType) || p.Type == _selectedType)
            ).ToList();

            StateHasChanged();
            return Task.CompletedTask;
        }

        private void OnPartSelected(int partId)
        {
            _selectedPartId = partId;
            _selectedPartIdString = partId.ToString();
            StateHasChanged();
        }

        private void ProceedToDetails()
        {
            if (_selectedPartId > 0 && _model != null)
            {
                var selectedPart = _allParts.FirstOrDefault(p => p.Id == _selectedPartId);
                if (selectedPart != null)
                {
                    _model.PartId = selectedPart.Id;
                    _model.ItemName = selectedPart.PartName;
                    _model.Type = selectedPart.Type;
                    _model.UnitPrice = selectedPart.UnitPrice;
                    
                    CalculateSubTotal();
                    _currentStep = DialogStep.EditDetails;
                    StateHasChanged();
                }
            }
        }

        private void BackToPartSelection()
        {
            _currentStep = DialogStep.SelectPart;
            StateHasChanged();
        }

        private void CalculateSubTotal()
        {
            if (_model != null)
            {
                _subTotal = (_model.UnitPrice * _model.Quantity) + _model.LaborCost;
            }
        }

        private async Task OnValidSubmit()
        {
            if (_model != null)
            {
                await OnSave.InvokeAsync(_model);
                _isVisible = false;
                _currentStep = DialogStep.SelectPart;
                _selectedPartId = 0;
                _selectedPartIdString = string.Empty;
                _partSearchText = string.Empty;
                _selectedType = string.Empty;
            }
        }

        private void Cancel()
        {
            _isVisible = false;
            _currentStep = DialogStep.SelectPart;
            _selectedPartId = 0;
            _selectedPartIdString = string.Empty;
            _partSearchText = string.Empty;
            _selectedType = string.Empty;
        }

        private enum DialogStep
        {
            SelectPart,
            EditDetails
        }

        private class RepairMethodItem
        {
            public string Value { get; set; } = string.Empty;
            public string Text { get; set; } = string.Empty;
        }
    }
}