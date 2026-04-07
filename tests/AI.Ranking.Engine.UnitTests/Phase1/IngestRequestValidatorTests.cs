using AI.Ranking.Engine.Application.Contracts;
using AI.Ranking.Engine.Application.Options;
using AI.Ranking.Engine.Application.Validation;
using AI.Ranking.Engine.Domain;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace AI.Ranking.Engine.UnitTests.Phase1;

public sealed class IngestRequestValidatorTests
{
    private static IngestRequestValidator CreateValidator(int maxBytes = 200 * 1024)
    {
        var options = Options.Create(new IngestOptions { MaxUploadBytes = maxBytes });
        return new IngestRequestValidator(options);
    }

    [Fact]
    public void Valid_pdf_request_passes()
    {
        var v = CreateValidator();
        var request = new IngestRequest("cand-1", "resume.pdf", ContentLength: 1024, DocumentContentType.Pdf);
        var result = v.Validate(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Empty_entity_id_fails()
    {
        var v = CreateValidator();
        var request = new IngestRequest("", "resume.pdf", 100, DocumentContentType.Pdf);
        var result = v.Validate(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ContentLength_over_max_fails()
    {
        var v = CreateValidator(maxBytes: 100);
        var request = new IngestRequest("id", "resume.pdf", ContentLength: 101, DocumentContentType.Pdf);
        var result = v.Validate(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Disallowed_extension_fails()
    {
        var v = CreateValidator();
        var request = new IngestRequest("id", "resume.exe", 10, DocumentContentType.Pdf);
        var result = v.Validate(request);
        Assert.False(result.IsValid);
    }
}
