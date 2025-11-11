using PokerDatabaseCLI.Application;
using PokerDatabaseCLI.Commands.Core;
using PokerDatabaseCLI.Core;
using PokerDatabaseCLI.Core.Dependencies;
using PokerDatabaseCLI.Domain.Poker;
using System.Globalization;
using System.Text;

namespace PokerDatabaseCLI.Commands;

public static class GetInfoOnPlayerCommand
{
    /// <summary>
    /// Definition of the information on player of db. Command for the CLI.
    /// </summary>
    public static readonly CommandDefinition Definition = new(
        Name: "InfoOnPlayer",
        Description: "Get info on player and last hands",
        Parameters: new[]
        {
            new ParameterDefinition("name", "n", "Count hands", true),
            new ParameterDefinition("count", "c", "Count hands", false, DefaultValue: "20"),
        },
        Execute: ExecuteGetInfoOnPlayerCommand
    );

    private static Result<string> ExecuteGetInfoOnPlayerCommand(CommandContext ctx)
    {
        var name = ctx.Parameters["name"];
        var countStr = ctx.Parameters["count"];

        if (!int.TryParse(countStr, out var count))
        {
            return Result<string>.Fail($"Invalid number: '{countStr}'");
        }

        var getInfoOnPlayerDependencies = GetInfoOnPlayerDependencies.Default;


        var getInfoOnPlayerServiceParams = new GetInfoOnPlayerServiceParams(
            GetInfoOnPlayerDependencies: getInfoOnPlayerDependencies,
            Name: name,
            Count: count
        );

        var result = GetInfoOnPlayerService.StartGetInfo(getInfoOnPlayerServiceParams);

        return result
            .Map(info =>
            {
                var sb = new StringBuilder();
                if (info.hands.Count != 0)
                {

                    sb.AppendLine("Hands:");

                    foreach (var hand in info.hands)
                    {
                        sb.AppendLine($"Hand Number - {hand.Number,-10}  | Dealt Cards - {GetCards(hand, name),10}| Stack Size - {GetChips(hand, name),-6}");
                    }
                }

                sb.AppendLine();
                sb.Append($"Total hands - {info.CountHands}");

                return sb.ToString();

            })
            .MapError(error => "unexcpected error");
    }

    private static string GetChips(Hand hand, string name)
    {
        var chips = hand.Players.First(p => p.Name == name).Chips;
        return chips.ToString("F2", CultureInfo.InvariantCulture);
    }

    private static string GetCards(Hand hand, string name)
    {
        return hand.Players.First(p => p.Name == name).Cards ?? "";
    }
}
