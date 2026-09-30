namespace Novolis.Geopolitics.Core;

/// <summary>Why a polity refuses a treaty or org join.</summary>
public enum TreatyRefusal
{
    Accepted = 0,
    RelationsTooLow,
    AtWar,
    HostileWithFriend,
    CannotAfford,
    PowerImbalance,
    AlreadyBound,
    Unstable,
    WrongProfile,
}
