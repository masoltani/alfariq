using System.Text.Json;

namespace AlFariq.Web.Tests;

public class PwaManifestTests
{
    private static JsonElement LoadManifest()
    {
        using var document = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(RepoPaths.WebRoot, "manifest.webmanifest")));
        return document.RootElement.Clone();
    }

    [Fact]
    public void Manifest_declares_what_installation_requires()
    {
        var manifest = LoadManifest();

        Assert.Equal("AlFariq", manifest.GetProperty("name").GetString());
        Assert.False(string.IsNullOrWhiteSpace(manifest.GetProperty("short_name").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(manifest.GetProperty("start_url").GetString()));
        Assert.Equal("standalone", manifest.GetProperty("display").GetString());
        Assert.Matches("^#[0-9a-fA-F]{6}$", manifest.GetProperty("theme_color").GetString()!);
        Assert.Matches("^#[0-9a-fA-F]{6}$", manifest.GetProperty("background_color").GetString()!);
    }

    [Fact]
    public void Manifest_icons_cover_192_512_and_maskable_and_the_files_exist()
    {
        var icons = LoadManifest().GetProperty("icons").EnumerateArray().ToList();

        Assert.Contains(icons, i => i.GetProperty("sizes").GetString() == "192x192");
        Assert.Contains(icons, i =>
            i.GetProperty("sizes").GetString() == "512x512" && i.GetProperty("purpose").GetString() == "any");
        Assert.Contains(icons, i => i.GetProperty("purpose").GetString() == "maskable");

        foreach (var icon in icons)
        {
            var source = icon.GetProperty("src").GetString()!;
            Assert.True(File.Exists(Path.Combine(RepoPaths.WebRoot, source)), $"Icône manquante : {source}");
        }
    }

    [Fact]
    public void Index_html_links_the_manifest_and_registers_the_service_worker()
    {
        var html = File.ReadAllText(Path.Combine(RepoPaths.WebRoot, "index.html"));

        Assert.Contains("rel=\"manifest\"", html);
        Assert.Contains("serviceWorker", html);
        Assert.Contains("service-worker.js", html);
    }

    [Fact]
    public void Service_worker_exists_in_both_variants_and_the_published_one_handles_updates()
    {
        Assert.True(File.Exists(Path.Combine(RepoPaths.WebRoot, "service-worker.js")));

        var published = File.ReadAllText(Path.Combine(RepoPaths.WebRoot, "service-worker.published.js"));
        Assert.Contains("assetsManifest", published);
        Assert.Contains("SKIP_WAITING", published);
    }
}
