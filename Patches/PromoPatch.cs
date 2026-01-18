using System.Text.RegularExpressions;
using ILCCL.API;
using ILCCL.Content;

namespace ILCCL.Patches;

[HarmonyPatch]
internal class PromoPatch
{
    
    /*
     * Patch:
     * - Runs promo script of custom promos.
     */
    [HarmonyPatch(typeof(UnmappedPromo), nameof(UnmappedPromo.boe))]
    [HarmonyPrefix]
    public static void Promo_boe()
    {
        int promoId = UnmappedPromo.gbo - PromoData.BASE_PROMO_OFFSET;
        if (promoId < 0)
        {
            return;
        }

        PromoData promo;
        PromoLine promoLine;
        if (promoId >= PromoData.CODE_PROMO_OFFSET - PromoData.BASE_PROMO_OFFSET) {
            promo = CodePromoManager.GetPromoData(promoId - PromoData.CODE_PROMO_OFFSET + PromoData.BASE_PROMO_OFFSET);
            promoLine = CodePromoManager.HandlePage(promoId - PromoData.CODE_PROMO_OFFSET + PromoData.BASE_PROMO_OFFSET);
            if (promoLine == null)
            {
                MappedPromo.script = 0;
                return;
            }
        }
        else
        {
            promo = CustomPromoData[promoId];
            int page = UnmappedPromo.gbp - 1;
            if (page >= promo.NumLines)
            {
                MappedPromo.script = 0;
                return;
            }
            promoLine = promo.PromoLines[page];
        }

        if (promo.UseCharacterNames)
        {
            if (!promo.NameToID.TryGetValue(promoLine.FromName, out int fromid))
            {
                fromid = 0;
            }
            if (!promo.NameToID.TryGetValue(promoLine.ToName, out int toid))
            {
                toid = 0;
            }
            ExecutePromoLine(promoLine.Line1, promoLine.Line2, fromid,
                toid, promoLine.Demeanor, promoLine.TauntAnim, true);
        }
        else
        {
            ExecutePromoLine(promoLine.Line1, promoLine.Line2, promoLine.From,
                promoLine.To, promoLine.Demeanor, promoLine.TauntAnim, false);
        }

        if (UnmappedPromo.gbr >= 100f && UnmappedPromo.gbw < UnmappedPromo.gbp)
        {
            if (promoLine.Features != null)
            {
                foreach (AdvFeatures feature in promoLine.Features)
                {
                    AdvFeatures.CommandType cmd = feature.Command;
                    switch (cmd)
                    {
                        case AdvFeatures.CommandType.SetRealEnemy:
                            promo.GetCharacterForCmd(feature.Args[0])
                                .@if(promo.GetCharacterForCmd(feature.Args[1]).id, -1, 0);
                            break;
                        case AdvFeatures.CommandType.SetStoryEnemy:
                            promo.GetCharacterForCmd(feature.Args[0])
                                .@if(promo.GetCharacterForCmd(feature.Args[1]).id, -1);
                            break;
                        case AdvFeatures.CommandType.SetRealFriend:
                            promo.GetCharacterForCmd(feature.Args[0])
                                .@if(promo.GetCharacterForCmd(feature.Args[1]).id, 1, 0);
                            break;
                        case AdvFeatures.CommandType.SetStoryFriend:
                            promo.GetCharacterForCmd(feature.Args[0])
                                .@if(promo.GetCharacterForCmd(feature.Args[1]).id, 1);
                            break;
                        case AdvFeatures.CommandType.SetRealNeutral:
                            promo.GetCharacterForCmd(feature.Args[0])
                                .@if(promo.GetCharacterForCmd(feature.Args[1]).id, 0, 0);
                            break;
                        case AdvFeatures.CommandType.SetStoryNeutral:
                            promo.GetCharacterForCmd(feature.Args[0])
                                .@if(promo.GetCharacterForCmd(feature.Args[1]).id, 0);
                            break;
                        case AdvFeatures.CommandType.PlayAudio:
                            if (feature.Args[0] == "-1")
                            {
                                UnmappedSound.byv(UnmappedPromo.gcb, -1, 1f);
                            }
                            else
                            {
                                UnmappedSound.gsa.PlayOneShot(
                                    UnmappedSound.gse[Indices.ParseCrowdAudio(feature.Args[0])], 1);
                            }

                            break;
                    }
                }
            }

            UnmappedPromo.gbw = UnmappedPromo.gbp;
        }
    }
    
#pragma warning disable Harmony003
    private static void ExecutePromoLine(string line1, string line2, int from, int to, float demeanor, int taunt, bool useNames)
    {
        line1 = ReplaceVars(line1);
        line2 = ReplaceVars(line2);
        if(useNames)
        {
            UnmappedPromo.bog(from, to, demeanor, taunt);
        }
        else
        {
            UnmappedPromo.bog(UnmappedPromo.gbx[from], UnmappedPromo.gbx[to], demeanor, taunt);
        }

        UnmappedPromo.gbm[1] = line1;
        UnmappedPromo.gbm[2] = line2;
    }

