﻿#region using directives

using System.Linq;
using System.Threading.Tasks;
using PoGo.NecroBot.Logic.Event;
using PoGo.NecroBot.Logic.Model;
using PoGo.NecroBot.Logic.State;
using PoGo.NecroBot.Logic.Utils;

#endregion

namespace PoGo.NecroBot.Logic.Tasks
{
    public class EvolveSpecificPokemonTask
    {
        public static async Task Execute(ISession session, ulong pokemonId)
        {
            using (var blocker = new BlockableScope(session, BotActions.Envolve))
            {
                if (!await blocker.WaitToRun()) return;


                var all = await session.Inventory.GetPokemons();
                var pokemons = all.OrderByDescending(x => x.Cp).ThenBy(n => n.StaminaMax);
                var pokemon = pokemons.FirstOrDefault(p => p.Id == pokemonId);

                if (pokemon == null) return;

                var pokemonSettings = await session.Inventory.GetPokemonSettings();
                var pokemonFamilies = await session.Inventory.GetPokemonFamilies();

                var setting = pokemonSettings.FirstOrDefault(q => pokemon != null && q.PokemonId == pokemon.PokemonId);
                var family = pokemonFamilies.FirstOrDefault(q => setting != null && q.FamilyId == setting.FamilyId);

                if (setting.CandyToEvolve > family.Candy_) return;
                family.Candy_ -= setting.CandyToEvolve;

                var evolveResponse = await session.Client.Inventory.EvolvePokemon(pokemon.Id);

                // Update setting after evolve.
                setting = pokemonSettings.FirstOrDefault(q => pokemon != null && q.PokemonId == evolveResponse.EvolvedPokemonData.PokemonId); 

                session.EventDispatcher.Send(new PokemonEvolveEvent
                {
                    OriginalId = pokemonId,
                    Id = pokemon.PokemonId,
                    Exp = evolveResponse.ExperienceAwarded,
                    UniqueId = pokemon.Id,
                    Result = evolveResponse.Result,
                    EvolvedPokemon = evolveResponse.EvolvedPokemonData,
                    PokemonSetting = setting,
                    Family = family
                });
                DelayingUtils.Delay(session.LogicSettings.EvolveActionDelay, 0);
            }
        }
    }
}