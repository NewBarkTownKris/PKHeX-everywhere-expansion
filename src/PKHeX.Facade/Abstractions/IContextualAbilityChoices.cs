namespace PKHeX.Facade.Abstractions;

/// <summary>Ability slots whose values depend on the open game's state.</summary>
public interface IContextualAbilityChoices
{
    /// <summary>Whether contextual ability choices apply to this Pokémon.</summary>
    bool HasContextualAbilityChoices { get; }
    /// <summary>Get the currently available contextual ability IDs.</summary>
    IReadOnlyList<int> GetContextualAbilityChoices();
}
