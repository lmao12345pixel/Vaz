using FluentAssertions;
using Vazer.Core.Markdown;

namespace Vazer.Core.Tests;

public sealed class MarkdownLiteParserTests
{
    [Fact]
    public void PlainText_ProducesSingleTextToken()
    {
        var paragraphs = MarkdownLiteParser.Parse("Hello world");

        paragraphs.Should().HaveCount(1);
        paragraphs[0].Tokens.Should().ContainSingle()
            .Which.Type.Should().Be(MarkdownTokenType.Text);
    }

    [Fact]
    public void Bold_ProducesBoldToken()
    {
        var paragraphs = MarkdownLiteParser.Parse("**bold**");

        var token = paragraphs.Should().ContainSingle().Subject
            .Tokens.Should().ContainSingle().Subject;
        token.Type.Should().Be(MarkdownTokenType.Bold);
        token.Text.Should().Be("bold");
    }

    [Fact]
    public void HttpLink_ProducesLinkToken()
    {
        var paragraphs = MarkdownLiteParser.Parse("[link](https://x.com)");

        var token = paragraphs.Should().ContainSingle().Subject
            .Tokens.Should().ContainSingle().Subject;
        token.Type.Should().Be(MarkdownTokenType.Link);
        token.Text.Should().Be("link");
        token.Url.Should().Be("https://x.com");
    }

    [Fact]
    public void BoldThenLink_ProducesMultipleInlines()
    {
        var paragraphs = MarkdownLiteParser.Parse("**bold** [link](https://x.com)");

        var tokens = paragraphs.Should().ContainSingle().Subject.Tokens;
        tokens.Should().HaveCount(3);
        tokens[0].Type.Should().Be(MarkdownTokenType.Bold);
        tokens[1].Type.Should().Be(MarkdownTokenType.Text);
        tokens[2].Type.Should().Be(MarkdownTokenType.Link);
    }

    [Fact]
    public void SingleNewline_ProducesLineBreakToken()
    {
        var paragraphs = MarkdownLiteParser.Parse("line1\nline2");

        var tokens = paragraphs.Should().ContainSingle().Subject.Tokens;
        tokens.Should().Contain(t => t.Type == MarkdownTokenType.LineBreak);
    }

    [Fact]
    public void DoubleNewline_SplitsIntoTwoParagraphs()
    {
        var paragraphs = MarkdownLiteParser.Parse("para1\n\npara2");

        paragraphs.Should().HaveCount(2);
        paragraphs[0].Tokens.Should().ContainSingle()
            .Which.Text.Should().Be("para1");
        paragraphs[1].Tokens.Should().ContainSingle()
            .Which.Text.Should().Be("para2");
    }

    [Fact]
    public void UnclosedBold_StaysLiteral()
    {
        var paragraphs = MarkdownLiteParser.Parse("**unclosed");

        var token = paragraphs.Should().ContainSingle().Subject
            .Tokens.Should().ContainSingle().Subject;
        token.Type.Should().Be(MarkdownTokenType.Text);
        token.Text.Should().Be("**unclosed");
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,evil")]
    [InlineData("file:///C:/secret.txt")]
    public void NonHttpScheme_FallsBackToPlainText(string url)
    {
        var paragraphs = MarkdownLiteParser.Parse($"[text]({url})");

        var token = paragraphs.Should().ContainSingle().Subject
            .Tokens.Should().ContainSingle().Subject;
        token.Type.Should().Be(MarkdownTokenType.Text);
        token.Text.Should().Be("text");
        token.Url.Should().BeNull();
    }

    [Fact]
    public void ScriptTag_IsStrippedWithContent()
    {
        MarkdownLiteParser.Sanitize("<script>alert(1)</script>").Should().BeEmpty();
    }

    [Fact]
    public void ParagraphAndBrTags_BecomeNewlines()
    {
        MarkdownLiteParser.Sanitize("<p>Hello</p><br>World")
            .Should().Be("Hello\n\nWorld");
    }
}
