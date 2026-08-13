using System.Text.RegularExpressions;
using Ringly.Trunking.Abstractions.Models;
using Ringly.Trunking.Asterisk.Models.Foundations.Trunks.Exceptions;

namespace Ringly.Trunking.Asterisk.Services.Foundations.Trunks;

public partial class SipTrunkFoundationService
{
    // NANP default — the doc doesn't specify a target market, confirm the actual domestic
    // country code before shipping.
    private const string DomesticCountryCode = "1";

    // E.164: '+' followed by 7-15 digits, first digit non-zero.
    private static readonly Regex PhoneNumberPattern = new(@"^\+[1-9]\d{6,14}$", RegexOptions.Compiled);

    private static void ValidateDialOutRequest(string phoneNumber, string trunkName)
    {
        var invalidDialOutRequestException = new InvalidDialOutRequestException();

        if (string.IsNullOrWhiteSpace(phoneNumber) || !PhoneNumberPattern.IsMatch(phoneNumber))
        {
            invalidDialOutRequestException.UpsertDataList(
                key: nameof(phoneNumber),
                value: "Value must be a valid E.164 phone number (e.g. +15555550100)");
        }

        if (string.IsNullOrWhiteSpace(trunkName))
        {
            invalidDialOutRequestException.UpsertDataList(
                key: nameof(trunkName),
                value: "Value is required");
        }

        invalidDialOutRequestException.ThrowIfContainsErrors();
    }

    // Checking against just the domestic code and the caller's own configured allow-list (both
    // known, small sets) rather than a full E.164 country-calling-code table — the only two
    // comparisons this decision actually needs.
    private static void ValidateDestinationAllowed(string phoneNumber, SipTrunkConfig config)
    {
        bool isDomestic = phoneNumber.StartsWith($"+{DomesticCountryCode}", StringComparison.Ordinal);

        bool isExplicitlyAllowed = config.AllowedDestinationCountryCodes?.Any(code =>
            phoneNumber.StartsWith($"+{code}", StringComparison.Ordinal)) == true;

        bool isBlocked = !isDomestic && !(config.InternationalDialingEnabled && isExplicitlyAllowed);

        if (isBlocked)
        {
            throw new BlockedDestinationException(phoneNumber);
        }
    }

    private static void ValidateWithinLimits(TrunkCallLimitStatus status, SipTrunkConfig config, string trunkName)
    {
        bool isOverConcurrencyLimit = status.ActiveCallCount >= config.MaxConcurrentCallsPerTrunk;

        bool isOverSpendLimit =
            config.MaxDailySpendUsd.HasValue && status.SpendTodayUsd >= config.MaxDailySpendUsd.Value;

        if (isOverConcurrencyLimit || isOverSpendLimit)
        {
            throw new TrunkSpendLimitExceededException(trunkName);
        }
    }
}
