using AlFariq.Application;
using Microsoft.JSInterop;

namespace AlFariq.Client.Core.Sharing;

/// <summary>
/// Partage dans le navigateur (module <c>share.js</c>) : partage natif, presse-papiers et image dessinée sur place.
/// </summary>
/// <remarks>
/// Le navigateur n'ouvre le partage que pendant un geste de l'utilisateur : une fois le module chargé (par
/// <see cref="CanShareAsync"/>), les appels partent sans attente préalable.
/// </remarks>
public sealed class BrowserShareService : IShareService
{
    private const string ModulePath = "./_content/AlFariq.Client.Core/js/share.js";

    private readonly IJSRuntime _js;
    private Task<IJSObjectReference>? _module;

    public BrowserShareService(IJSRuntime js) => _js = js;

    public async Task<bool> CanShareAsync()
    {
        try
        {
            var module = await GetModuleAsync();
            return await module.InvokeAsync<bool>("canShare");
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return false;
        }
    }

    public async Task<ShareOutcome> ShareTextAsync(string text)
    {
        try
        {
            var module = await GetModuleAsync();
            return ToOutcome(await module.InvokeAsync<string>("shareText", text));
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return ShareOutcome.Failed;
        }
    }

    public async Task<bool> CopyTextAsync(string text)
    {
        try
        {
            var module = await GetModuleAsync();
            return await module.InvokeAsync<bool>("copyText", text);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return false;
        }
    }

    public async Task<ILineupImage?> CreateImageAsync(LineupCard card, string fileName)
    {
        try
        {
            var module = await GetModuleAsync();
            var handle = await module.InvokeAsync<IJSObjectReference>("createImage", card, fileName);
            var info = await handle.InvokeAsync<ImageInfo>("info");
            return new BrowserImage(handle, info.Url, fileName, info.CanShare);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return null;
        }
    }

    private static ShareOutcome ToOutcome(string result) => result switch
    {
        "shared" => ShareOutcome.Shared,
        "cancelled" => ShareOutcome.Cancelled,
        _ => ShareOutcome.Failed,
    };

    // Chargé à la première utilisation, rechargé si le chargement a échoué.
    private async Task<IJSObjectReference> GetModuleAsync()
    {
        _module ??= _js.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();
        try
        {
            return await _module;
        }
        catch
        {
            _module = null;
            throw;
        }
    }

    private sealed record ImageInfo(string Url, bool CanShare);

    private sealed class BrowserImage(IJSObjectReference handle, string url, string fileName, bool canShare)
        : ILineupImage
    {
        public string Url => url;

        public string FileName => fileName;

        public bool CanShare => canShare;

        public async Task<ShareOutcome> ShareAsync()
        {
            try
            {
                return ToOutcome(await handle.InvokeAsync<string>("share"));
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return ShareOutcome.Failed;
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await handle.InvokeVoidAsync("dispose");
                await handle.DisposeAsync();
            }
            catch (Exception exception)
            {
                // Libérer la mémoire ne doit jamais gêner la navigation.
                Console.Error.WriteLine(exception);
            }
        }
    }
}
