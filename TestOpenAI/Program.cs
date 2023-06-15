using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using TestOpenAI.Data;
using TestOpenAI.Models;

var builder = WebApplication.CreateBuilder(args);



// Add services to the container.
builder.Services.Configure<AOAISettings>(builder.Configuration.GetSection(nameof(AOAISettings)));
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
//builder.Services.AddSingleton<WeatherForecastService>();

await PyProcessors.PySetup.Initialize((s) => { Console.WriteLine(s); });

var app = builder.Build();
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

await app.RunAsync();
