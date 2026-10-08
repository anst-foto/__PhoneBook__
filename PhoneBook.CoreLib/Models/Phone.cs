namespace PhoneBook.CoreLib.Models;

public enum PhoneType
{
    Mobile,
    Home,
    Work
}

public sealed class Phone : BaseContact
{
    public PhoneType Type { get; set; }

    public required string Number { get; set; }
}