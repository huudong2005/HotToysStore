namespace ToyStore.Services;

/// <summary>
/// Dữ liệu địa chỉ mẫu khi Token GHN không hợp lệ / API không phản hồi (chỉ để dev & demo UI).
/// Cập nhật Token thật tại appsettings → Ghn:Token để dùng API GHN chính thức.
/// </summary>
internal static class GhnMasterDataFallback
{
    private static readonly IReadOnlyList<GhnProvinceDto> Provinces =
    [
        new() { ProvinceId = 201, ProvinceName = "Hà Nội" },
        new() { ProvinceId = 202, ProvinceName = "Hồ Chí Minh" },
        new() { ProvinceId = 203, ProvinceName = "Đà Nẵng" }
    ];

    private static readonly Dictionary<int, IReadOnlyList<GhnDistrictDto>> DistrictsByProvince = new()
    {
        [201] =
        [
            new() { DistrictId = 1485, ProvinceId = 201, DistrictName = "Quận Ba Đình" },
            new() { DistrictId = 1486, ProvinceId = 201, DistrictName = "Quận Hoàn Kiếm" },
            new() { DistrictId = 1490, ProvinceId = 201, DistrictName = "Quận Cầu Giấy" }
        ],
        [202] =
        [
            new() { DistrictId = 1442, ProvinceId = 202, DistrictName = "Quận 1" },
            new() { DistrictId = 1444, ProvinceId = 202, DistrictName = "Quận 3" },
            new() { DistrictId = 1456, ProvinceId = 202, DistrictName = "Quận 7" },
            new() { DistrictId = 1451, ProvinceId = 202, DistrictName = "Quận Bình Thạnh" }
        ],
        [203] =
        [
            new() { DistrictId = 1522, ProvinceId = 203, DistrictName = "Quận Hải Châu" },
            new() { DistrictId = 1523, ProvinceId = 203, DistrictName = "Quận Thanh Khê" }
        ]
    };

    private static readonly Dictionary<int, IReadOnlyList<GhnWardDto>> WardsByDistrict = new()
    {
        [1442] =
        [
            new() { WardCode = "21004", DistrictId = 1442, WardName = "Phường Bến Nghé" },
            new() { WardCode = "21006", DistrictId = 1442, WardName = "Phường Đa Kao" },
            new() { WardCode = "21008", DistrictId = 1442, WardName = "Phường Bến Thành" }
        ],
        [1444] =
        [
            new() { WardCode = "21010", DistrictId = 1444, WardName = "Phường 1" },
            new() { WardCode = "21011", DistrictId = 1444, WardName = "Phường 2" },
            new() { WardCode = "21012", DistrictId = 1444, WardName = "Phường 3" }
        ],
        [1456] =
        [
            new() { WardCode = "21040", DistrictId = 1456, WardName = "Phường Tân Hưng" },
            new() { WardCode = "21041", DistrictId = 1456, WardName = "Phường Tân Phú" }
        ],
        [1451] =
        [
            new() { WardCode = "21020", DistrictId = 1451, WardName = "Phường 1" },
            new() { WardCode = "21021", DistrictId = 1451, WardName = "Phường 2" }
        ],
        [1485] =
        [
            new() { WardCode = "1A0101", DistrictId = 1485, WardName = "Phường Phúc Xá" },
            new() { WardCode = "1A0102", DistrictId = 1485, WardName = "Phường Trúc Bạch" }
        ],
        [1486] =
        [
            new() { WardCode = "1A0201", DistrictId = 1486, WardName = "Phường Phúc Tân" },
            new() { WardCode = "1A0202", DistrictId = 1486, WardName = "Phường Đồng Xuân" }
        ],
        [1490] =
        [
            new() { WardCode = "1A0601", DistrictId = 1490, WardName = "Phường Dịch Vọng" },
            new() { WardCode = "1A0602", DistrictId = 1490, WardName = "Phường Nghĩa Tân" }
        ],
        [1522] =
        [
            new() { WardCode = "20301", DistrictId = 1522, WardName = "Phường Thạch Thang" },
            new() { WardCode = "20302", DistrictId = 1522, WardName = "Phường Hải Châu I" }
        ],
        [1523] =
        [
            new() { WardCode = "20310", DistrictId = 1523, WardName = "Phường Thanh Khê Tây" },
            new() { WardCode = "20311", DistrictId = 1523, WardName = "Phường Thanh Khê Đông" }
        ]
    };

    public static IReadOnlyList<GhnProvinceDto> GetProvinces() => Provinces;

    public static IReadOnlyList<GhnDistrictDto> GetDistricts(int provinceId)
    {
        return DistrictsByProvince.TryGetValue(provinceId, out var list)
            ? list
            : Array.Empty<GhnDistrictDto>();
    }

    public static IReadOnlyList<GhnWardDto> GetWards(int districtId)
    {
        return WardsByDistrict.TryGetValue(districtId, out var list)
            ? list
            : Array.Empty<GhnWardDto>();
    }
}
