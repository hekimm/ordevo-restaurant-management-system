using Microsoft.AspNetCore.Authentication.Cookies;
using Ordevo.Web.Api;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.Configure<OrdevoApiOptions>(builder.Configuration.GetSection(OrdevoApiOptions.SectionName));

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "ordevo.web";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/denied";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(10);
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Login");
    options.Conventions.AllowAnonymousToPage("/Denied");
});

builder.Services.AddHttpClient<OrdevoApiClient>((services, client) =>
{
    var options = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<OrdevoApiOptions>>().Value;
    client.BaseAddress = options.BaseUrl;
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

var app = builder.Build();

app.UseExceptionHandler("/Error");

if (!app.Environment.IsDevelopment())
    app.UseHsts();

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/ui-health", () => Results.Ok(new { service = "Ordevo.Web", status = "up" })).AllowAnonymous();
app.MapGet("/print", () => Results.Redirect("/printer")).RequireAuthorization();
app.MapGet("/settings", () => Results.Redirect("/ayarlar")).RequireAuthorization();
app.MapGet("/entegrasyon", () => Results.Redirect("/ayarlar?tab=integrations")).RequireAuthorization();
app.MapRazorPages();

app.Run();