    private static string ReplaceVars(string line)
    {
        // Replace $variables
        MatchCollection matches = Regex.Matches(line, @"\$\$?([a-zA-Z]+)(\W|$)");
        foreach (Match match in matches)
        {
            try
            {
                string varName = match.Groups[1].Value.ToLower();
                string varValue = varName switch
                {
                    "location" => MappedWorld.DescribeLocation(World.location),
                    "date" => "Day " + Progress.day,
                    _ => "UNKNOWN"
                };
                line = line.Replace(match.Value, varValue + match.Groups[2].Value);
            }
            catch (Exception e)
            {
                line = line.Replace(match.Value, "INVALID");
                LogError(e);
            }
        }
        matches = Regex.Matches(line, @"\$\$?([a-zA-Z]+)-?(\d+)(\W|$)");
        foreach (Match match in matches)
        {
            try
            {
                string varName = match.Groups[1].Value.ToLower();
                int varIndex = int.Parse(match.Groups[2].Value);
                string varValue;
                if (varName == "date")
                {
                    var date = Progress.day + varIndex;
                    varValue = "Day " + date;
                }
                else
                {
                    varValue = varName switch
                    {
                        "name" => MappedPromo.c[varIndex].name,
                        "prop" => MappedWeapons.Describe(MappedPromo.c[varIndex].prop),
                        "team" => MappedPromo.c[varIndex].teamName,
                        _ => "UNKNOWN"
                    };
                }
                line = line.Replace(match.Value, varValue + match.Groups[3].Value);
            }
            catch (Exception e)
            {
                line = line.Replace(match.Value, "INVALID");
                LogError(e);
            }
        }
        matches = Regex.Matches(line,
            @"\$\$?([a-zA-Z]+)-?(\d+)_-?(\d+)(\W|$)");
        foreach (Match match in matches)
        {
            try
            {
                string varName = match.Groups[1].Value.ToLower();
                int varIndex1 = int.Parse(match.Groups[2].Value);
                int varIndex2 = int.Parse(match.Groups[3].Value);
                string varValue = varName switch
                {
                    "movefront" => MappedAnims.DescribeMove(MappedPromo.c[varIndex1].moveset[MappedPromo.c[varIndex1].activeCostume - 1].moveFront[varIndex2]),
                    "moveback" => MappedAnims.DescribeMove(MappedPromo.c[varIndex1].moveset[MappedPromo.c[varIndex1].activeCostume - 1].moveBack[varIndex2]),
                    "moveground" => MappedAnims.DescribeMove(MappedPromo.c[varIndex1].moveset[MappedPromo.c[varIndex1].activeCostume - 1].moveGround[varIndex2]),
                    "moveattack" => MappedAnims.DescribeMove(MappedPromo.c[varIndex1].moveset[MappedPromo.c[varIndex1].activeCostume - 1].moveAttack[varIndex2]),
                    "movecrush" => MappedAnims.DescribeMove(MappedPromo.c[varIndex1].moveset[MappedPromo.c[varIndex1].activeCostume - 1].moveCrush[varIndex2]),
                    "taunt" => ((MappedTaunt) MappedAnims.taunt[MappedPromo.c[varIndex1].moveset[MappedPromo.c[varIndex1].activeCostume - 1].taunt[varIndex2]]).name,
                    "stat" => MappedPromo.c[varIndex1].stat[varIndex2].ToString("0"),
                    _ => "UNKNOWN"
                };

                line = line.Replace(match.Value, varValue + match.Groups[4].Value);
            }
            catch (Exception e)
            {
                line = line.Replace(match.Value, "INVALID");
                LogError(e);
            }
        }

        matches = Regex.Matches(line, @"@([a-zA-Z]+)(\d+)(\W|$)");
        foreach (Match match in matches)
        {
            string varName = match.Groups[1].Value;
            int varIndex = int.Parse(match.Groups[2].Value);
            string varValue = UnmappedPromo.gbz[varIndex].gu(varName);

            line = line.Replace(match.Value, varValue + match.Groups[3].Value);
        }

        return line;
    }
#pragma warning restore Harmony003
}