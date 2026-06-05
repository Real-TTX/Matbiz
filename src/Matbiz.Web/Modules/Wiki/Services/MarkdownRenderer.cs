using Markdig;
using Microsoft.AspNetCore.Html;

namespace Matbiz.Web.Modules.Wiki.Services;

/// <summary>
/// Singleton Markdig pipeline used to render WikiPage content. Advanced
/// extensions enable tables, task lists, footnotes, etc. Raw HTML is
/// disabled — only Markdown syntax produces output, so user content can't
/// inject &lt;script&gt; tags.
/// </summary>
public class MarkdownRenderer
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownRenderer()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .DisableHtml()
            .Build();
    }

    public IHtmlContent Render(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return HtmlString.Empty;
        var html = Markdown.ToHtml(markdown, _pipeline);
        return new HtmlString(html);
    }
}
