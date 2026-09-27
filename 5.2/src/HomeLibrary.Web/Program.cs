using HomeLibrary.Web.Data;
using HomeLibrary.Web.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("HomeLibrary")
    ?? throw new InvalidOperationException("Не задана строка подключения \"HomeLibrary\".");

builder.Services.AddSingleton(new DbConnectionFactory(connectionString));
builder.Services.AddScoped<BookService>();
builder.Services.AddScoped<DatabaseInitializer>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Загрузка демонстрационных данных при первом запуске (если таблица пуста).
await using (var scope = app.Services.CreateAsyncScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.SeedAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();

// Корневой маршрут ведёт к списку книг.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Books}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();