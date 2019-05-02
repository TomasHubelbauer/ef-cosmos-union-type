using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ef_cosmos_union_type
{
  class Program
  {
    static async Task Main(string[] args)
    {
      string primaryKey;
      try
      {
        Console.WriteLine("Downloading the primary key from https://localhost:8081/_explorer/quickstart.html…");
        var html = await new HttpClient().GetStringAsync("https://localhost:8081/_explorer/quickstart.html");
        primaryKey = Regex.Match(html, "Primary Key</p>\\s+<input .* value=\"(?<primaryKey>.*)\"").Groups["primaryKey"].Value;
        Console.WriteLine("The primary key has been downloaded.");
      }
      catch
      {
        Console.WriteLine("Failed to download the primary key. Make sure to install and run the Cosmos emulator.");
        Console.WriteLine("The primary key gets downloaded from https://localhost:8081/_explorer/quickstart.html");
        return;
      }

      using (var appDbContext = new AppDbContext(primaryKey))
      {
        await appDbContext.Database.EnsureDeletedAsync();
        await appDbContext.Database.EnsureCreatedAsync();
        await appDbContext.Recurrences.AddAsync(new Recurrence
        {
          Monday = true,
          Unrecurrence = new UnrecurrenceAtIndex { RecurrenceIndex = 10 },
          Exceptions = new Exception[] {
            new ExceptionByDate { DateAndTime = DateTime.Today },
            new ExceptionAtIndex { RecurrenceIndex = 2, SlotIndex = 1 },
          },
        });

        await appDbContext.SaveChangesAsync();
        var recurrence = await appDbContext.Recurrences.SingleOrDefaultAsync();
        // Place debugger here
      }
    }
  }

  public class AppDbContext : DbContext
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<UnrecurrenceAtIndex>().HasBaseType<Unrecurrence>();
      modelBuilder.Entity<UnrecurrenceByDate>().HasBaseType<Unrecurrence>();
      modelBuilder.Entity<ExceptionAtIndex>().HasBaseType<Exception>();
      modelBuilder.Entity<ExceptionByDate>().HasBaseType<Exception>();
    }
  }

  public sealed class Recurrence
  {
    public Guid Id { get; set; }
    public bool Monday { get; set; }
    public bool Tuesday { get; set; }
    public bool Wednesday { get; set; }
    public bool Thursday { get; set; }
    public bool Friday { get; set; }
    public bool Saturday { get; set; }
    public bool Sunday { get; set; }
    public Unrecurrence Unrecurrence { get; set; }
    public ICollection<Exception> Exceptions { get; set; }
  }

  public abstract class Unrecurrence
  {
    public Guid Id { get; set; }
  }

  public sealed class UnrecurrenceByDate : Unrecurrence
  {
    public DateTime DateAndTime { get; set; }
  }

  public sealed class UnrecurrenceAtIndex : Unrecurrence
  {
    public int RecurrenceIndex { get; set; }
  }

  public abstract class Exception
  {
    public Guid Id { get; set; }
  }

  public sealed class ExceptionByDate : Exception
  {
    public DateTime DateAndTime { get; set; }
  }

  public sealed class ExceptionAtIndex : Exception
  {
    public int RecurrenceIndex { get; set; }
    public int SlotIndex { get; set; }
  }
}
