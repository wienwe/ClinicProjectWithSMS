namespace PolyclinicApp
{
    public class UserInfo
    {
        public required string FullName { get; set; }
        public required string Phone { get; set; }
    }

    public class AppointmentInfo
    {
        public required int AppointmentId { get; set; }
        public required string DoctorName { get; set; }
        public required string Specialization { get; set; }
        public required string AppointmentTime { get; set; }
    }

    public class Doctor
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Specialization { get; set; }
    }

    public class ScheduleSlot
    {
        public int Id { get; set; }
        public required string Time { get; set; }
    }
}