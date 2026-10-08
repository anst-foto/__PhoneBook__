using System;
using System.Collections.Generic;

namespace PhoneBook.CoreLib.Models;

public abstract class BaseContact
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public List<Person> Persons { get; set; } = [];
}