namespace LRP.Models;

public class Lab
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<Computer> Computers { get; set; } = new();
}

public class Computer
{
    public int Id { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Processor { get; set; } = string.Empty;
    public string Ram { get; set; } = string.Empty;
    public bool HasHdmi { get; set; }
    public bool HasVeyon { get; set; }
    public int LabId { get; set; }
    public Lab? Lab { get; set; }
    public int? AssignedUserId { get; set; }
    public UserAccount? AssignedUser { get; set; }
}

public class UserAccount
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? StudentNumber { get; set; }
    public string? FullName { get; set; }
    public Computer? AssignedComputer { get; set; }
}
