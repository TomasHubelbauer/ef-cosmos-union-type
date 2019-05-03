using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

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
        await appDbContext.Availabilities.AddAsync(new Availability
        {
          DateAndTime = DateTime.Today,
          Recurrence = new Recurrence
          {
            Monday = true,
            Unrecurrence = new UnrecurrenceAtIndex { RecurrenceIndex = 10 },
            Exceptions = new Exception0[] {
              new ExceptionByDate { DateAndTime = DateTime.Today },
              new ExceptionAtIndex { RecurrenceIndex = 2, SlotIndex = 1 },
            },
          },
        });

        await appDbContext.SaveChangesAsync();
        var availability = appDbContext.Availabilities.ToArray();
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

    public static readonly LoggerFactory LoggerFactory = new LoggerFactory(new[] { new ConsoleLoggerProvider((_, __) => true, true) });

    public DbSet<Availability> Availabilities { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      //optionsBuilder.UseCosmos("https://localhost:8081", this.primaryKey, nameof(ef_cosmos_union_type));
      optionsBuilder.UseSqlServer($@"Server=(localdb)\{nameof(ef_cosmos_union_type)};Database={nameof(ef_cosmos_union_type)};");

      optionsBuilder.UseLoggerFactory(LoggerFactory);
      optionsBuilder.EnableDetailedErrors();
      optionsBuilder.EnableSensitiveDataLogging();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<UnrecurrenceAtIndex>().HasBaseType<Unrecurrence>();
      modelBuilder.Entity<UnrecurrenceByDate>().HasBaseType<Unrecurrence>();
      modelBuilder.Entity<ExceptionAtIndex>().HasBaseType<Exception0>();
      modelBuilder.Entity<ExceptionByDate>().HasBaseType<Exception0>();
      modelBuilder.Entity<Availability>().OwnsOne(a => a.Recurrence);
      modelBuilder.Entity<Recurrence>().OwnsOne(a => a.Unrecurrence);
      modelBuilder.Entity<Recurrence>().OwnsMany(a => a.Exceptions);
      modelBuilder.Entity<Unrecurrence>().HasKey(a => a.Id);
      modelBuilder.Entity<Exception0>().HasKey(a => a.Id);
    }
  }

  public sealed class Availability
  {
    public Guid Id { get; set; }
    public DateTime DateAndTime { get; set; }
    public Recurrence Recurrence { get; set; }
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
    public ICollection<Exception0> Exceptions { get; set; }
  }

  public class Unrecurrence
  {
    public Guid Id { get; set; }
  }

  public sealed class UnrecurrenceByDate : Unrecurrence
  {
    public Guid Id { get; set; }
    public DateTime DateAndTime { get; set; }
  }

  public sealed class UnrecurrenceAtIndex : Unrecurrence
  {
    public Guid Id { get; set; }
    public int RecurrenceIndex { get; set; }
  }

  public class Exception0
  {
    public Guid Id { get; set; }
  }

  public sealed class ExceptionByDate : Exception0
  {
    public Guid Id { get; set; }
    public DateTime DateAndTime { get; set; }
  }

  public sealed class ExceptionAtIndex : Exception0
  {
    public Guid Id { get; set; }
    public int RecurrenceIndex { get; set; }
    public int SlotIndex { get; set; }
  }
}
