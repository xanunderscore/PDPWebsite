namespace PDPWebsite.Models;

public record Schedule(Guid Id, string Name, ulong HostId, TimeSpan Duration, DateTime At)
{
    public List<SignUp> Signups { get; set; } // unused due to cross server requirement not being met
}

public record SignUp(Guid Id, Guid ScheduleId, ulong UserId, string Name, bool IsHost, bool IsBackup, bool IsConfirmed) //unused due to cross server requirement not being met
{
    public Schedule Schedule { get; set; }
}