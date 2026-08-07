using RentalPlatform.Models.Entities;
using RentalPlatform.Models.ViewModels.Vehicle;
using RentalPlatform.Repositories;

namespace RentalPlatform.Services;

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _repository;

    public VehicleService(IVehicleRepository repository)
    {
        _repository = repository;
    }

    public async Task<VehicleOperationResult> CreateAsync(VehicleCreateViewModel model, Guid ownerId)
    {
        if (await _repository.BienSoExistsAsync(model.BienSo))
            return VehicleOperationResult.Fail("Biển số này đã được đăng ký trên hệ thống.");

        // Validate link ảnh TRƯỚC khi tạo xe trong DB. Trước đây validate diễn ra SAU khi xe
        // đã được AddAsync xuống DB — nếu ảnh không hợp lệ, hàm trả lỗi nhưng xe đã tồn tại
        // trong DB ở trạng thái ChoDuyet mà không có ảnh nào ("mồ côi").
        var anhMoi = ParseImageUrls(model.ImageUrls);
        var loi = ValidateImageUrls(anhMoi);
        if (loi is not null) return VehicleOperationResult.Fail(loi);

        var vehicle = new Vehicle
        {
            OwnerId = ownerId,
            TenXe = model.TenXe,
            LoaiXe = model.LoaiXe,
            BienSo = model.BienSo,
            SoChoNgoi = model.SoChoNgoi,
            HopSo = model.HopSo,
            NhienLieu = model.NhienLieu,
            Nam = model.Nam,
            KhuVuc = model.KhuVuc,
            DiaChiCuThe = model.DiaChiCuThe,
            GiaThueMotNgay = model.GiaThueMotNgay,
            MoTa = model.MoTa,
            TrangThai = TrangThaiXe.ChoDuyet
        };

        ThemAnhXe(vehicle, anhMoi);

        vehicle = await _repository.AddAsync(vehicle);
        return VehicleOperationResult.Ok(vehicle);
    }

    public async Task<VehicleOperationResult> UpdateAsync(VehicleEditViewModel model, Guid currentUserId)
    {
        var vehicle = await _repository.GetByIdAsync(model.Id);
        if (vehicle is null) return VehicleOperationResult.Fail("Không tìm thấy xe.");
        if (vehicle.OwnerId != currentUserId) return VehicleOperationResult.Fail("Bạn không có quyền sửa xe này.");

        // Validate link ảnh mới TRƯỚC khi đổi bất kỳ field nào của xe — nếu ảnh không hợp lệ,
        // trả lỗi ngay, chưa có thay đổi nào (kể cả việc xe bị đưa về ChoDuyet ở dưới)
        // được ghi xuống DB.
        var anhMoi = ParseImageUrls(model.NewImageUrls);
        var loi = ValidateImageUrls(anhMoi);
        if (loi is not null) return VehicleOperationResult.Fail(loi);

        vehicle.TenXe = model.TenXe;
        vehicle.LoaiXe = model.LoaiXe;
        vehicle.SoChoNgoi = model.SoChoNgoi;
        vehicle.HopSo = model.HopSo;
        vehicle.NhienLieu = model.NhienLieu;
        vehicle.Nam = model.Nam;
        vehicle.KhuVuc = model.KhuVuc;
        vehicle.DiaChiCuThe = model.DiaChiCuThe;
        vehicle.GiaThueMotNgay = model.GiaThueMotNgay;
        vehicle.MoTa = model.MoTa;

        // Sửa thông tin xe đã duyệt thì cho xét duyệt lại từ đầu, tránh Owner đổi giá/thông tin
        // sau khi đã được Admin duyệt mà không qua kiểm duyệt lại.
        if (vehicle.TrangThai == TrangThaiXe.SanSang)
        {
            vehicle.TrangThai = TrangThaiXe.ChoDuyet;
            vehicle.LyDoTuChoi = null;
        }

        ThemAnhXe(vehicle, anhMoi);

        // Gộp cập nhật field + ảnh mới trong 1 lần SaveChanges (trước đây gọi UpdateAsync
        // 2 lần riêng biệt — lần đầu lưu field, lần sau lưu ảnh — khiến field có thể đã
        // commit dù ảnh sau đó lỗi).
        await _repository.UpdateAsync(vehicle);
        return VehicleOperationResult.Ok(vehicle);
    }

    public async Task<VehicleOperationResult> DeleteAsync(Guid vehicleId, Guid currentUserId)
    {
        var vehicle = await _repository.GetByIdAsync(vehicleId);
        if (vehicle is null) return VehicleOperationResult.Fail("Không tìm thấy xe.");
        if (vehicle.OwnerId != currentUserId) return VehicleOperationResult.Fail("Bạn không có quyền xoá xe này.");
        if (vehicle.TrangThai == TrangThaiXe.DangThue)
            return VehicleOperationResult.Fail("Không thể xoá xe đang trong thời gian cho thuê.");

        await _repository.DeleteAsync(vehicle);
        return VehicleOperationResult.Ok(vehicle);
    }

    public async Task<VehicleOperationResult> DeleteImageAsync(Guid vehicleId, Guid imageId, Guid currentUserId)
    {
        var vehicle = await _repository.GetByIdAsync(vehicleId);
        if (vehicle is null) return VehicleOperationResult.Fail("Không tìm thấy xe.");
        if (vehicle.OwnerId != currentUserId) return VehicleOperationResult.Fail("Bạn không có quyền sửa xe này.");

        var image = vehicle.HinhAnh.FirstOrDefault(i => i.Id == imageId);
        if (image is null) return VehicleOperationResult.Fail("Không tìm thấy ảnh.");

        vehicle.HinhAnh.Remove(image);
        await _repository.UpdateAsync(vehicle);
        return VehicleOperationResult.Ok(vehicle);
    }

    public async Task<VehicleOperationResult> ChangeStatusByOwnerAsync(Guid vehicleId, Guid currentUserId, TrangThaiXe newStatus)
    {
        if (newStatus is not (TrangThaiXe.SanSang or TrangThaiXe.BaoTri))
            return VehicleOperationResult.Fail("Owner chỉ được chuyển trạng thái Sẵn sàng hoặc Bảo trì.");

        var vehicle = await _repository.GetByIdAsync(vehicleId, includeImages: false);
        if (vehicle is null) return VehicleOperationResult.Fail("Không tìm thấy xe.");
        if (vehicle.OwnerId != currentUserId) return VehicleOperationResult.Fail("Bạn không có quyền thao tác trên xe này.");
        if (vehicle.TrangThai == TrangThaiXe.DangThue)
            return VehicleOperationResult.Fail("Xe đang được thuê, không thể đổi trạng thái.");
        if (vehicle.TrangThai is TrangThaiXe.ChoDuyet or TrangThaiXe.TuChoi)
            return VehicleOperationResult.Fail("Xe chưa được Admin duyệt nên chưa thể chuyển trạng thái hoạt động.");

        vehicle.TrangThai = newStatus;
        await _repository.UpdateAsync(vehicle);
        return VehicleOperationResult.Ok(vehicle);
    }

    public async Task<VehicleOperationResult> ApproveAsync(Guid vehicleId)
    {
        var vehicle = await _repository.GetByIdAsync(vehicleId, includeImages: false);
        if (vehicle is null) return VehicleOperationResult.Fail("Không tìm thấy xe.");
        if (vehicle.TrangThai != TrangThaiXe.ChoDuyet)
            return VehicleOperationResult.Fail("Xe này không ở trạng thái chờ duyệt.");

        vehicle.TrangThai = TrangThaiXe.SanSang;
        vehicle.LyDoTuChoi = null;
        await _repository.UpdateAsync(vehicle);
        return VehicleOperationResult.Ok(vehicle);
    }

    public async Task<VehicleOperationResult> RejectAsync(Guid vehicleId, string lyDo)
    {
        var vehicle = await _repository.GetByIdAsync(vehicleId, includeImages: false);
        if (vehicle is null) return VehicleOperationResult.Fail("Không tìm thấy xe.");
        if (vehicle.TrangThai != TrangThaiXe.ChoDuyet)
            return VehicleOperationResult.Fail("Xe này không ở trạng thái chờ duyệt.");
        if (string.IsNullOrWhiteSpace(lyDo))
            return VehicleOperationResult.Fail("Vui lòng nhập lý do từ chối.");

        vehicle.TrangThai = TrangThaiXe.TuChoi;
        vehicle.LyDoTuChoi = lyDo;
        await _repository.UpdateAsync(vehicle);
        return VehicleOperationResult.Ok(vehicle);
    }

    public Task<Vehicle?> GetDetailAsync(Guid id) => _repository.GetByIdAsync(id);

    public Task<List<Vehicle>> GetMyVehiclesAsync(Guid ownerId) => _repository.GetByOwnerAsync(ownerId);

    public Task<List<Vehicle>> GetPendingApprovalAsync() => _repository.GetPendingApprovalAsync();

    public async Task<VehicleSearchResultViewModel> SearchAsync(VehicleSearchViewModel filter)
    {
        var (items, totalCount) = await _repository.SearchAsync(filter);
        filter.TotalCount = totalCount;
        return new VehicleSearchResultViewModel { Filter = filter, Items = items };
    }

    /// <summary>
    /// Tách chuỗi nhập từ textarea (mỗi dòng 1 link) thành danh sách link ảnh, bỏ dòng trống.
    /// </summary>
    private static List<string> ParseImageUrls(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return new List<string>();

        return raw
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(dong => dong.Trim().TrimEnd('\r'))
            .Where(dong => dong.Length > 0)
            .ToList();
    }

    private static string? ValidateImageUrls(List<string> urls)
    {
        foreach (var url in urls)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return $"Link ảnh '{url}' không hợp lệ (phải bắt đầu bằng http:// hoặc https://).";
            }
        }

        return null;
    }

    private static void ThemAnhXe(Vehicle vehicle, List<string> urls)
    {
        if (urls.Count == 0) return;

        var thuTuBatDau = vehicle.HinhAnh.Count;
        var coAnhDaiDien = vehicle.HinhAnh.Any(i => i.LaAnhDaiDien);

        var index = 0;
        foreach (var url in urls)
        {
            vehicle.HinhAnh.Add(new VehicleImage
            {
                VehicleId = vehicle.Id,
                DuongDanAnh = url,
                ThuTu = thuTuBatDau + index,
                LaAnhDaiDien = !coAnhDaiDien && index == 0
            });
            index++;
        }
    }
}