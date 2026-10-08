using System;
using System.Collections.Generic;

namespace PhoneBook.CoreLib.Models;

public class Person
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public required string Name { get; set; }

    public List<BaseContact> Contacts { get; set; } = [];
}