namespace PhoneBook.CoreLib.Models;

public enum AddressType
{
    Home,
    Work,
    Other
}

public sealed class Address : BaseContact
{
    public AddressType Type { get; set; }

    public required string Location { get; set; }
}