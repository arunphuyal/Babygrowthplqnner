using System.Text.RegularExpressions;
using Babybunu.Api.Data;
using Babybunu.Api.Domain;

namespace Babybunu.Api.Tests;

/// <summary>Keeps the seeded guides at the same bar as the on-page checklist in the admin editor (admin/src/seo.ts).</summary>
public class StarterGuideQualityTests
{
    private static readonly List<Article> All = Guides.All.ToList();
    private static readonly Regex Link = new(@"\[[^\]]+\]\((/[^)\s]*)\)");

    public static IEnumerable<object[]> Slugs() => All.Select(g => new object[] { g.Slug });
    private static Article Get(string slug) => All.Single(g => g.Slug == slug);
    private static int Words(string s) => Regex.Matches(s, @"\S+").Count;

    [Fact]
    public void There_are_at_least_twelve_guides_with_unique_slugs()
    {
        Assert.True(All.Count >= 12, $"only {All.Count} guides");
        Assert.Equal(All.Count, All.Select(g => g.Slug).Distinct().Count());
    }

    [Theory, MemberData(nameof(Slugs))]
    public void Passes_the_on_page_checklist(string slug)
    {
        var g = Get(slug);
        var kw = g.FocusKeyword!.ToLowerInvariant();
        var firstParagraph = g.Body.Split("\n\n").First(b => !b.StartsWith('#')).ToLowerInvariant();

        Assert.InRange(g.MetaTitle!.Length, 30, 60);
        Assert.InRange(g.MetaDescription!.Length, 70, 160);
        Assert.True(Words(g.Body) >= 300, $"{slug}: only {Words(g.Body)} words");
        Assert.True(Regex.Matches(g.Body, "^## ", RegexOptions.Multiline).Count >= 2, $"{slug}: needs 2+ subheadings");
        Assert.Matches(@"^[a-z0-9]+(-[a-z0-9]+){0,5}$", g.Slug);
        Assert.True(g.Slug.Length <= 60);
        Assert.Contains(kw, g.MetaTitle.ToLowerInvariant());
        Assert.Contains(kw, g.MetaDescription.ToLowerInvariant());
        Assert.Contains(kw.Replace(' ', '-'), g.Slug);
        Assert.Contains(kw, firstParagraph);
        Assert.False(string.IsNullOrWhiteSpace(g.Title) || string.IsNullOrWhiteSpace(g.Summary));
        Assert.True(g.Summary.Length <= 160, $"{slug}: summary is {g.Summary.Length} chars");
    }

    [Theory, MemberData(nameof(Slugs))]
    public void Has_internal_links_and_every_link_resolves(string slug)
    {
        var links = Link.Matches(Get(slug).Body).Select(m => m.Groups[1].Value).ToList();
        Assert.NotEmpty(links);
        var known = All.Select(g => "/guides/" + g.Slug).ToHashSet();
        foreach (var l in links) Assert.True(known.Contains(l), $"{slug} links to missing page {l}");
        Assert.DoesNotContain("/guides/" + slug, links); // no self-links
    }

    [Fact]
    public void No_guide_is_orphaned()
    {
        var linked = All.SelectMany(g => Link.Matches(g.Body).Select(m => m.Groups[1].Value)).ToHashSet();
        foreach (var g in All) Assert.True(linked.Contains("/guides/" + g.Slug), $"nothing links to {g.Slug}");
    }

    [Fact]
    public void Wording_is_neutral_international_english_with_us_spelling()
    {
        // Plain words everyone understands; US spelling where regions differ; no brand name inside seeded content.
        var banned = new Regex(@"\b(wee|wees|poo|poos|nappy|nappies|paediatric\w*|colour\w*|behaviour\w*|fibre|favourite|organis\w+|recognis\w+|programme|Babybunu)\b", RegexOptions.IgnoreCase);
        foreach (var g in All)
            foreach (var text in new[] { g.Title, g.Summary, g.Body, g.MetaTitle!, g.MetaDescription! })
                Assert.DoesNotMatch(banned, text);
    }

    [Fact]
    public void Health_guidance_stays_cautious()
    {
        foreach (var g in All.Where(g => g.Slug is "toddler-constipation-potty-training" or "night-time-potty-training" or "potty-training-regression" or "potty-training-accidents"))
            Assert.Matches(new Regex(@"\b(doctor|health visitor)\b", RegexOptions.IgnoreCase), g.Body);
        // never gives doses or tells parents to medicate their child
        foreach (var g in All) Assert.DoesNotMatch(@"\b\d+\s?(mg|ml/kg|mcg)\b", g.Body);
        Assert.Contains("without a doctor's advice", Get("toddler-constipation-potty-training").Body);
    }
}
