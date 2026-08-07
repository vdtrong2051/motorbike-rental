using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace RentalPlatform.Extensions;

public static class EnumExtensions
{
    // Lấy text từ [Display(Name = "...")] gắn trên giá trị enum để hiển thị ra giao diện
    // (VD: TrangThaiXe.SanSang -> "Sẵn sàng"). Nếu enum không có attribute này thì trả về
    // ToString() mặc định để không bị lỗi/rỗng.
    public static string GetDisplayName(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        var displayAttribute = member?.GetCustomAttribute<DisplayAttribute>();
        return displayAttribute?.Name ?? value.ToString();
    }
}