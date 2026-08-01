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

        vehicle = await _repository.AddAsync(vehicle);

        if (!string.IsNullOrWhiteSpace(model.ImageUrls))
        {
            var (ok, error, newImages) = TaoAnhXeTuLinkAsync(vehicle, model.ImageUrls);
            if (!ok) return VehicleOperationResult.Fail(error!);
            await _repository.AddImagesAsync(newImages);
        }

        return VehicleOperationResult.Ok(vehicle);
    }

    public async Task<VehicleOperationResult> UpdateAsync(VehicleEditViewModel model, Guid currentUserId)
    {
        var vehicle = await _repository.GetByIdAsync(model.Id);
        if (vehicle is null) return VehicleOperationResult.Fail("Không tìm thấy xe.");
        if (vehicle.OwnerId != currentUserId) return VehicleOperationResult.Fail("Bạn không có quyền sửa xe này.");

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

        await _repository.UpdateAsync(vehicle);

        if (!string.IsNullOrWhiteSpace(model.NewImageUrls))
        {
            var (ok, error, newImages) = TaoAnhXeTuLinkAsync(vehicle, model.NewImageUrls);
            if (!ok) return VehicleOperationResult.Fail(error!);
            await _repository.AddImagesAsync(newImages);
        }

        return VehicleOperationResult.Ok(vehicle);
    }

    public async Task<VehicleOperationResult> DeleteAsync(Guid vehicleId, Guid currentUserId)
    {
        var vehicle = await _repository.GetByIdAsync(vehicleId);
        if (vehicle is null) return VehicleOperationResult.Fail("Không tìm thấy xe.");
        if (vehicle.OwnerId != currentUserId) return VehicleOperationResult.Fail("Bạn không có quyền xoá xe này.");
        if (vehicle.TrangThai == TrangThaiXe.DangThue)
            return VehicleOperationResult.Fail("Không thể xoá xe đang trong thời gian cho thuê.");

        // Ảnh giờ chỉ là link ngoài (không lưu file vật lý trên server), nên không cần
        // xoá thư mục uploads nữa. EF sẽ tự xoá các bản ghi VehicleImage liên quan qua
        // cascade delete khi Vehicle bị xoá.
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

        // Ảnh là link ngoài, không có file vật lý trên server để xoá — chỉ cần gỡ bản ghi.
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

    // Tách chuỗi link (mỗi dòng 1 link) do người dùng nhập, validate định dạng URL cơ bản,
    // và tạo các VehicleImage tương ứng. Không tải ảnh về server — chỉ lưu trực tiếp link.
    private (bool Success, string? Error, List<VehicleImage> NewImages) TaoAnhXeTuLinkAsync(Vehicle vehicle, string rawUrls)
    {
        var newImages = new List<VehicleImage>();

        var links = rawUrls
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();

        if (links.Count == 0) return (true, null, newImages);

        var thuTuBatDau = vehicle.HinhAnh.Count;
        var coAnhDaiDien = vehicle.HinhAnh.Any(i => i.LaAnhDaiDien);

        for (var i = 0; i < links.Count; i++)
        {
            var link = links[i];

            // Chỉ chấp nhận URL tuyệt đối, giao thức http/https — chặn javascript:, data:, file:...
            // để tránh chèn link độc hại vào thuộc tính src.
            if (!Uri.TryCreate(link, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return (false, $"Link '{link}' không hợp lệ. Link ảnh phải bắt đầu bằng http:// hoặc https://.", newImages);
            }

            var image = new VehicleImage
            {
                VehicleId = vehicle.Id,
                DuongDanAnh = link,
                ThuTu = thuTuBatDau + i,
                LaAnhDaiDien = !coAnhDaiDien && i == 0
            };

            // Vẫn thêm vào vehicle.HinhAnh để object trong bộ nhớ nhất quán ngay sau khi
            // gọi, nhưng việc LƯU vào DB được thực hiện tường minh qua
            // repository.AddImagesAsync(newImages) — không dựa vào EF tự dò tìm entity
            // mới qua navigation (xem giải thích trong VehicleRepository.AddImagesAsync).
            vehicle.HinhAnh.Add(image);
            newImages.Add(image);
        }

        return (true, null, newImages);
    }
}
