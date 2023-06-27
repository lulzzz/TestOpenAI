using JLBlazorComponents;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Options;
using PyProcessors;
using TestOpenAI.Data;
using TestOpenAI.Helpers;
using TestOpenAI.Models;
using TestOpenAI.Services;

var builder = WebApplication.CreateBuilder(args);

nint? pythonState = null;
try
{
    // Add services to the container.
    builder.Services.Configure<AOAISettings>(builder.Configuration.GetSection(nameof(AOAISettings)));
    builder.Services.AddRazorPages();
    builder.Services.AddServerSideBlazor();
    //builder.Services.AddSingleton<WeatherForecastService>();
    builder.Services.AddJLBlazorComponents();
    builder.Services.AddScoped<AppData>();

    builder.Services.AddScoped<PdfProcessor>((serviceProvider) =>
    {
        var aoaiSettings = serviceProvider.GetService<IOptionsMonitor<AOAISettings>>()?.CurrentValue;
        return new PdfProcessor(aoaiSettings?.AOAI_KEY, aoaiSettings?.AOAI_ENDPOINT, aoaiSettings?.AOAI_EMBEDDED_DEPLOYMENT_NAME);

    });
    builder.Services.AddScoped<FAISSChatProcessor>((serviceProvider) =>
    {
        var aoaiSettings = serviceProvider.GetService<IOptionsMonitor<AOAISettings>>()?.CurrentValue;
        return new FAISSChatProcessor(aoaiSettings?.AOAI_KEY, aoaiSettings?.AOAI_ENDPOINT, aoaiSettings?.AOAI_CHAT_DEPLOYMENT_NAME, aoaiSettings?.AOAI_CHAT_DEPLOYMENT_MODEL, aoaiSettings?.AOAI_EMBEDDED_DEPLOYMENT_MODEL);
    });
    builder.Services.AddScoped<AZCognitiveSearch>((serviceProvider) =>
    {
        
        var aoaiSettings = serviceProvider.GetService<IOptionsMonitor<AOAISettings>>()?.CurrentValue;
        return new AZCognitiveSearch(aoaiSettings?.AOAI_KEY, aoaiSettings?.AOAI_ENDPOINT, aoaiSettings?.AOAI_CHAT_DEPLOYMENT_NAME, aoaiSettings?.AOAI_CHAT_DEPLOYMENT_MODEL, aoaiSettings?.AOAI_EMBEDDED_DEPLOYMENT_MODEL);
    });
    builder.Services.AddScoped<AZChatCognitiveService>();

    builder.Services.AddHttpClient();


    pythonState = await PyProcessors.PySetup.Initialize((s) => { Console.WriteLine(s); });

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
}
catch (Exception ex)
{

}
finally
{
    PySetup.Shutdown(pythonState);
}

