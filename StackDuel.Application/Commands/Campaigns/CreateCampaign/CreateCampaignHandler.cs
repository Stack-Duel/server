using Ardalis.Result;
using FluentValidation;
using StackDuel.Application.Campaigns;
using StackDuel.Domain.Campaigns;
using StackDuel.Domain.Campaigns.Entities;
using System.Text;

namespace StackDuel.Application.Commands.Campaigns.CreateCampaign;

internal sealed class CreateCampaignHandler(
    ICampaignReadRepository campaignReadRepository,
    ICampaignWriteRepository campaignWriteRepository,
    IValidator<CreateCampaignCommand> validator
) : AbstractCommandHandler<CreateCampaignCommand, Guid>(validator)
{
    protected override async Task<Result<Guid>> HandleValidated(
        CreateCampaignCommand request,
        CancellationToken cancellationToken
    )
    {
        string baseSlug = Slugify(request.Title);
        string slug = baseSlug;
        int suffix = 2;

        while (await campaignReadRepository.SlugExistsAsync(slug, cancellationToken))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix++;
        }

        Campaign campaign = new(slug, request.Title, request.Description, request.Difficulty);

        await campaignWriteRepository.AddAsync(campaign, cancellationToken);

        return Result<Guid>.Success(campaign.Id);
    }

    private static string Slugify(string title)
    {
        StringBuilder builder = new();
        bool lastWasHyphen = false;

        foreach (char c in title.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(c);
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen && builder.Length > 0)
            {
                builder.Append('-');
                lastWasHyphen = true;
            }
        }

        if (lastWasHyphen && builder.Length > 0)
            builder.Length--;

        return builder.Length > 0 ? builder.ToString() : "campaign";
    }
}