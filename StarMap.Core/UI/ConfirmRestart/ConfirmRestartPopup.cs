using Brutal.ImGuiApi;
using KSA;

namespace StarMap.Core.UI.ConfirmRestart
{
    internal class ConfirmRestartPopup : Popup
    {
        private readonly string _title;
        public ConfirmRestart UI { get; }
        private static readonly PopupButton<ConfirmRestartPopup> PopupButtonContinue = CreateButton("Continue", (Action<ConfirmRestartPopup>)(popup =>
        {
            popup.Active = false;
            popup.UI.Restart = false;
            popup.UI.Show = false;
        }));
        private static readonly PopupButton<ConfirmRestartPopup> PopupButtonRestart = CreateButton("Restart", (Action<ConfirmRestartPopup>)(popup =>
        {
            popup.Active = false;
            popup.UI.Restart = true;
            popup.UI.Show = false;
        }));

        private static readonly IPopupWidget<ConfirmRestartPopup>[] ButtonMatrix = [
            PopupButtonContinue,
            PopupButtonRestart
        ];

        private ConfirmRestartPopup(ConfirmRestart ui)
        { 
            _title = "Game requires restart";
            UI = ui;
        }

        public static ConfirmRestartPopup Create(ConfirmRestart ui) => new(ui);

        protected override void OnDrawUi()
        {
            ImGui.OpenPopup((ImString)_title);
            ImGui.BeginPopup((ImString)_title, ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.Popup);
            ImGuiHelper.SetCurrentWindowToCenter();
            var text1 = new ImString(60, 1);
            text1.AppendLiteral("New mods have been enabled after starting KSA.\nThe game needs to be restarted for these mods to be loaded in StarMap.".AsSpan());
            ImGui.TextWrapped(text1);
            ImGui.Separator();
            DrawUi(this, ButtonMatrix);
            ImGui.EndPopup();
        }
    }
}
