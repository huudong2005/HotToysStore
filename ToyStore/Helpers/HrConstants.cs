namespace ToyStore.Helpers;

public static class HrConstants
{
    public const decimal SalaryPerCompletedShift = 200_000m;

    public const string ShiftMorning = "Sáng";
    public const string ShiftAfternoon = "Chiều";
    public const string ShiftEvening = "Tối";

    public const string StatusPending = "Chưa chấm công";
    public const string StatusCompleted = "Đã hoàn thành";
    public const string StatusAbsent = "Vắng mặt";

    public const string PayrollStatusFinalized = "Đã chốt";

    public static readonly string[] ShiftNames =
    {
        ShiftMorning,
        ShiftAfternoon,
        ShiftEvening
    };

    public static readonly string[] AttendanceStatuses =
    {
        StatusPending,
        StatusCompleted,
        StatusAbsent
    };

    public static readonly string[] UpdatableAttendanceStatuses =
    {
        StatusCompleted,
        StatusAbsent
    };
}
