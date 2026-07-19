using CodeCafe.Shared.Domain.Ai;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CodeCafe.Shared.Infrastructure.Persistence.Configurations;

public sealed class AiEditProposalConfiguration : IEntityTypeConfiguration<AiEditProposal>
{
    public void Configure(EntityTypeBuilder<AiEditProposal> entity)
    {
        entity.ToTable("AiEditProposals");

        entity.HasKey(proposal => proposal.Id);

        entity.Property(proposal => proposal.Id)
            .ValueGeneratedNever();

        entity.Property(proposal => proposal.ActorUserId)
            .IsRequired();

        entity.Property(proposal => proposal.NotebookId)
            .IsRequired();

        entity.Property(proposal => proposal.NotebookSlug)
            .HasMaxLength(180)
            .IsRequired();

        entity.Property(proposal => proposal.PayloadJson)
            .IsRequired();

        entity.Property(proposal => proposal.ExpiresAtUtc)
            .IsRequired();

        entity.Property(proposal => proposal.CreatedAtUtc)
            .IsRequired();

        entity.Property(proposal => proposal.UpdatedAtUtc);

        entity.HasIndex(proposal => new { proposal.ActorUserId, proposal.ExpiresAtUtc });
    }
}
