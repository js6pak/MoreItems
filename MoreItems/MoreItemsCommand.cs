using System;
using System.Collections.Generic;
using System.Linq;
using CommandSystem;
using SixModLoader.Api.Extensions;

namespace MoreItems
{
    [AutoCommandHandler(typeof(RemoteAdminCommandHandler))]
    public class MoreItemsCommand : ParentCommand
    {
        public override string Command => "moreitems";
        public override string[] Aliases => new[] { "mi" };
        public override string Description => "MoreItems";

        public MoreItemsCommand()
        {
            LoadGeneratedCommands();
        }

        public sealed override void LoadGeneratedCommands()
        {
            RegisterCommand(new GiveCommand());
            RegisterCommand(new ListCommand());
        }

        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = "Please specify a valid subcommand!";
            return false;
        }

        public class ListCommand : ICommand
        {
            public string Command => "list";
            public string[] Aliases => new[] { "items" };
            public string Description => "List all custom items";

            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                if (!sender.HasPermission("moreitems.list"))
                {
                    response = "Insufficient permissions";
                    return false;
                }

                response = $"Items ({CustomItem.Items.Count}): " + string.Join(", ", CustomItem.Items.Select(item => $"{item.Key} ({item.Value.FullName})"));
                return true;
            }
        }

        public class GiveCommand : ICommand
        {
            public string Command => "give";
            public string[] Aliases => new string[0];
            public string Description => "Give custom item";

            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                if (!sender.HasPermission("moreitems.give"))
                {
                    response = "Insufficient permissions";
                    return false;
                }

                if (arguments.Count <= 1)
                {
                    response = "Wrong syntax";
                    return false;
                }

                var targets = CommandExtensions.MatchPlayers(arguments.ElementAt(0), sender);

                if (targets.Count <= 0)
                {
                    response = "No players found";
                    return false;
                }

                var itemId = arguments.ElementAtOrDefault(1);
                if (itemId != null && CustomItem.Items.ContainsKey(itemId))
                {
                    var givenItems = new List<CustomItem>();

                    foreach (var target in targets)
                    {
                        var customItem = (CustomItem) Activator.CreateInstance(CustomItem.Items[itemId]);
                        if (customItem == null)
                        {
                            response = $"Item with id: {itemId} is invalid! (no constructor)";
                            return false;
                        }

                        if (customItem.Give(target) != null)
                        {
                            givenItems.Add(customItem);
                        }
                    }

                    response = $"Given custom items to {givenItems.Count()}/{targets.Count} players";
                    return true;
                }

                response = "Unknown custom item";
                return false;
            }
        }
    }
}