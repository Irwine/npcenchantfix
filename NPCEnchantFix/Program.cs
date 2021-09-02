using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using System.Threading.Tasks;

namespace NPCEnchantFix
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "NPCEnchantFix.esp")
                .Run(args);
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            // Loop over all NPCs in the load order
            foreach (var npc in state.LoadOrder.PriorityOrder.Npc().WinningOverrides())
            {
                try
                {
                    // Skip NPC if it inherits spells from its template
                    if (npc.Configuration.TemplateFlags.HasFlag(NpcConfiguration.TemplateFlag.SpellList)) continue;

                    // Find if the NPC has PerkSkill or AlchemySkill perks
                    var hasPerkSkillBoosts = false;
                    var hasAlchemySkillBoosts = false;

                    foreach (var perk in npc.Perks.EmptyIfNull())
                    {
                        if (perk.Perk.Equals(Skyrim.Perk.AlchemySkillBoosts)) hasAlchemySkillBoosts = true;
                        if (perk.Perk.Equals(Skyrim.Perk.PerkSkillBoosts)) hasPerkSkillBoosts = true;
                        if (hasAlchemySkillBoosts && hasPerkSkillBoosts) break;
                    }

                    // If NPC has both, it is safe
                    if (hasAlchemySkillBoosts && hasPerkSkillBoosts) continue;

                    // Otherwise, add the NPC to the patch
                    var modifiedNpc = state.PatchMod.Npcs.GetOrAddAsOverride(npc);
                    
                    if (modifiedNpc.Name != null && modifiedNpc.Name.TryLookup(Language.French, out string i18nNpcName)) {
                        modifiedNpc.Name = Encoding.GetEncoding("ISO-8859-1").GetString(Encoding.UTF8.GetBytes(i18nNpcName));
                    }
                    if (modifiedNpc.ShortName != null && modifiedNpc.ShortName.TryLookup(Language.French, out string i18nNpcShortName)) {
                        modifiedNpc.ShortName = Encoding.GetEncoding("ISO-8859-1").GetString(Encoding.UTF8.GetBytes(i18nNpcShortName));
                    }

                    // Ensure perk list exists
                    modifiedNpc.Perks ??= new ExtendedList<PerkPlacement>();

                    // Add missing perks
                    if (!hasAlchemySkillBoosts)
                    {
                        modifiedNpc.Perks.Add(new PerkPlacement()
                        {
                            Perk = Skyrim.Perk.AlchemySkillBoosts,
                            Rank = 1
                        });
                    }

                    if (!hasPerkSkillBoosts)
                    {
                        modifiedNpc.Perks.Add(new PerkPlacement()
                        {
                            Perk = Skyrim.Perk.PerkSkillBoosts,
                            Rank = 1
                        });
                    }
                }
                catch (Exception ex)
                {
                    throw RecordException.Factory(ex, npc);
                }
            }
        }
    }
}
