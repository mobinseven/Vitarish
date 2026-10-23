using FluentAssertions;
using Xunit;

namespace Vitarish.Landing.Application.Tests;

public sealed class LandingConsultationTests
{
    [Fact]
    public void Every_answer_points_only_to_features_the_page_may_claim()
    {
        IEnumerable<string> referenced = new[] { LandingConsultation.Booking, LandingConsultation.Pains }
            .SelectMany(question => question.Options)
            .SelectMany(option => option.FeatureKeys);

        referenced.Should().OnlyContain(key => LandingFeatureCatalog.Keys.Contains(key));
    }

    [Fact]
    public void Answer_keys_are_unique_within_each_question_and_every_pain_leads_to_an_example()
    {
        foreach (LandingConsultationQuestion question in new[] { LandingConsultation.Kind(["dentistry", "dermatology"]), LandingConsultation.Booking, LandingConsultation.Pains })
        {
            question.Options.Select(option => option.Key).Should().OnlyHaveUniqueItems(question.Key);
            question.MaxSelections.Should().BeInRange(1, question.Options.Count);
        }

        LandingConsultation.Pains.Options.Should().OnlyContain(option => option.Proof != null && option.FeatureKeys.Count > 0);
    }

    [Fact]
    public void Only_booking_and_pain_answers_can_be_carried_into_sign_up()
    {
        LandingConsultation.AreKnownAnswers(["phone", "no-shows"]).Should().BeTrue();
        LandingConsultation.AreKnownAnswers(["phone", "anything-else"]).Should().BeFalse();
    }
}
