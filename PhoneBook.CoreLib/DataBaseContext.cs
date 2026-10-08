using System.Collections.Generic;
using System.Text;

using Microsoft.EntityFrameworkCore;

using PhoneBook.CoreLib.Models;

namespace PhoneBook.CoreLib;

public sealed class DataBaseContext : DbContext
{
    public DbSet<BaseContact> Contacts => Set<BaseContact>();
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<Phone> Phones => Set<Phone>();
    public DbSet<Address> Addresses => Set<Address>();

    public DataBaseContext(DbContextOptions<DataBaseContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ──────────────────────────────────────────────────────────────
        // 1. TPH: одна таблица table_contacts + дискриминатор ContactType
        // ──────────────────────────────────────────────────────────────
        modelBuilder.Entity<BaseContact>(entity =>
        {
            entity.ToTable("table_contacts");
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Id).ValueGeneratedNever();

            entity.HasDiscriminator<string>("ContactType")
                  .HasValue<Phone>(nameof(Phone))
                  .HasValue<Address>(nameof(Address));

            entity.Property<string>("ContactType")
                  .IsRequired()
                  .HasMaxLength(32);
        });

        // ──────────────────────────────────────────────────────────────
        // 2. Свойства подтипов (в TPH столбцы будут NULL для "чужих" типов)
        // ──────────────────────────────────────────────────────────────
        modelBuilder.Entity<Phone>(entity =>
        {
            entity.Property(p => p.Number).IsRequired().HasMaxLength(32);
            entity.Property(p => p.Type).HasConversion<int>();
        });

        modelBuilder.Entity<Address>(entity =>
        {
            entity.Property(a => a.Location).IsRequired().HasMaxLength(256);
            entity.Property(a => a.Type).HasConversion<int>();
        });

        // ──────────────────────────────────────────────────────────────
        // 3. Person
        // ──────────────────────────────────────────────────────────────
        modelBuilder.Entity<Person>(entity =>
        {
            entity.ToTable("table_persons");
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Id).ValueGeneratedNever();
            entity.Property(p => p.Name).IsRequired().HasMaxLength(128);

            // ──────────────────────────────────────────────────────────
            // 4. many-to-many Person <-> BaseContact через
            //    table_person_contacts (person_id, contact_id).
            //    Каскад выключен явно через OnDelete(Restrict).
            // ──────────────────────────────────────────────────────────
            entity.HasMany(p => p.Contacts)
                  .WithMany(c => c.Persons)
                  .UsingEntity<Dictionary<string, object>>(
                      "PersonContact",
                      // Первая лямбда — сторона HasMany (BaseContact)
                      j => j.HasOne<BaseContact>()
                            .WithMany()
                            .HasForeignKey("contact_id")
                            .OnDelete(DeleteBehavior.Restrict),
                      // Вторая лямбда — сторона WithMany (Person)
                      j => j.HasOne<Person>()
                            .WithMany()
                            .HasForeignKey("person_id")
                            .OnDelete(DeleteBehavior.Restrict),
                      j =>
                      {
                          j.ToTable("table_person_contacts");
                          j.HasKey("person_id", "contact_id");
                      });
        });

        // ──────────────────────────────────────────────────────────────
        // 5. Глобальные правила:
        //    - все FK — без каскадного удаления (Restrict);
        //    - все столбцы — snake_case.
        //    Делается ПОСЛЕ явных конфигураций, чтобы переименовать
        //    и дискриминатор, и FK-столбцы join-таблицы.
        // ──────────────────────────────────────────────────────────────
        DisableCascadeDelete(modelBuilder);
        ApplySnakeCaseColumns(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    // ──────────────────────────────────────────────────────────────────
    // Запрет каскадного удаления для всех внешних ключей модели.
    // DeleteBehavior.Restrict вместо Cascade/SetNull.
    // ──────────────────────────────────────────────────────────────────
    private static void DisableCascadeDelete(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var foreignKey in entityType.GetForeignKeys())
            {
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // Приведение имён столбцов к snake_case.
    // Имена свойств (Id, ContactType, Number, ...) не трогаем —
    // только имена столбцов, чтобы код оставался в PascalCase.
    // ──────────────────────────────────────────────────────────────────
    private static void ApplySnakeCaseColumns(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                var columnName = property.GetColumnName();
                if (!string.IsNullOrEmpty(columnName))
                {
                    property.SetColumnName(ToSnakeCase(columnName));
                }
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────
    // Преобразование PascalCase / camelCase в snake_case.
    //   "Id"           -> "id"
    //   "ContactType"  -> "contact_type"
    //   "PersonId"     -> "person_id"
    //   "person_id"    -> "person_id" (без изменений)
    // ──────────────────────────────────────────────────────────────────
    private static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        var builder = new StringBuilder(name.Length + 8);

        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];

            if (char.IsUpper(c))
            {
                var hasPrevious = i > 0;
                var previousIsLower = hasPrevious && !char.IsUpper(name[i - 1]);
                var previousIsUpper = hasPrevious && char.IsUpper(name[i - 1]);
                var nextIsLower = i + 1 < name.Length && !char.IsUpper(name[i + 1]);

                // Граница: XxxYyy -> xxx_yyy
                // или: XXXYyy -> xxx_yyy
                if (previousIsLower || (previousIsUpper && nextIsLower))
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(c));
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }
}