# EF Cosmos Union Type

This is not really a union type, but I didn't know what else to call it.

1. `dotnet new console`
2. `dotnet add package Microsoft.EntityFrameworkCore.Cosmos`
3. `<LangVersion>latest</LangVersion>` for `async Task Main`
4. Ensure the Cosmos emulator is running
5. Download the Cosmos key from the running instance to avoid versioning it
6. Wire up the model as follows:

```csharp
public sealed class Recurrence {
  public Guid Id { get; set; }
  public bool Monday { get; set; }
  public bool Tuesday { get; set; }
  public bool Wednesday { get; set; }
  public bool Thursday { get; set; }
  public bool Friday { get; set; }
  public bool Saturday { get; set; }
  public bool Sunday { get; set; }
  public Unrecurrence Unrecurrence { get; set; }
  public Exception[] Exceptions { get; set; }
}

public abstract class Unrecurrence {
  public Guid Id { get; set; }
}

public sealed class UnrecurrenceByDate: Unrecurrence {
  public DateTime DateAndTime { get; set; }
}

public sealed class UnrecurrenceAtIndex: Unrecurrence {
  public int RecurrenceIndex { get; set; }
}

public abstract class Exception {
  public Guid Id { get; set; }
}

public sealed class ExceptionByDate: Exception {
  public DateTime DateAndTime { get; set; }
}

public sealed class ExceptionAtIndex: Exception {
  public int RecurrenceIndex { get; set; }
  public int SlotIndex { get; set; }
}
```

7. Create EF context using the Cosmos key as follows:

```csharp
public class AppDbContext: DbContext
{
  private string primaryKey;

  public AppDbContext(string primaryKey)
  {
    this.primaryKey = primaryKey;
  }

  public DbSet<Recurrence> Recurrences { get; set; }
  protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
  {
    optionsBuilder.UseCosmos("https://localhost:8081", this.primaryKey, nameof(ef_cosmos_union_type));
  }
}
```

8. Try the naive approach:

```csharp
using (var appDbContext = new AppDbContext(primaryKey))
{
  await appDbContext.Recurrences.AddAsync(new Recurrence
  {
      Monday = true,
      Unrecurrence = new UnrecurrenceAtIndex { RecurrenceIndex = 10, },
      Exceptions = new Exception[] {
          new ExceptionByDate { DateAndTime = DateTime.Today },
          new ExceptionAtIndex { RecurrenceIndex = 2, SlotIndex = 1 },
      },
  });
}
```

The key is to use `HasBaseType` in `OnModelCreating`. See updated source code.
