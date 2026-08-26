using Microsoft.EntityFrameworkCore;
using Vromonsathi.Data;
AppContext.SetSwitch("Switch.Microsoft.Data.SqlClient.UseManagedNetworkingOnWindows", true);

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Session (needed for manual authentication)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// HttpContextAccessor so we can read session from anywhere (helpers, views)
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Vromonsathi.Services.IBudgetCalculatorService, Vromonsathi.Services.BudgetCalculatorService>();

var app = builder.Build();

app.UseDeveloperExceptionPage();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();          // must be before UseAuthorization / MapControllerRoute
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed default Admin account + sample data
// TEMP: entire block disabled to isolate whether Microsoft.Data.SqlClient loading
// itself is the problem on Somee, or just the DB connection at migrate/query time.

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();

    if (!context.Users.Any(u => u.Role == "Admin"))
    {
        Vromonsathi.Helpers.PasswordHelper.CreatePasswordHash("Admin@123", out string hash, out string salt);

        var admin = new Vromonsathi.Models.User
        {
            FullName = "System Admin",
            Email = "admin@vromonsathi.com",
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = "Admin",
            IsActive = true
        };

        context.Users.Add(admin);
        context.SaveChanges();
    }

    if (!context.Destinations.Any())
    {
        var bandarban = new Vromonsathi.Models.Destination
        {
            Name = "Bandarban",
            NameBn = "বান্দরবান",
            District = "Bandarban",
            Division = "Chattogram",
            Category = "Hill & Mountain",
            Description = "Nilgiri ridge and Boga Lake in the Chittagong Hill Tracts.",
            EntryFee = 100,
            BestTimeToVisit = "October to February",
            Latitude = 21.7987,
            Longitude = 92.3616,
            NearestViewpointName = "Nilgiri Ridge",
            NearestViewpointDistanceKm = 27,
            RequiresConvoyEscort = false,
            IsApproved = true,
            Checkpoints = new List<Vromonsathi.Models.Checkpoint>
{
                new Vromonsathi.Models.Checkpoint { Type = Vromonsathi.Models.CheckpointType.Army, Name = "Army Camp - Bandarban Entry", SequenceOrder = 1, DistanceFromStartKm = 4, Notes = "ID registration required" },
                new Vromonsathi.Models.Checkpoint { Type = Vromonsathi.Models.CheckpointType.BGB, Name = "BGB Camp - Ruma Road", SequenceOrder = 2, DistanceFromStartKm = 14, Notes = "Group manifest checked" },
                new Vromonsathi.Models.Checkpoint { Type = Vromonsathi.Models.CheckpointType.Police, Name = "Police Check - Nilgiri Road", SequenceOrder = 3, DistanceFromStartKm = 22 },
                new Vromonsathi.Models.Checkpoint { Type = Vromonsathi.Models.CheckpointType.Army, Name = "Army Camp - Nilgiri", SequenceOrder = 4, DistanceFromStartKm = 27, Notes = "Final checkpoint before viewpoint" }
            },
            EmergencyContacts = new List<Vromonsathi.Models.EmergencyContact>
{
                new Vromonsathi.Models.EmergencyContact { Type = Vromonsathi.Models.EmergencyContactType.BGB, Name = "Nearest BGB Camp", Phone = "01769-000000" },
                new Vromonsathi.Models.EmergencyContact { Type = Vromonsathi.Models.EmergencyContactType.Hospital, Name = "Bandarban Sadar Hospital", Phone = "01711-000000" }
            }
        };

        var sajek = new Vromonsathi.Models.Destination
        {
            Name = "Sajek Valley",
            NameBn = "সাজেক ভ্যালি",
            District = "Rangamati",
            Division = "Chattogram",
            Category = "Hill & Mountain",
            Description = "Cloud-line ridge villages, reached via convoy.",
            EntryFee = 150,
            BestTimeToVisit = "June to September",
            Latitude = 23.3833,
            Longitude = 92.2833,
            NearestViewpointName = "Konglak Pahar",
            NearestViewpointDistanceKm = 65,
            RequiresConvoyEscort = true,
            IsApproved = true,
            Checkpoints = new List<Vromonsathi.Models.Checkpoint>
{
                new Vromonsathi.Models.Checkpoint { Type = Vromonsathi.Models.CheckpointType.BGB, Name = "BGB - Dighinala", SequenceOrder = 1, DistanceFromStartKm = 20, Notes = "Convoy assembly point" },
                new Vromonsathi.Models.Checkpoint { Type = Vromonsathi.Models.CheckpointType.Army, Name = "Army - Baghaihat", SequenceOrder = 2, DistanceFromStartKm = 38, Notes = "Convoy departure time posted" },
                new Vromonsathi.Models.Checkpoint { Type = Vromonsathi.Models.CheckpointType.BGB, Name = "BGB - Machalong", SequenceOrder = 3, DistanceFromStartKm = 52 },
                new Vromonsathi.Models.Checkpoint { Type = Vromonsathi.Models.CheckpointType.Police, Name = "Police - Baghaihat Rd", SequenceOrder = 4, DistanceFromStartKm = 60 }
            },
            EmergencyContacts = new List<Vromonsathi.Models.EmergencyContact>
{
                new Vromonsathi.Models.EmergencyContact { Type = Vromonsathi.Models.EmergencyContactType.Army, Name = "Baghaihat Army Camp", Phone = "01769-111111" }
            }
        };

        var sundarbans = new Vromonsathi.Models.Destination
        {
            Name = "Sundarbans",
            NameBn = "সুন্দরবন",
            District = "Khulna",
            Division = "Khulna",
            Category = "Forest & Wildlife",
            Description = "The largest mangrove forest in the world and home to the Royal Bengal Tiger.",
            EntryFee = 500,
            BestTimeToVisit = "November to February",
            Latitude = 22.1667,
            Longitude = 89.2000,
            NearestViewpointName = "Kotka Watch Tower",
            NearestViewpointDistanceKm = 42,
            IsApproved = true,
            Checkpoints = new List<Vromonsathi.Models.Checkpoint>
{
                new Vromonsathi.Models.Checkpoint { Type = Vromonsathi.Models.CheckpointType.Police, Name = "Police Outpost - Mongla", SequenceOrder = 1, DistanceFromStartKm = 8 }
            }
        };

        var coxsbazar = new Vromonsathi.Models.Destination
        {
            Name = "Cox's Bazar Beach",
            District = "Cox's Bazar",
            Division = "Chattogram",
            Category = "Beach",
            Description = "World's longest natural sea beach, stretching over 120 km along the Bay of Bengal.",
            EntryFee = 0,
            BestTimeToVisit = "November to March",
            Latitude = 21.4272,
            Longitude = 92.0058,
            NearestViewpointName = "Himchari Hilltop",
            NearestViewpointDistanceKm = 18,
            IsApproved = true,
            Checkpoints = new List<Vromonsathi.Models.Checkpoint>
{
                new Vromonsathi.Models.Checkpoint { Type = Vromonsathi.Models.CheckpointType.Police, Name = "Tourist Police - Marine Drive", SequenceOrder = 1, DistanceFromStartKm = 6 },
                new Vromonsathi.Models.Checkpoint { Type = Vromonsathi.Models.CheckpointType.Police, Name = "Tourist Police - Himchari", SequenceOrder = 2, DistanceFromStartKm = 18 }
            }
        };

        var sylhet = new Vromonsathi.Models.Destination
        {
            Name = "Jaflong",
            NameBn = "জাফলং",
            District = "Sylhet",
            Division = "Sylhet",
            Category = "Waterfall",
            Description = "Border river and stone hills, with rolling tea gardens nearby.",
            EntryFee = 0,
            BestTimeToVisit = "September to March",
            Latitude = 25.1622,
            Longitude = 92.0089,
            NearestViewpointName = "Jaflong Zero Point",
            NearestViewpointDistanceKm = 62,
            IsApproved = true,
            Checkpoints = new List<Vromonsathi.Models.Checkpoint>
{
                new Vromonsathi.Models.Checkpoint { Type = Vromonsathi.Models.CheckpointType.BGB, Name = "BGB Border Post - Jaflong", SequenceOrder = 1, DistanceFromStartKm = 62 }
            }
        };

        context.Destinations.AddRange(bandarban, sajek, sundarbans, coxsbazar, sylhet);
        context.SaveChanges();
    }

    if (!context.Facilities.Any())
    {
        context.Facilities.AddRange(
            new Vromonsathi.Models.Facility { Key = Vromonsathi.Models.FacilityType.Hotel, NameEn = "Hotel / resort", DefaultPrice = 1200, Unit = Vromonsathi.Models.PricingUnit.PerNight, Description = "Accommodation for the group" },
            new Vromonsathi.Models.Facility { Key = Vromonsathi.Models.FacilityType.Guide, NameEn = "Local tour guide", DefaultPrice = 1800, Unit = Vromonsathi.Models.PricingUnit.PerDay, Description = "Licensed local guide" },
            new Vromonsathi.Models.Facility { Key = Vromonsathi.Models.FacilityType.Transport, NameEn = "Private transport", DefaultPrice = 3500, Unit = Vromonsathi.Models.PricingUnit.PerDay, Description = "Car / CHT-permitted jeep" },
            new Vromonsathi.Models.Facility { Key = Vromonsathi.Models.FacilityType.Meals, NameEn = "Meal package", DefaultPrice = 600, Unit = Vromonsathi.Models.PricingUnit.PerDayPerPerson, Description = "Local set-meal plan" },
            new Vromonsathi.Models.Facility { Key = Vromonsathi.Models.FacilityType.Sim, NameEn = "Local SIM / data", DefaultPrice = 150, Unit = Vromonsathi.Models.PricingUnit.PerPerson, Description = "Connectivity pack" },
            new Vromonsathi.Models.Facility { Key = Vromonsathi.Models.FacilityType.Insurance, NameEn = "Travel insurance", DefaultPrice = 250, Unit = Vromonsathi.Models.PricingUnit.PerPerson, Description = "Per-traveler coverage" }
        );
        context.SaveChanges();
    }

    if (!context.EmergencyContacts.Any(c => c.DestinationId == null))
    {
        context.EmergencyContacts.AddRange(
            new Vromonsathi.Models.EmergencyContact { Type = Vromonsathi.Models.EmergencyContactType.TouristPolice, Name = "Tourist Police Helpline", Phone = "999" },
            new Vromonsathi.Models.EmergencyContact { Type = Vromonsathi.Models.EmergencyContactType.Police, Name = "National Emergency Service", Phone = "999" }
        );
        context.SaveChanges();
    }
}


app.Run();