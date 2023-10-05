using FluentValidation;
using HashidsNet;
using MediatR;
using UrlShortenerService.Application.Common.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Numerics;

namespace UrlShortenerService.Application.Url.Commands;

public record CreateShortUrlCommand : IRequest<string>
{
    public string Url { get; init; } = default!;
}

public class CreateShortUrlCommandValidator : AbstractValidator<CreateShortUrlCommand>
{
    public CreateShortUrlCommandValidator()
    {
        _ = RuleFor(v => v.Url)
          .NotEmpty()
          .WithMessage("Url is required.");

        // TODO Url validation
    }
}

public class CreateShortUrlCommandHandler : IRequestHandler<CreateShortUrlCommand, string>
{
    private readonly IApplicationDbContext _context;
    private readonly IHashids _hashids;

    public CreateShortUrlCommandHandler(IApplicationDbContext context, IHashids hashids)
    {
        _context = context;
        _hashids = hashids;
    }

    public async Task<string> Handle(CreateShortUrlCommand request, CancellationToken cancellationToken)
    {
        using var md5Hasher = MD5.Create();
        var md5hash = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(request.Url));

        var url = new Domain.Entities.Url
        {
            Hash = md5hash,
            OriginalUrl = request.Url
        };

        _ = await _context.Urls.AddAsync(url, cancellationToken);
        _ = await _context.SaveChangesAsync(cancellationToken);

        var uniqueId = Convert.ToHexString(md5hash);
        var encodedUrl = _hashids.EncodeHex(uniqueId);
        return encodedUrl;
    }
}
