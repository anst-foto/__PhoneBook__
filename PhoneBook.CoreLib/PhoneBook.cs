using System.Collections.Generic;
using System.Linq;

using Microsoft.EntityFrameworkCore;

using PhoneBook.CoreLib.Models;

namespace PhoneBook.CoreLib;

public class PhoneBook
{
    private DataBaseContext _db;

    public PhoneBook(string connectionString)
    {
        var options = new DbContextOptionsBuilder<DataBaseContext>()
            .UseSqlite(connectionString)
            .Options;
        
        _db = new DataBaseContext(options);
    }
    
    public IEnumerable<Person> GetAllPersons() => _db.Persons;

    public void AddPerson(Person person)
    {
        _db.Persons.Add(person);
        _db.SaveChanges();
    }

    public void RemovePerson(Person person)
    {
        var _person = _db.Persons.SingleOrDefault(p => p.Id == person.Id);
        _db.Persons.Remove(_person);
    }

    public void UpdatePerson(Person person)
    {
        var _person = _db.Persons.SingleOrDefault(p => p.Id == person.Id);
        _db.Persons.Update();
    }
}