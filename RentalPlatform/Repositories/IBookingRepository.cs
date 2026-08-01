using RentalPlatform.Models.Entities;

namespace RentalPlatform.Repositories;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(Guid id);
    Task<Booking> AddAsync(Booking booking);
    Task UpdateAsync(Booking booking);

    // Trả về các booking của cùng 1 xe, đang ở trạng thái DaXacNhan hoặc DangThue,
    // có khoảng ngày giao với [tuNgay, denNgay]. Dùng cho KiemTraConTrongAsync.
    Task<List<Booking>> GetOverlappingAsync(Guid vehicleId, DateOnly tuNgay, DateOnly denNgay, Guid? loaiTruBookingId = null);

    Task<List<Booking>> GetByCustomerAsync(Guid customerId);

    // Danh sách booking của tất cả xe thuộc 1 Owner (để Owner duyệt/từ chối)
    Task<List<Booking>> GetByOwnerVehiclesAsync(Guid ownerId);
}
