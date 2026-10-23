using Microsoft.Extensions.Options;
using PottyTrain.Api.Domain;

namespace PottyTrain.Api.Services;

public class FeatureOptions
{
    /// <summary>While true every account gets every feature and the app is funded by donations and website ads. Set false to enforce plans again.</summary>
    public bool AllFree { get; set; } = true;
    /// <summary>Abuse guard on children per account while everything is free.</summary>
    public int MaxChildrenWhenAllFree { get; set; } = 10;
}

/// <summary>Single place that decides what an account may do, so the paywall can be switched with <c>Features:AllFree</c>.</summary>
public class Entitlements(IOptions<FeatureOptions> options)
{
    private readonly FeatureOptions _o = options.Value;

    public bool IsPremium(User u) => _o.AllFree || u.EffectivePlan != Plan.Free;
    public bool CanShare(User u) => _o.AllFree || u.EffectivePlan == Plan.Family;
    public int MaxChildren(User u) => _o.AllFree ? _o.MaxChildrenWhenAllFree : u.EffectivePlan == Plan.Free ? 1 : int.MaxValue;
}
