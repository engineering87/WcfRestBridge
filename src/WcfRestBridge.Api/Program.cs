// (c) 2025 Francesco Del Re <francesco.delre.87@gmail.com>
// This code is licensed under MIT license (see LICENSE.txt for details)
using System.Reflection;
using WcfRestBridge.Core;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Scansiona i servizi WCF all'avvio e registrali come singleton
var assembly = Assembly.Load("WcfRestBridge.TestHost");
var services = WcfServiceScanner.DiscoverServices(assembly);
builder.Services.AddSingleton<IEnumerable<WcfServiceDescriptor>>(services);

// Aggiungi configurazione per leggere appsettings.json
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
