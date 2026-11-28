using System.Globalization;
using AlFariq.Application;
using AlFariq.Client.Core;
using AlFariq.Client.Core.Storage;
using AlFariq.Domain;
using AlFariq.UI;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

// Français par défaut ; le choix de langue arrivera avec US-016.
var culture = new CultureInfo("fr-FR");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services
    .AddAlFariqApplication()
    .AddAlFariqClientCore()
    .AddLocalRepository<Profile>(LocalSchema.ProfilesStore)
    .AddLocalRepository<Player>(LocalSchema.PlayersStore)
    .AddLocalRepository<Match>(LocalSchema.MatchesStore)
    .AddLocalRepository<MatchParticipant>(LocalSchema.MatchParticipantsStore)
    .AddLocalRepository<MatchSide>(LocalSchema.MatchSidesStore)
    .AddLocalRepository<MatchEvent>(LocalSchema.MatchEventsStore)
    .AddAlFariqUi();

await builder.Build().RunAsync();
