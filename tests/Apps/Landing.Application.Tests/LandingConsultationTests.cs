using FluentAssertions;
using Xunit;

namespace Vitarish.Landing.Application.Tests;

public sealed class LandingConsultationTests
{
    [Fact]
    public void Every_answer_points_only_to_features_the_page_may_claim()
    {
        IEnumerable<string> referenced = LandingConsultation.Questions
            .SelectMany(question => question.Options)
            .SelectMany(option => option.FeatureKeys);

        referenced.Should().OnlyContain(key => LandingFeatureCatalog.Keys.Contains(key));
    }

    [Fact]
    public void Answer_keys_are_unique_within_each_question_and_every_pain_leads_to_an_example()
    {
        foreach (LandingConsultationQuestion question in LandingConsultation.Questions)
        {
            question.Options.Select(option => option.Key).Should().OnlyHaveUniqueItems(question.Key);
            question.MaxSelections.Should().BeInRange(1, question.Options.Count);
        }

        LandingConsultation.Pains.Options.Should().OnlyContain(option => option.Proof != null && option.FeatureKeys.Count > 0);
    }
}
