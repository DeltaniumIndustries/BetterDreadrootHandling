using System;
using Qud.UI;
using XRL.UI;
using XRL.World;

namespace XRL.World.Parts
{
    /// <summary>
    /// Handles automatic modifications of Dreadroot entities, including making them solid, hostile,
    /// or updating their configurations dynamically.
    /// </summary>
    [Serializable]
    public class BetterDreadrootHandling : IPart
    {
        private static bool DEBUG_LOGGING = false;
        private static int configCounter = 0;

        private bool OptionDreadrootEnabled { get; set; }
        private bool OptionDreadrootSolid { get; set; }
        private bool OptionDreadrootHostile { get; set; }
        private bool OptionDreadrootUpdateOld { get; set; }

        public override void Initialize() => GetConfigOptions();

        private static void Info(string message)
        {
            DEBUG_LOGGING = GetBooleanOption("OptionDreadrootLogging");
            if (!DEBUG_LOGGING) return;

            string logMessage = $"[Laurus] {message}";
            Messages.MessageQueue.AddPlayerMessage(logMessage);
            Core.XRLCore.Core.WriteConsoleLine(logMessage);
        }

        public override bool WantEvent(int ID, int cascade) =>
            base.WantEvent(ID, cascade) || ID == AfterObjectCreatedEvent.ID || ID == EnteredCellEvent.ID;

        public override bool HandleEvent(EnteredCellEvent E)
        {
            if (!OptionDreadrootEnabled || OptionDreadrootUpdateOld)
            {
                return base.HandleEvent(E);
            }

            var originalObject = E.Object ?? ParentObject;
            var updatedObject = HandleDreadrootObject(originalObject);

            if (originalObject != updatedObject)
            {
                Info($"EnteredCellEvent: Replaced '{originalObject.DisplayName}' with '{updatedObject.DisplayName}'.");
                ParentObject = updatedObject;
            }

            return base.HandleEvent(E);
        }

        public override bool HandleEvent(AfterObjectCreatedEvent E)
        {
            if (!OptionDreadrootEnabled)
            {
                return base.HandleEvent(E);
            }
            var originalObject = E.Object ?? ParentObject;
            var updatedObject = HandleDreadrootObject(originalObject);

            if (originalObject != updatedObject)
            {
                E.ReplacementObject = updatedObject;
                Info($"AfterObjectCreatedEvent: '{originalObject.DisplayName}' replaced and destroyed.");
                //originalObject.Obliterate();
                ParentObject = updatedObject;
            }

            return base.HandleEvent(E);
        }

        private GameObject HandleDreadrootObject(GameObject obj)
        {
            HandleConfigRefresh();
            if (obj == null)
            {
                Info("HandleDreadrootObject: No valid object found, skipping.");
                return obj;
            }

            Info($"HandleDreadrootObject: Processing '{obj.DisplayName}' with blueprint '{obj.Blueprint}'.");

            if (!obj.Blueprint.EqualsExceptFormattingAndCase("Dreadroot"))
            {
                Info("HandleDreadrootObject: Not a Dreadroot, skipping.");
                return obj;
            }

            var updatedObject = TryRemoveTag(obj, "ExcludeFromHostiles");

            if (OptionDreadrootSolid) SetSolidPhysics(updatedObject);
            if (OptionDreadrootHostile) SetHostile(updatedObject);

            return updatedObject;
        }

        private void HandleConfigRefresh()
        {
            if (++configCounter % 10 == 0)
            {
                GetConfigOptions();
                configCounter = 0;
            }
        }

        private void SetSolidPhysics(GameObject obj)
        {
            if (obj?.Physics == null)
            {
                Info("SetSolidPhysics: Object has no IPart<Physics>, skipping.");
                return;
            }

            obj.Physics.Solid = true;
            Info($"SetSolidPhysics: '{obj.DisplayName}' set to solid.");
        }

        private void SetHostile(GameObject obj)
        {
            if (obj?.Brain == null)
            {
                Info("SetHostile: Object has no IPart<Brain>, skipping.");
                return;
            }

            obj.DisplayName = "Deadroot";
            obj.Brain.Allegiance.Hostile = true;
            obj.Brain.Allegiance.TryAdd("Player", -500);
            obj.Brain.SetFactionFeeling("Player", -500);
            obj.Brain.Passive = false;

            Info($"SetHostile: '{obj.DisplayName}' set to Hostile, Player faction also now hated.\nTargetable by Player: {obj.CanBeTargetedByPlayer()}");
        }

        private GameObject TryRemoveTag(GameObject obj, string tagName)
        {
            var blueprint = obj?.GetBlueprint(false);
            if (blueprint == null || !blueprint.HasTag(tagName))
            {
                Info($"TryRemoveTag: Blueprint '{obj?.DisplayName}' does not contain tag '{tagName}', skipping.");
                return obj;
            }

            Info($"TryRemoveTag: Removing tag '{tagName}' from '{blueprint.DisplayName()}'.");
            if (blueprint.Tags.Remove(tagName))
            {
                var newObject = GameObject.Create(blueprint);
                Info($"TryRemoveTag: Created new object '{newObject.DisplayName}'.");
                return newObject;
            }

            return obj;
        }

        private static bool GetBooleanOption(string optionKey) =>
            Options.GetOption(optionKey).EqualsNoCase("Yes");

        private void GetConfigOptions()
        {
            OptionDreadrootEnabled = GetBooleanOption("OptionDreadrootEnabled");
            OptionDreadrootSolid = GetBooleanOption("OptionDreadrootSolid");
            OptionDreadrootHostile = GetBooleanOption("OptionDreadrootHostile");
            OptionDreadrootUpdateOld = GetBooleanOption("OptionDreadrootUpdateOld");
        }
    }
}
